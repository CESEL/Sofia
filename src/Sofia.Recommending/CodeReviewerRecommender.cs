using Octokit;
using Sofia.Data.Contexts;
using Sofia.Data.Models;
using Sofia.Recommending.RecommendationStraregies;
using Sofia.Recommending.RecommendationStraregies.ChrevRecommendationStrategy;
using Sofia.Recommending.RecommendationStraregies.PersistBasedSpreadingRecommendationStrategy;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sofia.Recommending
{
    public class CodeReviewerRecommender
    {
        private SofiaDbContext _dbContext;
        private RecommenderType _recommenderType;

        public CodeReviewerRecommender(RecommenderType recommenderType, SofiaDbContext dbContext)
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
