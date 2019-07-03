using System;
using System.Collections.Generic;
using System.Text;

namespace Sofia.Recommending.RecommendationStraregies.PersistBasedSpreadingRecommendationStrategy
{
    public class PersistBasedSpreadingCandidate: MetaCandidate<PersistBasedSpreadingMetaCandidateInfo>
    {
        public PersistBasedSpreadingCandidate()
        {
            Meta = new PersistBasedSpreadingMetaCandidateInfo();
        }
    }
}
