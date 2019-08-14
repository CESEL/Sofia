using Microsoft.EntityFrameworkCore;
using Octokit.Bot;
using Polly;
using Sophia.Data;
using Sophia.Data.Contexts;
using Sophia.Data.Models;
using Sophia.InformationGathering.GitHub;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sophia.Jobs.ScaningSteps
{
    public class PullRequestGatheringStep:Step
    {
        private readonly GitHubRepositoryPullRequestService _gitHubRepositoryPullRequestService;
        private readonly GitHubOption _gitHubOption;

        public override SubscriptionStatus PreConditionStatus => SubscriptionStatus.CommitGatheringCompleted;

        public override SubscriptionStatus MidConditionStatus => SubscriptionStatus.PullRequestsGathering;

        public override SubscriptionStatus PostConditionStatus => SubscriptionStatus.PullRequestsGatheringCompleted;

        public PullRequestGatheringStep(SophiaDbContext dbContext, GitHubRepositoryPullRequestService gitHubRepositoryPullRequestService, GitHubOption gitHubOption)
            :base(dbContext)
        {
            _gitHubRepositoryPullRequestService = gitHubRepositoryPullRequestService;
            _gitHubOption = gitHubOption;
        }

        protected override async Task Execute()
        {
            if (await AnotherSubscriptionExist())
                return;

            var response = new GetPullRequestsOfRepositoryQueryResponse()
            {
                Cursor = Subscription.LastPullRequestGithubCursor
            };
            var installationContext = await GetInstallationContext();
            var batchNumber = 0;
            var pullRequestInfos = new List<PullRequestInfo>(1000);

            do
            {
                response = await GetPullRequests(installationContext, response);

                pullRequestInfos.AddRange(response.PullRequests);

                if (batchNumber % 10 == 0)
                {
                    Subscription.LastPullRequestGithubCursor = response.Cursor;
                    batchNumber = 0;

                    await SavePullRequestInfos(pullRequestInfos, Subscription);

                    pullRequestInfos.Clear();
                }

                batchNumber++;

            } while (response.HasNextPage);

            await SavePullRequestInfos(pullRequestInfos, Subscription);

        }

        private async Task<GetPullRequestsOfRepositoryQueryResponse> GetPullRequests(InstallationContext installationContext, GetPullRequestsOfRepositoryQueryResponse response, bool isRetry=false)
        {
            try
            {
                return await _gitHubRepositoryPullRequestService
                    .GetPullRequestsOfRepository(installationContext.AccessToken.Token, Subscription.Owner, Subscription.Repo, response.Cursor);
            }
            catch (GitHubUnauthorizedException e)
            {
                if (isRetry)
                    throw;

                return await GetPullRequests(await GetInstallationContext(),response,true);
            }
          
        }

        private async Task<InstallationContext> GetInstallationContext()
        {
            return await GitHubClientFactory.CreateGitHubInstallationClient(_gitHubOption, Subscription.InstallationId);
        }

        private async Task<bool> AnotherSubscriptionExist()
        {
            return await DbContext.Subscriptions.AnyAsync(q => q.RepositoryId == Subscription.RepositoryId && q.Id != Subscription.Id && q.ScanningStatus == SubscriptionStatus.Completed);
        }

        private async Task SavePullRequestInfos(IEnumerable<PullRequestInfo> pullRequestInfos, Subscription subscription)
        {
            await DbContext.AddRangeAsync(pullRequestInfos.Select(pullRequestInfo => pullRequestInfo.ToPullRequest(subscription.Id)));
            await DbContext.SaveChangesAsync();
        }

    }
}
