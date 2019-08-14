using Octokit;
using Sophia.Data.Contexts;
using Sophia.Data.Models;
using Sophia.Recommending.RecommendationStraregies;
using Sophia.Recommending.RecommendationStraregies.ChrevRecommendationStrategy;
using Sophia.Recommending.RecommendationStraregies.PersistBasedSpreadingRecommendationStrategy;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sophia.Recommending
{
    public class CodeReviewerRecommender
    {
        private SophiaDbContext _dbContext;
        private RecommenderType _recommenderType;

        public CodeReviewerRecommender(RecommenderType recommenderType, SophiaDbContext dbContext)
        {
            _dbContext = dbContext;
            _recommenderType = recommenderType;
        }

        public async Task<IEnumerable<Candidate>> Recommend(long subscriptionId , Octokit.PullRequest pullRequest,IReadOnlyList<Octokit.PullRequestFile> pullRequestFiles, int topCandidatesLength)
        {
            Recommender recommender = null;

            if (_recommenderType == RecommenderType.Chrev)
            {
                recommender= new ChrevRecommender(_dbContext);
            }
            else if (_recommenderType == RecommenderType.KnowledgeRec)
            {
                recommender= new PersistBasedSpreadingRecommender(_dbContext);
            }
            else
            {
               // TODO
                throw new System.Exception("");
            }

            await recommender.ScoreCandidates(subscriptionId, pullRequest, pullRequestFiles);
            return recommender.GetCandidates(topCandidatesLength);
        }
    }   
  
}
