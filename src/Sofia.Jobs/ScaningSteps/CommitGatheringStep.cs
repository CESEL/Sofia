using Octokit.Bot;
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
    public class CommitGatheringStep:Step
    {
        private readonly GitHubOption _gitHubOption;

        public override SubscriptionStatus PreConditionStatus => SubscriptionStatus.ClonningCompleted;

        public override SubscriptionStatus MidConditionStatus => SubscriptionStatus.CommitGathering;

        public override SubscriptionStatus PostConditionStatus => SubscriptionStatus.CommitGatheringCompleted;

        public CommitGatheringStep(SophiaDbContext dbContext,GitHubOption gitHubOption):base(dbContext)
        {
            _gitHubOption = gitHubOption;
        }

        protected override async Task Execute()
        {
            var installationContext = await GitHubClientFactory.CreateGitHubInstallationClient(_gitHubOption, Subscription.InstallationId);

            var commitAnalyzer = new CommitAnalyzer(Subscription, installationContext.Client);

            var gitCommitTraverser = new CloneBasedGitCommitTraverser(Subscription);
            await gitCommitTraverser.Traverse(commitAnalyzer.AnalayzeCommit);

            await DbContext.Commits.AddRangeAsync(gitCommitTraverser.Commits);
            await DbContext.AddRangeAsync(commitAnalyzer.Contributors);
        }

    }
}
