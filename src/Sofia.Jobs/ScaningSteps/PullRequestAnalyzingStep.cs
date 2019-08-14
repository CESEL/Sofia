using Microsoft.EntityFrameworkCore;
using Sophia.Data.Contexts;
using Sophia.Data.Models;
using Sophia.InformationGathering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sophia.Jobs.ScaningSteps
{
    public class PullRequestAnalyzingStep : Step
    {
        public override SubscriptionStatus PreConditionStatus => SubscriptionStatus.PullRequestsGatheringCompleted;

        public override SubscriptionStatus PostConditionStatus => SubscriptionStatus.PullRequestsAnalyzingCompleted;

        public override SubscriptionStatus MidConditionStatus => SubscriptionStatus.PullRequestsAnalyzing;

        public PullRequestAnalyzingStep(SophiaDbContext dbContext):base(dbContext)
        {
            
        }

        protected override async Task Execute()
        {
            if (await AnotherSubscriptionExist())
                return;


            var analyzer = new PullRequestAnalyzer(DbContext,null,null);
            await analyzer.Analyze(Subscription.Id);
        }


        private async Task<bool> AnotherSubscriptionExist()
        {
            return await DbContext.Subscriptions.AnyAsync(q => q.RepositoryId == Subscription.RepositoryId && q.Id != Subscription.Id && q.ScanningStatus == SubscriptionStatus.Completed);
        }

    }
}
