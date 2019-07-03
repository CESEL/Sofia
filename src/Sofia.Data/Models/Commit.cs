using Sofia.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sofia.Data
{
    public class Commit:IEntity
    {
        public long Id { get; set; }

        public string Sha { get; set; }

        public DateTimeOffset DateTime { get; set; }

        public Subscription Subscription { get; set; }

        public long SubscriptionId { get; set; }

        public Contributor Contributor { get; set; }

        public long ContributorId { get; set; }

        public ICollection<CommitChange> Changes { get; set; }

        public string AuthorEmail { get; set; }

        public string AuthorName { get; set; }

        public string AuthorGitHubLogin { get; set; }
    }

    public class CommitChange
    {
        public string Path { get; set; }

        public string OldPath { get; set; }

        public bool? OldExists { get; set; }

        public FileHistoryType Status { get; set; }
    }
}
