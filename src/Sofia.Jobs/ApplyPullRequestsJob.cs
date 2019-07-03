using Sofia.Data.Contexts;
using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Sofia.InformationGathering;
using Microsoft.Extensions.DependencyInjection;
using Sofia.Data.Models;
using System.Threading;
using Microsoft.Extensions.Options;
using Octokit.Bot;
using Sofia.InformationGathering.GitHub;
using Microsoft.Extensions.Logging;

namespace Sofia.Jobs
{
    public class ApplyPullRequestsJob
    {
        private static SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private ILogger<ApplyPullRequestsJob> _logger;
        private IServiceProvider _serviceProvider;
        private SofiaDbContext _sofiaDbContext;

        public ApplyPullRequestsJob(SofiaDbContext sofiaDbContext
            , IServiceProvider serviceProvider
            ,ILogger<ApplyPullRequestsJob> logger)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _sofiaDbContext = sofiaDbContext;
        }

        public async Task Apply()
        {
            _logger.LogInformation("ApplyPullRequestsJob is started");

            _semaphore.Wait();

            _logger.LogInformation("ApplyPullRequestsJob got the semaphore");


            try
            {
                var subscriptionIds = await GetSubscriptions();

                var tasks = ApplyPullRequestBatches(subscriptionIds);

                Task.WaitAll(tasks);

            }
            catch (Exception e)
            {
                _logger.LogError("ApplyPullRequestsJob: Exception {exception}", e.ToString());

                throw;
            }
            finally
            {
                _semaphore.Release();
            }

        }

        private Task[] ApplyPullRequestBatches(long[] subscriptionIds)
        {
            var tasks = new Task[subscriptionIds.Length];

            for (int i = 0; i < subscriptionIds.Length; i++)
            {
                var subscriptionId = subscriptionIds[i]; // to escape the closure effect
                tasks[i] = Task.Run(async () => await Apply(subscriptionId));
            }

            return tasks;
        }

        private async Task<long[]> GetSubscriptions()
        {
            var subscriptionIds = await _sofiaDbContext
                .PullRequests
                .Where(q => q.PullRequestAnalyzeStatus==PullRequestAnalyzeStatus.NotAnalyzed && q.Subscription.ScanningStatus==SubscriptionStatus.Completed)
                .Take(100)
                .Select(q=>q.SubscriptionId)
                .Distinct()
                .ToArrayAsync();

            return subscriptionIds;
        }

        private async Task Apply(long subscriptionId)
        {
            using (var scope=_serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetService<SofiaDbContext>();
                var gitHubOption = scope.ServiceProvider.GetService<IOptions<GitHubOption>>().Value;
                var gitHubRepositoryPullRequestService = scope.ServiceProvider.GetService<GitHubRepositoryPullRequestService>();
                await AnalyzePullRequests(subscriptionId, dbContext, gitHubOption, gitHubRepositoryPullRequestService);
            }
        }

        private async Task AnalyzePullRequests(long subscriptionId, SofiaDbContext dbContext, GitHubOption gitHubOption, GitHubRepositoryPullRequestService gitHubRepositoryPullRequestService)
        {
            try
            {

                var analyzer = new PullRequestAnalyzer(dbContext, gitHubOption, gitHubRepositoryPullRequestService);
                await analyzer.Analyze(subscriptionId);

                await dbContext.SaveChangesAsync();
            }
            catch (Exception e)
            {
                _logger.LogError("ApplyPullRequestsJob: Exception {exception} in {subscriptionId}", e.ToString(), subscriptionId);
                throw;
            }

        }
    }
}
