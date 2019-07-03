using Octokit.Bot;
using System.Threading.Tasks;

namespace Sofia.Jobs
{
    public class GitHubWebHookHandler
    {
        private readonly WebHookHandlerRegistry _registry;

        public GitHubWebHookHandler(WebHookHandlerRegistry registry)
        {
            _registry = registry;
        }

        public async Task Handle(WebHookEvent webhookEvent)
        {
            await _registry.Handle(webhookEvent);
        }
    }
}
