using Octokit.Bot;
using Sophia.Data.Contexts;
using Sophia.Data.Models;
using Sophia.InformationGathering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Sophia.Jobs.ScaningSteps
{
    public class RepositoryCloningStep:Step
    {
        private GitHubOption _gitHubOption;
        private static string _basePath;

        public override SubscriptionStatus PreConditionStatus => SubscriptionStatus.NotScanned;

        public override SubscriptionStatus MidConditionStatus => SubscriptionStatus.Clonning;

        public override SubscriptionStatus PostConditionStatus => SubscriptionStatus.ClonningCompleted;

        public RepositoryCloningStep(SophiaDbContext dbContext, GitHubOption gitHubOption) :base(dbContext)
        {
            _gitHubOption = gitHubOption;
        }

        protected override async Task Execute()
        {
            var cloner = new RepositoryCloner();
            var installationContext = await GitHubClientFactory.CreateGitHubInstallationClient(_gitHubOption, Subscription.InstallationId);
            var token = installationContext.AccessToken.Token;
            var gitHubRepositoryUrl = $"https://x-access-token:{token}@github.com/{Subscription.Owner}/{Subscription.Repo}.git";

            var clonePath = Path.Combine(_basePath, $"{Subscription.Owner}-{Subscription.Repo}");

             cloner.Clone(clonePath, gitHubRepositoryUrl);

            Subscription.LocalRepositoryPath = clonePath;
        }

        public static void SetBasePath(string basePath)
        {
            _basePath = basePath;
        }
    }
}
