using System;
using System.Collections.Generic;
using System.Text;

namespace Sofia.Recommending.RecommendationStraregies.PersistBasedSpreadingRecommendationStrategy
{
    public class PersistBasedSpreadingMetaCandidateInfo:IMetaCandidateInfo
    {
        public int GlobalTotalReviews { get; set; }

        public int GlobalTotalCommits { get; set; }

        public int LocalTotalReviews { get; set; }

        public int LocalTotalCommits { get; set; }

        public int TotalTouchedFiles { get; set; }

        public int TotalNewFiles { get; set; }

        public int TotalActiveMonths { get; set; }

        public double ProbabilityOfStay { get; set; }

        public double EffortRatio { get; set; }

        public double StayRatio { get; set; }

        public double SpreadingRatio { get; internal set; }
        public int TotalModifiedFiles { get; internal set; }
        public int TotalReviewedFiles { get; internal set; }
    }
}
