using System;
using System.Collections.Generic;
using System.Text;

namespace Sofia.Data.Models
{
    public class PullRequest:IEntity
    {
        private PullRequestInfo _pullRequestInfo;

        public long Id { get; set; }
        public long Number { get; set; }
        public int NumberOfFiles { get; set; }
        public PullRequestAnalyzeStatus PullRequestAnalyzeStatus { get; set; }
        public PullRequestInfo PullRequestInfo
        {
            get
            {
                return _pullRequestInfo;
            }
            set
            {
                _pullRequestInfo = value;
                NumberOfFiles = value.TotalPullRequestFiles;
            }
        }
        public long SubscriptionId { get; set; }
        public Subscription Subscription { get; set; }
    }

    public class PullRequestInfo
    {
        public int Number { get; set; }
        public string SubmitterLogin { get; set; }
        public DateTimeOffset? MergedDateTime { get; set; }
        public PullRequestReviewer[] PullRequestReviewers { get; set; }
        public PullRequestFile[] PullRequestFiles { get; set; }
        public string MergeCommitSha { get; set; }
        public int TotalPullRequestFiles { get; set; }

        public bool IsMegaPR()
        {
            return TotalPullRequestFiles > 50;
        }
    }

    public class PullRequestReviewer
    {
        public string Login { get; set; }
        public bool? IsEmployee { get; set; }
        public string Email { get; set; }
    }

    public class PullRequestFile
    {
        public string Path { get; set; }
        public int? Additions { get; set; }
        public int? Deletions { get; set; }
    }
}
