using Octokit;
using Sophia.Data.Contexts;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Sophia.Recommending.RecommendationStraregies
{
    public interface Recommender
    {
        Task ScoreCandidates(long subscriptionId, PullRequest pullRequest, IReadOnlyList<PullRequestFile> pullRequestFiles);
        IEnumerable<Candidate> GetCandidates(int count);
    }
}
