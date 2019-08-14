using System;
using System.Collections.Generic;
using System.Text;

namespace Sophia.Recommending.RecommendationStraregies.ChrevRecommendationStrategy
{
    public class ChrevCandidate : MetaCandidate<ChrevMetaCandidateInfo>
    {
        public ChrevCandidate()
        {
            Meta = new ChrevMetaCandidateInfo();
        }
    }
}
