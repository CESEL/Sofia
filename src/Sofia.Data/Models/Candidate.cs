using System;
using System.Collections.Generic;
using System.Text;

namespace Sophia.Data.Models
{
    public class Candidate : IEntity
    {
        public long Id { get; set; }

        public long SubscriptionId { get; set; }

        public Subscription Subscription { get; set; }

        public int PullRequestNumber { get; set; }

        public int Rank { get; set; }

        public string GitHubLogin  { get; set; }

        public RecommenderType RecommenderType { get; set; }

        public DateTimeOffset SuggestionDateTime { get; set; }
    }

    public enum RecommenderType
    {
        Chrev,KnowledgeRec,KnowledgeRecChRev
    }
}
