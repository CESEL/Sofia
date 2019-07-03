using Sofia.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sofia.Recommending
{
    public class Candidate
    {
        public int Rank { get; set; }

        public double Score { get; set; }

        public Contributor Contributor { get; set; }

        public List<Contribution> Contributions { get; set; } = new List<Contribution>();
    }

    public class MetaCandidate<IMeta>:Candidate where IMeta : IMetaCandidateInfo
    {
        public IMeta Meta { get; set; }
    }
}


