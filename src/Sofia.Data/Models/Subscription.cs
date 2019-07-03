using System;
using System.Collections.Generic;
using System.Text;

namespace Sofia.Data.Models
{
    public class Subscription:IEntity
    {
        public Subscription()
        {
            _subscriptionEvents = new List<SubscriptionEvent>();
        }
        public long Id { get; set; }

        public string GitHubRepositoryUrl { get; set; }

        public string Owner { get; set; }

        public string Repo { get; set; }

        public string Branch { get; set; }

        public DateTime SubscriptionDateTime { get; set; }

        public string LocalRepositoryPath { get; set; }

        public long InstallationId { get; set; }

        public long RepositoryId { get; set; }

        public int IssueNumber { get; set; }

        public SubscriptionStatus ScanningStatus { get; set; }

        private List<SubscriptionEvent> _subscriptionEvents;
        public IReadOnlyCollection<SubscriptionEvent> SubscriptionEvents => _subscriptionEvents.AsReadOnly();

        public string LastPullRequestGithubCursor { get; set; }

        public void AddEvent(SubscriptionEvent subscriptionEvent)
        {
            _subscriptionEvents.Add(subscriptionEvent);
        }
    }
}
