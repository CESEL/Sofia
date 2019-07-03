using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Octokit.Bot;
using Sofia.Data.Contexts;

namespace Sofia.WebHooksHandling.Commands
{
    internal class NullCommandHandler : ICommandHandler
    {
        public Task Execute(string action, string[] parts, string authorAssociation, EventContext eventContext, SofiaDbContext dbContext)
        {
            return Task.CompletedTask;
        }

        public bool IsMatch(string action, string[] parts, string authorAssociation, EventContext eventContext)
        {
            return false;
        }
    }
}
