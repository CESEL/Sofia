using System;
using System.Collections.Generic;
using System.Text;

namespace Sofia.Recommending.RecommendationStraregies.ChrevRecommendationStrategy
{
    public class ChrevCandidate : MetaCandidate<ChrevMetaCandidateInfo>
    {
        public ChrevCandidate()
        {
            Meta = new ChrevMetaCandidateInfo();
        }
    }
}
