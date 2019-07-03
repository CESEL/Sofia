using System;
using System.Collections.Generic;
using System.Text;

namespace Sofia.Data.Models
{
    public class Contribution :IEntity
    {
        public long Id { get; set; }
        public long FileId { get; set; }
        public File File { get; set; }
        public long ContributorId { get; set; }
        public Contributor Contributor { get; set; }
        public string ContributorName { get; set; }
        public string ContributorEmail { get; set; }
        public string ContributorGithubLogin { get; set; }
        public ContributionType ContributionType { get; set; }
        public string ActivityId { get; set; }
        public long SubscriptionId { get; set; }
        public Subscription Subscription { get; set; }
        public DateTimeOffset? DateTime { get; set; }
    }
}
