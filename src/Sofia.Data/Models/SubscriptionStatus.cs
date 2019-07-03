using System;
using System.Collections.Generic;
using System.Text;

namespace Sofia.Data.Models
{
    public enum SubscriptionStatus
    {
        NotScanned=1,
        Clonning=2,
        ClonningCompleted=3,
        CommitGathering=4,
        CommitGatheringCompleted=5,
        PullRequestsGathering=6,
        PullRequestsGatheringCompleted=7,
        PullRequestsAnalyzing=8,
        PullRequestsAnalyzingCompleted=9,
        Completed=10,
    }
}
