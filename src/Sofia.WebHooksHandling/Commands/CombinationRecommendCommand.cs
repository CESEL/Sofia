using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Octokit;
using Octokit.Bot;
using Sophia.Data.Contexts;
using Sophia.Data.Models;
using Sophia.Recommending;
using Sophia.Recommending.RecommendationStraregies.ChrevRecommendationStrategy;
using Sophia.Recommending.RecommendationStraregies.PersistBasedSpreadingRecommendationStrategy;
namespace Sophia.WebHooksHandling.Commands
{
    public class CombinationRecommendCommand : ICommandHandler
    {

        public bool IsMatch(string action, string[] parts, string authorAssociation, EventContext eventContext)
        {
            if (parts.Length != 2 && parts.Length !=4)
                return false;

            if (action != "created")
                return false;

            if (authorAssociation != "OWNER" && authorAssociation != "MEMBER" && authorAssociation != "COLLABORATOR")
                return false;

            if (!string.Equals(parts[0], "sophia", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!string.Equals(parts[1], "suggest", StringComparison.OrdinalIgnoreCase))
                return false;

            if (parts.Length == 4)
            {
                if (!string.Equals(parts[2], "top", StringComparison.OrdinalIgnoreCase))
                    return false;

                if (!int.TryParse(parts[3], out _))
                    return false;
            }

            return true;
        }

        public async Task Execute(string action,string[] parts, string authorAssociation, EventContext eventContext, SophiaDbContext dbContext)
        {

            var issueNumber = (int)eventContext.WebHookEvent.GetPayload().issue.number;
            var repositoryId = (long)eventContext.WebHookEvent.GetPayload().repository.id;
            var subscription = await dbContext.Subscriptions.SingleOrDefaultAsync(q => q.RepositoryId == repositoryId);

            var top = 10;

            if (parts.Length == 4)
            {
                top = int.Parse(parts[3]);
            }

            if (subscription == null)
            {

                var commentResponse = await eventContext.InstallationContext.Client.Issue.Comment
                    .Create(repositoryId, issueNumber,
                    "You have not registered the repository. First, you need to ask Sophia to scan it before asking for suggestions.");
                return;
            }

            if (subscription.ScanningStatus != SubscriptionStatus.Completed)
            {

                var commentResponse = await eventContext.InstallationContext.Client.Issue.Comment
                    .Create(repositoryId, issueNumber,
                    "Sophia has not yet finished scanning the repository. You can ask for suggestion once it is done.");
                return;
            }

            try
            {
                await GetCandidates(eventContext, dbContext, issueNumber, repositoryId, subscription, top);
            }
            catch (NotFoundException e)
            {
                await eventContext.InstallationContext.Client.Issue.Comment
                    .Create(repositoryId, issueNumber,
                    "It's not a pull request. Sophia suggests reviewers for pull requests.");
            }

        }

        private async Task GetCandidates(EventContext eventContext, SophiaDbContext dbContext, int issueNumber, long repositoryId, Data.Models.Subscription subscription, int topCandidatesLength)
        {
            var installationClient = eventContext.InstallationContext.Client;
            var pullRequest = await installationClient.PullRequest.Get(repositoryId, issueNumber);
            var pullRequestFiles = await installationClient.PullRequest.Files(repositoryId, issueNumber);

            var fileOwners = await GetFileOwners(dbContext,subscription.Id, pullRequestFiles);

            var expertCandidates = await GetExpertCandidates(dbContext,subscription.Id,pullRequest, pullRequestFiles, topCandidatesLength);
            var learnerCandidates = await GetLearnerCandidates(dbContext, subscription.Id, pullRequest, pullRequestFiles, topCandidatesLength);

            await SaveCandidates(dbContext, expertCandidates, learnerCandidates, pullRequest, subscription);

            var message = GenerateMessage(expertCandidates,learnerCandidates, pullRequestFiles, fileOwners);

            await installationClient.Issue.Comment.Create(repositoryId, issueNumber, message);
        }

        private async Task<IEnumerable<Recommending.Candidate>> GetLearnerCandidates(SophiaDbContext dbContext, long subscriptionId, Octokit.PullRequest pullRequest, IReadOnlyList<Octokit.PullRequestFile> pullRequestFiles, int topCandidatesLength)
        {
            var recommender = new CodeReviewerRecommender(RecommenderType.KnowledgeRec, dbContext);
            var candidates = await recommender.Recommend(subscriptionId, pullRequest, pullRequestFiles, topCandidatesLength);
            return candidates;
        }

        private async Task<IEnumerable<Recommending.Candidate>> GetExpertCandidates(SophiaDbContext dbContext, long subscriptionId, Octokit.PullRequest pullRequest, IReadOnlyList<Octokit.PullRequestFile> pullRequestFiles, int topCandidatesLength)
        {
            var recommender = new CodeReviewerRecommender(RecommenderType.Chrev, dbContext);
            var candidates = await recommender.Recommend(subscriptionId, pullRequest, pullRequestFiles, topCandidatesLength);
            return candidates;
        }

        private async Task<Dictionary<string, List<long>>> GetFileOwners(SophiaDbContext dbContext,long subscriptionId, IReadOnlyList<Octokit.PullRequestFile> pullRequestFiles)
        {
            var fileOwners = new Dictionary<string, List<long>>();

            foreach (var pullRequestFile in pullRequestFiles)
            {
                fileOwners[pullRequestFile.FileName] = new List<long>();

                var file = await GetFile(dbContext, subscriptionId, pullRequestFile);

                if (file == null)
                    continue;

                var contributorIds = file.Contributions.Select(q => q.ContributorId);

                foreach (var contributorId in contributorIds)
                {
                    var isActive = await IsContributorActive(dbContext, subscriptionId, contributorId);

                    if (!isActive)
                        continue;

                    fileOwners[pullRequestFile.FileName].Add(contributorId);
                }

            }

            return fileOwners;
        }

        private Task<bool> IsContributorActive(SophiaDbContext dbContext, long subscriptionId, long contributorId)
        {
            var lastCheck = DateTimeOffset.Now.Subtract(TimeSpan.FromDays(90));
            return dbContext.Contributions.AnyAsync(q => q.SubscriptionId == subscriptionId && q.ContributorId == contributorId && q.DateTime > lastCheck);
        }

        private async Task<File> GetFile(SophiaDbContext dbContext, long subscriptionId, Octokit.PullRequestFile pullRequestFile)
        {
            var fileHistory = await dbContext.FileHistories.Where(q => q.Path == pullRequestFile.FileName && q.SubscriptionId == subscriptionId)
                .Include(q => q.File)
                    .ThenInclude(q => q.Contributions)
                        .ThenInclude(q => q.Contributor)
                .OrderByDescending(q => q.ContributionId)
                .FirstOrDefaultAsync();

            return fileHistory?.File;
        }

        private async Task SaveCandidates(SophiaDbContext dbContext, IEnumerable<Recommending.Candidate> experts, IEnumerable<Recommending.Candidate> learners, Octokit.PullRequest pullRequest, Data.Models.Subscription subscription)
        {
            var dateTime = DateTimeOffset.UtcNow;

            dbContext.AddRange(experts.Select(q => new Data.Models.Candidate()
            {
                GitHubLogin = q.Contributor.GitHubLogin,
                PullRequestNumber = pullRequest.Number,
                Rank = q.Rank,
                RecommenderType = RecommenderType.Chrev,
                SubscriptionId = subscription.Id,
                SuggestionDateTime = dateTime
            }));

            dbContext.AddRange(learners.Select(q => new Data.Models.Candidate()
            {
                GitHubLogin = q.Contributor.GitHubLogin,
                PullRequestNumber = pullRequest.Number,
                Rank = q.Rank,
                RecommenderType = RecommenderType.KnowledgeRec,
                SubscriptionId = subscription.Id,
                SuggestionDateTime = dateTime
            }));

            await dbContext.SaveChangesAsync();

        }

        private string GenerateMessage(IEnumerable<Recommending.Candidate> expertCandidates, IEnumerable<Recommending.Candidate> learnerCandidates, IReadOnlyList<Octokit.PullRequestFile> pullRequestFiles, Dictionary<string, List<long>> fileOwners)
        {

            if(expertCandidates.Count() + learnerCandidates.Count() == 0)
            {
                return "Sorry! Sophia couldn't find any potential reviewer.";
            }

            var totalFiles = pullRequestFiles.Count();
            var message = "Sophia's suggestions are as below" + Environment.NewLine + Environment.NewLine;

            if (fileOwners.Any(q => q.Value.Count() < 3))
            {
                message += "**_Note_**: Sophia believes at least one of the files are at risk of loss. It is suggested to assign a learner to distribute knowledge" + Environment.NewLine + Environment.NewLine; 
            }

            if (expertCandidates.Count() > 0)
            {
                message += "Sophia has found following **_Potential Experts_**." + Environment.NewLine + Environment.NewLine;

                message += "| Rank | Name | Files Authored | Files Reviewed | New Files | Active Months |" + Environment.NewLine;
                message += "| - | - | - | - | - | - |" + Environment.NewLine;

                foreach (var candidate in expertCandidates.Cast<ChrevCandidate>())
                {
                    message += $"| {candidate.Rank} | {candidate.Contributor.GitHubLogin} | {candidate.Meta.TotalModifiedFiles} / {totalFiles} | {candidate.Meta.TotalReviewedFiles} / {totalFiles} | {candidate.Meta.TotalNewFiles} / {totalFiles} | {candidate.Meta.TotalActiveMonths} / 12 |" + Environment.NewLine;
                }
            }

            if (learnerCandidates.Count() > 0)
            {
                message += Environment.NewLine + "Sophia has found following **_Potential Learners_**." + Environment.NewLine + Environment.NewLine;

                message += "| Rank | Name | Files Authored | Files Reviewed | New Files | Active Months |" + Environment.NewLine;
                message += "| - | - | - | - | - | - |" + Environment.NewLine;

                foreach (var candidate in learnerCandidates.Cast<PersistBasedSpreadingCandidate>())
                {
                    message += $"| {candidate.Rank} | {candidate.Contributor.GitHubLogin} | {candidate.Meta.TotalModifiedFiles} / {totalFiles} | {candidate.Meta.TotalReviewedFiles} / {totalFiles} | {candidate.Meta.TotalNewFiles} / {totalFiles} | {candidate.Meta.TotalActiveMonths} / 12 |" + Environment.NewLine;
                }
            }

            return message;
        }

        
    }
}
