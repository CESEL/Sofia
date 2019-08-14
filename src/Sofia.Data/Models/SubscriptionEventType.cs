using System;
using System.Collections.Generic;
using System.Text;

namespace Sophia.Data.Models
{
    public enum SubscriptionEventType
    {
        Clone,
        CommitGathering,
        PullRequestGathering,
        CommitWebHook,
        PullRequestWebHook
    }
}
