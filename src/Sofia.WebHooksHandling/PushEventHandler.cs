using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Octokit;
using Octokit.Bot;
using Sophia.Data.Contexts;
using Sophia.Data.Models;
using Sophia.InformationGathering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sophia.WebHooksHandling
{
    public class PushEventHandler : IHookHandler
    {
        private ILogger<PushEventHandler> _logger;
        private SophiaDbContext _SophiaDbContext;

        public PushEventHandler(SophiaDbContext SophiaDbContext, ILogger<PushEventHandler> logger)
        {
            _logger = logger;
            _SophiaDbContext = SophiaDbContext;
        }

        public async Task Handle(EventContext eventContext)
        {
            _logger.LogInformation("PushEventHandler: {GitHubDelivery} {GitHubEvent}",
                           eventContext.WebHookEvent.GitHubDelivery, eventContext.WebHookEvent.GitHubEvent);

            if (!eventContext.WebHookEvent.IsMessageAuthenticated)
            {
                _logger.LogInformation("PushEventHandler: Message is not authentic {GitHubDelivery} {GitHubEvent}",
                eventContext.WebHookEvent.GitHubDelivery, eventContext.WebHookEvent.GitHubEvent);

                // message is not issued by GitHub. Possibly from a malucious attacker.
                // log it and return;
                return;
            }

            try
            {
                await HandleEvent(eventContext);
            }
            catch (Exception e )
            {

                _logger.LogError("PushEventHandler Exception: {GitHubDelivery} {exception}",
                    eventContext.WebHookEvent.GitHubDelivery, e.ToString());
                throw;
            }
            
           
        }

        private async Task HandleEvent(EventContext eventContext)
        {
            long repositoryId = (long)eventContext.WebHookEvent.GetPayload().repository.id;
            var branch = eventContext.WebHookEvent.JsonPayload["ref"].Value<string>().Replace("refs/heads/", "");

            var subscription = await _SophiaDbContext.Subscriptions.Where(q => q.RepositoryId == repositoryId && q.Branch == branch).SingleOrDefaultAsync();

            if (subscription == null)
                return;

            if (subscription.ScanningStatus != SubscriptionStatus.Completed)
                return;

            var commits = eventContext.WebHookEvent.GetPayload().commits;
            var commitShas = GetCommitShas(commits);

            var commitTraverser = new GitHubBasedGitCommitTraverser(_SophiaDbContext, subscription, commitShas);
            await commitTraverser.Traverse(eventContext, repositoryId);
        }

        private string[] GetCommitShas(dynamic commits)
        {
            var result = new string[(int)commits.Count];

            for (int i = 0; i < (int)commits.Count; i++)
            {
                result[i] = (string)commits[i].id;
            }

            return result;
        }
    }    
}
