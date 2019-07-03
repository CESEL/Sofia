using Octokit;
using Sofia.Data.Contexts;
using Sofia.Recommending.RecommendationStraregies.PersistBasedSpreadingRecommendationStrategy;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sofia.Recommending
{
    public class CodeReviewerRecommender
    {
        private SofiaDbContext _dbContext;

        public CodeReviewerRecommender(SofiaDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<Candidate>> Recommend(long subscriptionId ,PullRequest pullRequest,IReadOnlyList<PullRequestFile> pullRequestFiles)
        {
            var recommender = new PersistBasedSpreadingRecommender(_dbContext);
            await recommender.ScoreCandidates(subscriptionId, pullRequest, pullRequestFiles);
            return recommender.GetCandidates(10);
        }
    }    
}
