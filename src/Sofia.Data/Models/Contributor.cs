using System;
using System.Collections.Generic;
using System.Text;

namespace Sophia.Data.Models
{
    public class Contributor:IEntity
    {

        public Contributor()
        {
            Contributions = new List<Contribution>(500);
            Commits = new List<Commit>(500);
        }

        public long Id { get; set; }

        public string CanonicalName { get; set; }

        public string GitHubLogin { get; set; }

        public long SubscriptionId { get; set; }

        public Subscription Subscription { get; set; }

        public List<Contribution> Contributions { get; set; }

        public List<Commit> Commits { get; set; }

        public void AssignContribution(Contribution contribution)
        {
            Contributions.Add(contribution);
            contribution.Contributor = this;
        }

        public void RemoveContributions()
        {
            Contributions.Clear();
        }

        public void AssignCommits(Commit commit)
        {
            Commits.Add(commit);
            commit.Contributor = this;
            commit.ContributorId = Id;
        }

        public void RemoveCommits()
        {
            Commits.Clear();
        }
    }
}
