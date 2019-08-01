using System;
using System.Collections.Generic;
using System.Text;

namespace Sofia.Recommending.RecommendationStraregies.ChrevRecommendationStrategy
{
    public class ChrevMetaCandidateInfo : IMetaCandidateInfo
    {
        public int GlobalTotalReviews { get; set; }

        public int GlobalTotalCommits { get; set; }

        public int LocalTotalReviews { get; set; }

        public int LocalTotalCommits { get; set; }

        public int TotalTouchedFiles { get; set; }

        public int TotalNewFiles { get; set; }

        public int TotalActiveMonths { get; set; }
        public object TotalModifiedFiles { get; internal set; }
        public object TotalReviewedFiles { get; internal set; }
    }
}
