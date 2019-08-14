using System;
using System.Collections.Generic;
using System.Text;

namespace Sophia.Data.Models
{
    public class SubscriptionEvent:IEntity
    {
        public SubscriptionEvent()
        {
            StartDateTime = DateTime.Now;
        }
        public long Id { get; set; }

        public long SubscriptionId { get; set; }

        public Subscription Subscription { get; set; }

        public DateTime StartDateTime { get; private set; }

        public DateTime? EndDateTime { get; private set; }

        public SubscriptionStatus SubscriptionStatus { get; set; }

        public void Finished()
        {
            EndDateTime = DateTime.Now;
        }
    }
}
