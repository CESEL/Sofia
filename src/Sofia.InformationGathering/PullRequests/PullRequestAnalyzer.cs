using Microsoft.EntityFrameworkCore;
using Octokit.Bot;
using Sofia.Data.Contexts;
using Sofia.Data.Models;
using Sofia.InformationGathering.GitHub;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sofia.InformationGathering
{
    public class PullRequestAnalyzer
    {
        private bool _loadMegaInfo;
        private DateTimeOffset _latestCommitDateTime;
        private Subscription _subscription;
        private InstallationContext _installationClient;
        private SofiaDbContext _dbContext;
        private GitHubOption _gitHubOption;
        private GitHubRepositoryPullRequestService _gitHubRepositoryPullRequestService;
        private Dictionary<string, Contributor> _contributors = new Dictionary<string, Contributor>();
        private PullRequest[] _pullRequests;
        private Dictionary<string, File> _files = new Dictionary<string, File>();

        public PullRequestAnalyzer(SofiaDbContext dbContext, GitHubOption gitHubOption, GitHubRepositoryPullRequestService gitHubRepositoryPullRequestService)
        {
            _dbContext = dbContext;
            _gitHubOption = gitHubOption;
            _gitHubRepositoryPullRequestService = gitHubRepositoryPullRequestService;
        }

        public async Task Analyze(long subscriptionId)
        {
            _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;

            await Init(subscriptionId);

            await AnalyzePullRequests(subscriptionId);

            _dbContext.ChangeTracker.AutoDetectChangesEnabled = true;
        }

        private async Task Init(long subscriptionId)
        {
            _pullRequests = await _dbContext
                .PullRequests
                .AsNoTracking()
                .Where(q => q.SubscriptionId == subscriptionId
                && q.PullRequestAnalyzeStatus == PullRequestAnalyzeStatus.NotAnalyzed)
                .OrderBy(q=>q.PullRequestInfo.MergedDateTime)
                .ToArrayAsync();


            _loadMegaInfo = _pullRequests.Length > 100;

            _latestCommitDateTime = await _dbContext
                .Commits
                .Where(q => q.SubscriptionId == subscriptionId)
                .Select(q=>q.DateTime)
                .MaxAsync();

            _subscription = await _dbContext.Subscriptions.SingleAsync(q=>q.Id == subscriptionId);

            if(_gitHubOption!=null)
                _installationClient = await GitHubClientFactory.CreateGitHubInstallationClient(_gitHubOption, _subscription.InstallationId);

            if (!_loadMegaInfo)
                return;

            _contributors = await _dbContext
                .Contributors
                .AsNoTracking()
                .Where(q => q.SubscriptionId == subscriptionId && q.GitHubLogin != null).ToDictionaryAsync(q => q.GitHubLogin);

            var fileHistories = await _dbContext
                .FileHistories
                .AsNoTracking()
                .Include(q => q.File)
                .Where(q=> q.SubscriptionId== subscriptionId)
                .OrderBy(q=>q.Contribution.DateTime)
                .Select(q=> new
                {
                    q.File,q.OldPath,q.Path
                })
                .ToArrayAsync();

            _files = new Dictionary<string,File>();

            foreach (var fileHistory in fileHistories)
            {
                _files[fileHistory.Path] = fileHistory.File;
            }
        }

        private async Task AnalyzePullRequests(long subscriptionId)
        {
            foreach (var pullRequest in _pullRequests)
            {
                await Analyze(subscriptionId,pullRequest);
            }
        }

        private async Task Analyze(long subscriptionId,PullRequest pullRequest)
        {
            if (!ShouldProcessThisPullRequest(pullRequest))
            {
                return;
            }

            try
            {

                await InitPullRequest(_subscription, pullRequest);

                if (pullRequest.PullRequestAnalyzeStatus == PullRequestAnalyzeStatus.NotAnalyzed)
                {
                    await AddContributionOfReviewers(subscriptionId, pullRequest);
                    pullRequest.PullRequestAnalyzeStatus = PullRequestAnalyzeStatus.Analyzed;
                }

                await SavePullRequest(pullRequest);
            }
            catch (Exception e)
            {
                //TODO log
            }

        }

        private async Task SavePullRequest(PullRequest pullRequest)
        {
            _dbContext.Attach(pullRequest);
            _dbContext.Entry(pullRequest).Property(p => p.PullRequestAnalyzeStatus).IsModified = true;

            await _dbContext.SaveChangesAsync();
        }

        private async Task InitPullRequest(Subscription subscription,PullRequest pullRequest)
        {
            if (pullRequest.PullRequestInfo != null)
                return;

            pullRequest.PullRequestInfo = await _gitHubRepositoryPullRequestService.GetPullRequest(_installationClient.AccessToken.Token
                    ,(int) pullRequest.Number
                    , subscription.Owner
                    , subscription.Repo);

            pullRequest.PullRequestAnalyzeStatus = pullRequest.PullRequestInfo.IsMegaPR() ? PullRequestAnalyzeStatus.NotAnalyzedMegaPR : PullRequestAnalyzeStatus.NotAnalyzed;

            _dbContext.Attach(pullRequest);
            _dbContext.Entry(pullRequest).Property(q => q.PullRequestInfo).IsModified = true;
            _dbContext.Entry(pullRequest).Property(q => q.PullRequestAnalyzeStatus).IsModified = true;

            await _dbContext.SaveChangesAsync();
        }

        private bool ShouldProcessThisPullRequest(PullRequest pullRequest)
        {
            return pullRequest.PullRequestInfo.MergedDateTime < _latestCommitDateTime;
        }

        private async Task AddContributionOfReviewers(long subscriptionId,PullRequest pullRequest)
        {
            var pullRequestFiles = pullRequest.PullRequestInfo.PullRequestFiles;
            var reviewers = pullRequest.PullRequestInfo.PullRequestReviewers.Select(q => q.Login).Distinct();

            foreach (var file in pullRequestFiles)
            {
                foreach (var reviewer in reviewers)
                {
                    if(reviewer!=pullRequest.PullRequestInfo.SubmitterLogin)
                        await AddContribution(subscriptionId,pullRequest.Number, file,reviewer, ContributionType.Review,pullRequest.PullRequestInfo.MergedDateTime);
                }
            }
        }

        private async Task AddContribution(long subscriptionId,long pullRequestNumber,PullRequestFile pullRequestFile, string login, ContributionType contributionType, DateTimeOffset? mergedDateTime)
        {
            var file = await GetFile(pullRequestFile, subscriptionId);

            if (file == null || login == null)
                return;

            var contributor = await GetContributor(login, subscriptionId);

            var contribution = new Contribution()
            {
                ActivityId= pullRequestNumber.ToString(),
                ContributionType = contributionType,
                ContributorId = contributor.Id,
                SubscriptionId = subscriptionId,
                FileId =  file.Id,
                DateTime = mergedDateTime,
                ContributorGithubLogin=login
            };

            _dbContext.Add(contribution);
        }

        private async Task<Contributor> GetContributor(string gitHubLogin,long subscriptionId)
        {
            if (_contributors.ContainsKey(gitHubLogin))
                return _contributors[gitHubLogin];

            if (!_loadMegaInfo)
            {
                var contributor = await _dbContext
                    .Contributors
                    .SingleOrDefaultAsync(q => q.GitHubLogin == gitHubLogin && q.SubscriptionId == subscriptionId);

                if (contributor != null)
                {
                    _contributors[gitHubLogin] = contributor;
                    return contributor;
                }

            }

            var newContributor = new Contributor()
            {
                CanonicalName = gitHubLogin,
                GitHubLogin = gitHubLogin,
                SubscriptionId = subscriptionId
            };

            _contributors[gitHubLogin] = newContributor;
            _dbContext.Add(newContributor);

            return newContributor;
        }

        private async Task<File> GetFile(PullRequestFile pullRequestFile, long subscriptionId)
        {
            var file = _files.GetValueOrDefault(pullRequestFile.Path);

            if (!_loadMegaInfo && file==null)
            {
                file = await _dbContext.FileHistories
                    .Where(q => q.SubscriptionId == subscriptionId && q.Path == pullRequestFile.Path)
                    .OrderBy(q => q.Contribution.DateTime)
                    .Select(q=>q.File)
                    .LastOrDefaultAsync();

                _files[pullRequestFile.Path] = file;
            }

            return file;
        }   
    }
}
