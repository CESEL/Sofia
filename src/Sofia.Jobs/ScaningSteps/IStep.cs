using Sofia.Data.Contexts;
using Sofia.Data.Models;
using System;
using System.Threading.Tasks;

namespace Sofia.Jobs.ScaningSteps
{
    public abstract class Step
    {
        public abstract SubscriptionStatus PreConditionStatus { get;  }
        public abstract SubscriptionStatus MidConditionStatus { get; }
        public abstract SubscriptionStatus PostConditionStatus { get; }
        protected SofiaDbContext DbContext { get; }
        protected Subscription Subscription { get; private set; }

        public Step(SofiaDbContext dbContext)
        {
            DbContext = dbContext;
        }

        public async Task Execute(long subscriptionId)
        {
            Subscription = DbContext.Subscriptions.Find(subscriptionId);

            if (Subscription.ScanningStatus != PreConditionStatus)
                return;

            var @event = await AddEvent(Subscription);

            try
            {

                await Execute();

                @event.Finished();
                Subscription.ScanningStatus = PostConditionStatus;
            }
            catch (Exception e)
            {
                Subscription.ScanningStatus = PreConditionStatus;
                throw;
            }
            finally
            {
                await DbContext.SaveChangesAsync();
            }

        }

        protected abstract Task Execute();

        private async Task<SubscriptionEvent> AddEvent(Subscription subscription)
        {
            var @event = new SubscriptionEvent();
            subscription.AddEvent(@event);
            Subscription.ScanningStatus = MidConditionStatus;

            await DbContext.SaveChangesAsync();
            return @event;
        }

    }
}
