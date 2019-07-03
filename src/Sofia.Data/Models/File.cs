
using System;
using System.Collections.Generic;

namespace Sofia.Data.Models
{
    public class File:IEntity
    {
        public File()
        {
            Contributions = new List<Contribution>();
            FileHistories = new List<FileHistory>();
        }

        public long Id { get; set; }
        public string CanonicalName { get; set; }
        public long SubscriptionId { get; set; }
        public Subscription Subscription { get; set; }
        public List<Contribution > Contributions { get; set; }
        public List<FileHistory> FileHistories { get; set; }

        public void AddContribution(Contribution contribution)
        {
            Contributions.Add(contribution);
            
        }

        public void AddHistory(Commit commit, CommitChange change, Contribution contribution)
        {
            if (change.Status != FileHistoryType.Renamed 
                && change.Status != FileHistoryType.Added
                && change.Status !=FileHistoryType.Deleted)
                return;

            FileHistories.Add(new FileHistory()
            {
                File=this,
                Contribution = contribution,
                FileHistoryType = change.Status,
                SubscriptionId = contribution.SubscriptionId,
                OldPath = change.Status==FileHistoryType.Renamed ? change.OldPath : null,
                Path = change.Path,
            }); 
        }
    }
}
