using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit.Bot;
using Sofia.Data.Contexts;
using Sofia.Data.Models;
using Sofia.InformationGathering.GitHub;
using Sofia.Jobs.ScaningSteps;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Sofia.InformationGathering
{
    public class RepositoryScanner
    {
        private IServiceProvider _serviceProvider;
        private ILogger<RepositoryScanner> _logger;

        public RepositoryScanner(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = serviceProvider.GetService<ILogger<RepositoryScanner>>();
        }

        public async Task Scan(long subscriptionId)
        {
            _logger.LogInformation("RepositoryScanner: Clone subscription {subscriptionId}", subscriptionId);
            await Clone(subscriptionId);

            _logger.LogInformation("RepositoryScanner: Gather commits of subscription {subscriptionId}", subscriptionId);
            await GatherCommits(subscriptionId);

            _logger.LogInformation("RepositoryScanner: Gather pullRequests of subscription {subscriptionId}", subscriptionId);
            await GatherPullRequests(subscriptionId);

            _logger.LogInformation("RepositoryScanner: Analyze pullRequests of subscription {subscriptionId}", subscriptionId);
            await AnalyzePullRequests(subscriptionId);

            _logger.LogInformation("RepositoryScanner: completed scanning of subscription {subscriptionId}", subscriptionId);
            await Complete(subscriptionId);
        }

        private async Task AnalyzePullRequests(long subscriptionId)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetService<SofiaDbContext>();
                
                var pullRequestAnalyzingJob = new PullRequestAnalyzingStep(dbContext);
                await pullRequestAnalyzingJob.Execute(subscriptionId);
            }
        }

        private async Task GatherPullRequests(long subscriptionId)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var pullRequestGatheringStep = new PullRequestGatheringStep(
                    scope.ServiceProvider.GetService<SofiaDbContext>(),
                    scope.ServiceProvider.GetService<GitHubRepositoryPullRequestService>(),
                    scope.ServiceProvider.GetService<IOptions<GitHubOption>>().Value);

                await pullRequestGatheringStep.Execute(subscriptionId);
            }
        }

        private async Task GatherCommits(long subscriptionId)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var commitGatheringStep = new CommitGatheringStep(scope.ServiceProvider.GetService<SofiaDbContext>()
                    , scope.ServiceProvider.GetService<IOptions<GitHubOption>>().Value);
                await commitGatheringStep.Execute(subscriptionId);
            }

        }

        private async Task Clone(long subscriptionId)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetService<SofiaDbContext>();
                var gitHubOption = scope.ServiceProvider.GetService<IOptions<GitHubOption>>().Value;

                var clonningJob = new RepositoryCloningStep(dbContext,gitHubOption);
                await clonningJob.Execute(subscriptionId);
            }
        }

        private async Task Complete(long subscriptionId)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetService<SofiaDbContext>();
                var gitHubOption = scope.ServiceProvider.GetService<IOptions<GitHubOption>>().Value;

                var subscription = await dbContext.Subscriptions.SingleAsync(q=>q.Id == subscriptionId);

                if (subscription.ScanningStatus != SubscriptionStatus.PullRequestsAnalyzingCompleted)
                    return;

                var installationContext = await GitHubClientFactory.CreateGitHubInstallationClient(gitHubOption,subscription.InstallationId);

                await installationContext.Client.Issue.Comment.Create(subscription.RepositoryId, subscription.IssueNumber, "@SofiaRec has scanned the whole repository. From now on you can ask for recommendations for pull requests that are going to merge to the master branch.");
                subscription.ScanningStatus = SubscriptionStatus.Completed;

                await dbContext.SaveChangesAsync();
            }
        }
    }
}
