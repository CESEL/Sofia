using System;
using System.Collections.Generic;
using System.Text;

namespace Sofia.Data.Models
{
    public class FileHistory:IEntity
    {
        public long Id { get; set; }
        public FileHistoryType FileHistoryType { get; set; }
        public long FileId { get; set; }
        public File File { get; set; }
        public long ContributionId { get; set; }
        public Contribution Contribution { get; set; }
        public long SubscriptionId { get; set; }
        public Subscription Subscription { get; set; }
        public string OldPath { get; internal set; }
        public string Path { get; internal set; }

    }
}
