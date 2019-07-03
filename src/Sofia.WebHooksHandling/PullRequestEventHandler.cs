using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Octokit.Bot;
using Sofia.Data;
using Sofia.Data.Contexts;
using Sofia.Data.Models;
using Sofia.InformationGathering.GitHub;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sofia.WebHooksHandling
{
    public class PullRequestEventHandler:IHookHandler
    {
        private GitHubRepositoryPullRequestService _gitHubRepositoryPullRequestService;
        private SofiaDbContext _sofiaDbContext;
        private ILogger<PullRequestEventHandler> _logger;

        public PullRequestEventHandler(SofiaDbContext sofiaDbContext,
            GitHubRepositoryPullRequestService gitHubRepositoryPullRequestService,
            ILogger<PullRequestEventHandler> logger)
        {
            _gitHubRepositoryPullRequestService = gitHubRepositoryPullRequestService;
            _sofiaDbContext = sofiaDbContext;
            _logger = logger;
        }

        public async Task Handle(EventContext eventContext)
        {
            _logger.LogInformation("PullRequestEventHandler: {GitHubDelivery} {GitHubEvent}",
                eventContext.WebHookEvent.GitHubDelivery, eventContext.WebHookEvent.GitHubEvent);

            if (!eventContext.WebHookEvent.IsMessageAuthenticated)
            {
                _logger.LogInformation("PullRequestEventHandler: Message is not authentic {GitHubDelivery} {GitHubEvent}",
                eventContext.WebHookEvent.GitHubDelivery, eventContext.WebHookEvent.GitHubEvent);

                // message is not issued by GitHub. Possibly from a malucious attacker.
                // log it and return;
                return;
            }

            try
            {
                await HandleEvent(eventContext);
            }
            catch (Exception e)
            {

                _logger.LogError("PullRequestEventHandler Exception: {GitHubDelivery} {exception}",
                    eventContext.WebHookEvent.GitHubDelivery, e.ToString());

                throw;
            }            
        }

        private async Task HandleEvent(EventContext eventContext)
        {
            var action = (string)eventContext.WebHookEvent.GetPayload().action;
            var merged = (bool)eventContext.WebHookEvent.GetPayload().pull_request.merged;

            if (action != "closed" && !merged)
                return;

            var repositoryId = (long)eventContext.WebHookEvent.GetPayload().repository.id;
            var repositoryName = (string)eventContext.WebHookEvent.GetPayload().repository.name;
            var repositoryOwner = (string)eventContext.WebHookEvent.GetPayload().repository.owner.login;
            var pullRequestNumber = (int)eventContext.WebHookEvent.GetPayload().pull_request.number;

            // we have only one branch of each repository
            var subscription = await _sofiaDbContext.Subscriptions.Where(q => q.RepositoryId == repositoryId).SingleOrDefaultAsync();

            if (subscription == null)
                return;

            if (subscription.ScanningStatus != SubscriptionStatus.Completed)
                return;

            var pullRequest = await GetPullRequest(eventContext.InstallationContext.AccessToken.Token,
                pullRequestNumber, repositoryOwner, repositoryName,subscription.Id,eventContext);

            _sofiaDbContext.Add(pullRequest);

            await _sofiaDbContext.SaveChangesAsync();
        }

        private async Task<PullRequest> GetPullRequest(string token, int pullRequestNumber, string repositoryOwner,string repositoryName,long subscriptionId,EventContext eventContext)
        {
            try
            {
                var prInfo = await _gitHubRepositoryPullRequestService.GetPullRequest(token
                    , pullRequestNumber
                    , repositoryOwner
                    , repositoryName);

                return prInfo.ToPullRequest(subscriptionId);
            }
            catch (Exception e)
            {

                _logger.LogError("PullRequestEventHandler Exception: {GitHubDelivery} {exception} | Cannot fetch the pullRequestInfo.", 
                    eventContext.WebHookEvent.GitHubDelivery, e.ToString());

                //TODO fill it later during the job. Don't fail it here.
                return new PullRequest()
                {
                    SubscriptionId = subscriptionId,
                    Number = pullRequestNumber,
                    PullRequestAnalyzeStatus = PullRequestAnalyzeStatus.NotAnalyzed
                };
                
            }
        }
    }
}