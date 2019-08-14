using Sophia.Data.Contexts;
using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Sophia.InformationGathering;
using Microsoft.Extensions.DependencyInjection;
using Sophia.Data.Models;
using Sophia.InformationGathering.GitHub;
using Microsoft.Extensions.Options;
using System.Threading;
using Octokit.Bot;
using Microsoft.Extensions.Logging;

namespace Sophia.Jobs
{
    public class ScanRepositoriesJob
    {
        private static SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        private IServiceProvider _serviceProvider;
        private SophiaDbContext _SophiaDbContext;
        private IOptions<GitHubOption> _gitHubOptions;
        private ILogger<ScanRepositoriesJob> _logger;

        public ScanRepositoriesJob(SophiaDbContext SophiaDbContext,
            IOptions<GitHubOption> gitHubOptions, 
            IServiceProvider serviceProvider,
            ILogger<ScanRepositoriesJob> logger)
        {
            _serviceProvider = serviceProvider;
            _SophiaDbContext = SophiaDbContext;
            _gitHubOptions = gitHubOptions;
            _logger = logger;
        }

        public async Task Scan()
        {
            _logger.LogInformation("ScanRepositoriesJob is started");

            _semaphore.Wait();

            _logger.LogInformation("ScanRepositoriesJob got the semaphore");

            try
            {
                var unscannedSubscriptions = await GetSubscriptionsToRefresh();

                _logger.LogInformation("ScanRepositoriesJob will process {unscannedSubscriptionsLength} subscriptions", unscannedSubscriptions.Length);

                var refreshTasks = RefreshSubscriptions(unscannedSubscriptions);

                await Task.WhenAll(refreshTasks);

            }
            catch (Exception e)
            {
                _logger.LogError("ScanRepositoriesJob: Exception {exception}", e.ToString());
                throw;
            }
            finally
            {
                _semaphore.Release();
            }

        }

        private Task[] RefreshSubscriptions(Subscription[] unscannedSubscriptions)
        {
            var tasks = new Task[unscannedSubscriptions.Length];

            for (int i = 0; i < unscannedSubscriptions.Length; i++)
            {
                var subscription = unscannedSubscriptions[i]; // to escape the closure effect
                tasks[i] = Task.Run(async () => await Scan(subscription.Id));
            }

            return tasks;
        }

        private async Task<Subscription[]> GetSubscriptionsToRefresh()
        {
            var unscannedSubscriptions = await _SophiaDbContext
                .Subscriptions
                .Where(q => q.ScanningStatus!= SubscriptionStatus.Completed)
                .OrderBy(q => q.SubscriptionDateTime)
                .Take(10)
                .AsNoTracking()
                .ToArrayAsync();
            return unscannedSubscriptions;
        }

        private async Task Scan(long subscriptionId)
        {

            try
            {
                var scanner = new RepositoryScanner(_serviceProvider);
                await scanner.Scan(subscriptionId);
            }
            catch (Exception e)
            {
                _logger.LogError("RepositoryScanner: Exception {exception} in {subscriptionId}", e.ToString(),subscriptionId);
                throw;
            }


        }
    }
}
