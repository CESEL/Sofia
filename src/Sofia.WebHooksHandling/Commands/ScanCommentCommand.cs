using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Octokit.Bot;
using Sophia.Data.Contexts;
using Sophia.Data.Models;

namespace Sophia.WebHooksHandling.Commands
{
    public class ScanCommentCommand : ICommandHandler
    {
        public async Task Execute(string action, string[] parts, string authorAssociation, EventContext eventContext, SophiaDbContext dbContext)
        {
            var branch = parts[3];
            var issueNumber = (int)eventContext.WebHookEvent.GetPayload().issue.number;
            var repositoryId = (long)eventContext.WebHookEvent.GetPayload().repository.id;
            var owenerName = (string)eventContext.WebHookEvent.GetPayload().repository.owner.login;
            var repositoryName = (string)eventContext.WebHookEvent.GetPayload().repository.name;
            var repositoryUrl = (string)eventContext.WebHookEvent.GetPayload().repository.html_url;

            if (await AlreadyScanned(dbContext,repositoryId))
            {
                var commentResponse = await eventContext.InstallationContext.Client.Issue.Comment
                    .Create(repositoryId, issueNumber, "Sofia has already scanned this branch and is monitoring it.");
            }
            else
            {
                var commentResponse = await eventContext.InstallationContext.Client.Issue.Comment.Create(repositoryId, issueNumber, "Sofia just started to scan your repository. After Completion you can ask for suggestions for code reviewers!");
                await SaveSubscription(issueNumber, repositoryId, owenerName, repositoryName, repositoryUrl, branch, eventContext.WebHookEvent.GetInstallationId(), dbContext);
            }
        }

        private async Task<bool> AlreadyScanned(SophiaDbContext dbContext, long repositoryId)
        {
            return await dbContext.Subscriptions.AnyAsync(q => q.RepositoryId == repositoryId);
        }

        public bool IsMatch(string action, string[] parts, string authorAssociation, EventContext eventContext)
        {
            if (parts.Length != 4)
                return false;

            if (string.Equals(parts[0], "sofia", StringComparison.OrdinalIgnoreCase))
                return false;

            if (string.Equals(parts[1], "scan", StringComparison.OrdinalIgnoreCase))
                return false;

            if (string.Equals(parts[2], "branch", StringComparison.OrdinalIgnoreCase))
                return false;

            if (action != "created")
                return false;

            if (authorAssociation != "OWNER" && authorAssociation != "MEMBER" && authorAssociation != "COLLABORATOR")
                return false;

            return true;
        }

        private async Task SaveSubscription(int issueNumber, long repositoryId, string owner, string repository, string repositoryUrl, string branch, long? installationId,SophiaDbContext dbContext)
        {
            var subscription = new Subscription()
            {
                Branch = branch,
                InstallationId = installationId.Value,
                RepositoryId = repositoryId,
                Owner = owner,
                Repo = repository,
                IssueNumber = issueNumber,
                SubscriptionDateTime = DateTime.Now,
                ScanningStatus = SubscriptionStatus.NotScanned,
                GitHubRepositoryUrl = repositoryUrl
            };

            dbContext.Add(subscription);
            await dbContext.SaveChangesAsync();
        }
    }
}
