using Sophia.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sophia.Data
{
    public static class Extensions
    {
        public static PullRequest ToPullRequest(this PullRequestInfo pullRequestInfo, long subscriptionId)
        {
            return new PullRequest()
            {
                Number = pullRequestInfo.Number,
                PullRequestAnalyzeStatus = pullRequestInfo.IsMegaPR() ? PullRequestAnalyzeStatus.NotAnalyzedMegaPR : PullRequestAnalyzeStatus.NotAnalyzed,
                SubscriptionId = subscriptionId,
                PullRequestInfo = pullRequestInfo,
                NumberOfFiles = pullRequestInfo.TotalPullRequestFiles
            };
        }
    }
}
