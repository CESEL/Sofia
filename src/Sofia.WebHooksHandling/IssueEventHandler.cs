using Microsoft.Extensions.Logging;
using Octokit.Bot;
using Sofia.Data.Contexts;
using Sofia.WebHooksHandling.Commands;
using System.Linq;
using System.Threading.Tasks;

namespace Sofia.WebHooksHandling
{
    public class IssuesEventHandler:IHookHandler
    {
        private ILogger<IssueCommentEventHandler> _logger;
        private SofiaDbContext _sofiaDbContext;

        public IssuesEventHandler(SofiaDbContext sofiaDbContext,ILogger<IssueCommentEventHandler> logger)
        {
            _logger = logger;
            _sofiaDbContext = sofiaDbContext;
        }

        public async Task Handle(EventContext eventContext)
        {
            _logger.LogInformation("IssuesEventHandler: {GitHubDelivery} {GitHubEvent}",
                eventContext.WebHookEvent.GitHubDelivery, eventContext.WebHookEvent.GitHubEvent);

            if (!eventContext.WebHookEvent.IsMessageAuthenticated)
            {
                _logger.LogInformation("IssuesEventHandler: Message is not authentic {GitHubDelivery} {GitHubEvent}",
                eventContext.WebHookEvent.GitHubDelivery, eventContext.WebHookEvent.GitHubEvent);

                // message is not issued by GitHub. Possibly from a malucious attacker.
                // log it and return;
                return;
            }

            try
            {
                await HandleEvent(eventContext);
            }
            
            catch (System.Exception e)
            {

                _logger.LogError("IssuesEventHandler Exception: {GitHubDelivery} {exception}",
                    eventContext.WebHookEvent.GitHubDelivery, e.ToString());

                throw;
            }
            

        }

        private async Task HandleEvent(EventContext eventContext)
        {
            var action = (string)eventContext.WebHookEvent.GetPayload().action;
            var body = (string)eventContext.WebHookEvent.GetPayload().issue.body;
            body = body.Trim();
            var parts = body.Split(' ').Select(q => q.Trim()).ToArray();
            var authorAssociation = (string)eventContext.WebHookEvent.GetPayload().issue.author_association;

            var commandHandler = CommandHandlerFactory.GetHandler(action, parts, authorAssociation, eventContext);
            await commandHandler.Execute(action, parts, authorAssociation, eventContext, _sofiaDbContext);
        }
    }
}
