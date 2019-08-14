using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Octokit.Bot;
using Sophia.Data.Contexts;

namespace Sophia.WebHooksHandling.Commands
{
    internal class NullCommandHandler : ICommandHandler
    {
        public Task Execute(string action, string[] parts, string authorAssociation, EventContext eventContext, SophiaDbContext dbContext)
        {
            return Task.CompletedTask;
        }

        public bool IsMatch(string action, string[] parts, string authorAssociation, EventContext eventContext)
        {
            return false;
        }
    }
}
