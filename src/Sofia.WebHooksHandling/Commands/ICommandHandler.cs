using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Octokit.Bot;
using Sophia.Data.Contexts;

namespace Sophia.WebHooksHandling.Commands
{
    public interface ICommandHandler
    {
        bool IsMatch(string action, string[] parts, string authorAssociation, EventContext eventContext);
        Task Execute(string action,string[] parts, string authorAssociation, EventContext eventContext, SophiaDbContext dbContext);
    }
}
