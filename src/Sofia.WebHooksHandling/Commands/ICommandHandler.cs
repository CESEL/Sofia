using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Octokit.Bot;
using Sofia.Data.Contexts;

namespace Sofia.WebHooksHandling.Commands
{
    public interface ICommandHandler
    {
        bool IsMatch(string action, string[] parts, string authorAssociation, EventContext eventContext);
        Task Execute(string action,string[] parts, string authorAssociation, EventContext eventContext, SofiaDbContext dbContext);
    }
}
