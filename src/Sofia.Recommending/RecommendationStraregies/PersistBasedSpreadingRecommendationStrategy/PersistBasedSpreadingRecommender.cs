using Microsoft.EntityFrameworkCore;
using Octokit;
using Sophia.Data.Contexts;
using Sophia.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sophia.Recommending.RecommendationStraregies.PersistBasedSpreadingRecommendationStrategy
{
    public class PersistBasedSpreadingRecommender : Recommender
    {
        private SophiaDbContext _dbContext;
        private Dictionary<string, PersistBasedSpreadingCandidate> _dicCandidates = new Dictionary<string, PersistBasedSpreadingCandidate>();
        private PersistBasedSpreadingCandidate[] _candidates;
        private List<long> _fileIds = new List<long>();

        public PersistBasedSpreadingRecommender(SophiaDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task ScoreCandidates(long subscriptionId, Octokit.PullRequest pullRequest, IReadOnlyList<Octokit.PullRequestFile> pullRequestFiles)
        {
            await GetCandidates(subscriptionId, pullRequest, pullRequestFiles);

            if (AllCandidatesHaveZeroScore())
            {
                await FindFolderLevelCandidates(subscriptionId, pullRequest, pullRequestFiles);
            }
        }



        private async Task GetCandidates(long subscriptionId, Octokit.PullRequest pullRequest, IReadOnlyList<Octokit.PullRequestFile> pullRequestFiles)
        {
            var files = await GetFiles(subscriptionId, pullRequestFiles);
            FindCandidates(subscriptionId, pullRequest, files);
            await ScoreCandidates(subscriptionId, pullRequestFiles);
        }

        private bool AllCandidatesHaveZeroScore()
        {
            return _dicCandidates.Count == 0 && _dicCandidates.All(q=>q.Value.Score==0);
        }

        private async Task ScoreCandidates(long subscriptionId,IReadOnlyList<Octokit.PullRequestFile> pullRequestFiles)
        {
            await CalculateCandidatesScores(subscriptionId, pullRequestFiles);

            SortCandidates();
        }

        private async Task CalculateCandidatesScores(long subscriptionId, IReadOnlyList<Octokit.PullRequestFile> pullRequestFiles)
        {
            var totalEffort = await GetTotalEffort(subscriptionId, _dbContext);

            foreach (var candidate in _dicCandidates.Values)
            {
                var candidateEffort = await GetCandidateEffort(candidate, _dbContext);
                candidate.Meta.GlobalTotalCommits = candidateEffort.TotalCommts;
                candidate.Meta.GlobalTotalReviews = candidateEffort.TotalReviews;
                candidate.Meta.LocalTotalCommits = candidate.Contributions.Count(q => q.ContributionType == ContributionType.Commit && _fileIds.Any(f => f == q.FileId));
                candidate.Meta.LocalTotalReviews = candidate.Contributions.Count(q => q.ContributionType == ContributionType.Review && _fileIds.Any(f => f == q.FileId));
                candidate.Meta.TotalTouchedFiles = candidate.Contributions.Where(q => _fileIds.Any(f => f == q.FileId)).Select(q => q.FileId).Distinct().Count();
                candidate.Meta.TotalModifiedFiles = candidate.Contributions.Where(q => q.ContributionType == ContributionType.Commit && _fileIds.Any(f => f == q.FileId)).Select(q => q.FileId).Distinct().Count();
                candidate.Meta.TotalReviewedFiles = candidate.Contributions.Where(q => q.ContributionType == ContributionType.Review && _fileIds.Any(f => f == q.FileId)).Select(q => q.FileId).Distinct().Count();
                candidate.Meta.TotalNewFiles = pullRequestFiles.Count() - candidate.Meta.TotalTouchedFiles;
                candidate.Meta.TotalActiveMonths = await GetTotalActiveMonth(candidate, _dbContext);
                candidate.Meta.StayRatio = GetCandidateProbabilityOfStay(candidate); // needs TotalActiveMonth
                candidate.Meta.EffortRatio = ComputeEffortRatio(totalEffort.TotalReviews, totalEffort.TotalCommits, candidate);
                candidate.Meta.ProbabilityOfStay = Math.Pow(candidate.Meta.StayRatio * candidate.Meta.EffortRatio, 1);
                var specializedKnowledge = candidate.Meta.TotalTouchedFiles / (double)pullRequestFiles.Count;

                candidate.Meta.SpreadingRatio = 1 - specializedKnowledge;

                if (specializedKnowledge > 1) // if it's a folder level dev
                {
                    candidate.Score = candidate.Meta.ProbabilityOfStay;
                }
                else
                {
                    candidate.Score = candidate.Meta.ProbabilityOfStay * candidate.Meta.SpreadingRatio;
                }
            }
        }

        private void SortCandidates()
        {
            _candidates = _dicCandidates.Values.OrderByDescending(q => q.Score)
                 .ThenByDescending(q => q.Contributions.Count())
                 .ToArray();

            for (int i = 0; i < _candidates.Length; i++)
            {
                _candidates[i].Rank = i + 1;
            }
        }

        public IEnumerable<Candidate> GetCandidates(int count)
        {
            return _candidates.Take(count);
        }

        private static double ComputeEffortRatio(int TotalReviews, int TotalCommits, PersistBasedSpreadingCandidate candidate)
        {
            return ((2 * candidate.Meta.GlobalTotalReviews) + candidate.Meta.GlobalTotalCommits) / (double)((2 * TotalReviews) + TotalCommits);
        }

        private void FindCandidates(long subscriptionId, Octokit.PullRequest pullRequest,File[] files)
        {
            foreach (var file in files)
            {
                var contributions = file?.Contributions;
                AddContributions(contributions);
            }
        }

        private async Task FindFolderLevelCandidates(long subscriptionId, Octokit.PullRequest pullRequest, IReadOnlyList<Octokit.PullRequestFile> pullRequestFiles)
        {
            foreach (var pullRequestFile in pullRequestFiles)
            {
                var contributions = await GetFolderLevelContributions(subscriptionId, pullRequestFile);
                AddContributions(contributions);
                await ScoreCandidates(subscriptionId, pullRequestFiles);
            }

        }

        private void AddContributions(List<Contribution> contributions)
        {
            if (contributions == null)
                return;

            foreach (var contribution in contributions)
            {
                if (contribution.Contributor.GitHubLogin == null)
                    continue;

                if (!_dicCandidates.ContainsKey(contribution.Contributor.GitHubLogin))
                {
                    _dicCandidates[contribution.Contributor.GitHubLogin] = new PersistBasedSpreadingCandidate()
                    {
                        Contributor = contribution.Contributor
                    };
                }

                _dicCandidates[contribution.Contributor.GitHubLogin].Contributions.Add(contribution);
            }
        }

        private async Task<File[]> GetFiles(long subscriptionId, IReadOnlyList<Octokit.PullRequestFile> pullRequestFiles)
        {
            var files = new List<File>(pullRequestFiles.Count);

            foreach (var pullRequestFile in pullRequestFiles)
            {
                files.Add(await GetFile(subscriptionId, pullRequestFile));
            }

            _fileIds = files.Where(q => q != null).Select(q => q.Id).ToList();

            return files.ToArray();
        }

        private async Task<List<Contribution>> GetFolderLevelContributions(long subscriptionId,Octokit.PullRequestFile pullRequestFile)
        {
            var parts = pullRequestFile.FileName.Split('/');
            var length = parts.Length - 1;
            var result = new List<Contribution>();
            var hashSet = new HashSet<long>();
            do
            {

                var path = "";

                for (int i = 0; i < length; i++)
                {
                    path +=parts[i]+"/";
                }

                path += "%";

                // need to tune it. this really sucks performance-wise
                var contributions = await _dbContext.FileHistories.Where(q => q.FileHistoryType != FileHistoryType.Deleted
                && EF.Functions.Like(q.Path, path) && q.SubscriptionId== subscriptionId).SelectMany(q=>q.File.Contributions)
                .Include(q=>q.Contributor)
                .ToListAsync();

                // there are duplications in the contributions
                foreach (var contribution in contributions)
                {
                    if (hashSet.Contains(contribution.Id))
                        continue;

                    hashSet.Add(contribution.Id);
                    result.Add(contribution);
                }



            } while (length > 1 && result.Count == 0);

            return result;
        }

        private async Task<File> GetFile(long subscriptionId, Octokit.PullRequestFile pullRequestFile)
        {
            var fileHistory = await _dbContext.FileHistories.Where(q => q.Path == pullRequestFile.FileName && q.SubscriptionId == subscriptionId)
                .Include(q=>q.File)
                    .ThenInclude(q=>q.Contributions)
                        .ThenInclude(q=>q.Contributor)
                .OrderByDescending(q => q.ContributionId)
                .FirstOrDefaultAsync();

            return fileHistory?.File;
        }

        private async Task<int> GetTotalActiveMonth(Candidate candidate, SophiaDbContext dbContext)
        {
            var lastYear = DateTimeOffset.Now.AddYears(-1);

            return await _dbContext.Contributions
                .Where(q => q.ContributorId == candidate.Contributor.Id && q.DateTime >= lastYear)
                .Select(q => q.DateTime.Value.Month)
                .Distinct()
                .CountAsync();
        }

        private double GetCandidateProbabilityOfStay(PersistBasedSpreadingCandidate candidate)
        {
            return candidate.Meta.TotalActiveMonths / 12.0;
        }

        private async Task<(int TotalReviews, int TotalCommits)> GetTotalEffort(long subscriptionId, SophiaDbContext dbContext)
        {
            var lastYear = DateTimeOffset.Now.AddYears(-1);

            var totalReviews = await _dbContext.Contributions.Where(q => q.SubscriptionId == subscriptionId &&
            q.ContributionType == ContributionType.Review &&
            q.DateTime >= lastYear)
            .Select(q => q.ActivityId)
            .Distinct()
            .CountAsync();

            var totalCommits = await _dbContext.Contributions.Where(q => q.SubscriptionId== subscriptionId &&
            q.ContributionType == ContributionType.Commit &&
            q.DateTime >= lastYear)
            .Select(q=>q.ActivityId)
            .Distinct()
            .CountAsync();

            return (totalReviews, totalCommits);
        }

        private async Task<(int TotalReviews, int TotalCommts)> GetCandidateEffort(Candidate candidate, SophiaDbContext dbContext)
        {
            var lastYear = DateTimeOffset.Now.AddYears(-1);

            var totalReviews = await _dbContext.Contributions.Where(q => q.ContributorId == candidate.Contributor.Id &&
            q.ContributionType == ContributionType.Review &&
            q.DateTime >= lastYear)
            .Select(q=>q.ActivityId)
            .Distinct()
            .CountAsync();

            var totalCommits = await _dbContext.Contributions.Where(q => q.ContributorId == candidate.Contributor.Id &&
            q.ContributionType == ContributionType.Commit &&
            q.DateTime >= lastYear)
            .Select(q => q.ActivityId)
            .Distinct()
            .CountAsync();

            return (totalReviews, totalCommits);
        }
    }
}
