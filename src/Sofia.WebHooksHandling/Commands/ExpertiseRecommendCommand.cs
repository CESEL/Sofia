using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Octokit;
using Octokit.Bot;
using Sofia.Data.Contexts;
using Sofia.Data.Models;
using Sofia.Recommending;
using Sofia.Recommending.RecommendationStraregies.ChrevRecommendationStrategy;
using Sofia.Recommending.RecommendationStraregies.PersistBasedSpreadingRecommendationStrategy;
namespace Sofia.WebHooksHandling.Commands
{
    public class ExpertiseRecommendCommandRecommendCommand : ICommandHandler
    {

        public bool IsMatch(string action, string[] parts, string authorAssociation, EventContext eventContext)
        {
            if (parts.Length != 3 && parts.Length !=5)
                return false;

            if (action != "created")
                return false;

            if (authorAssociation != "OWNER" && authorAssociation != "MEMBER" && authorAssociation != "COLLABORATOR")
                return false;

            if (!string.Equals(parts[0], "@sophia", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!string.Equals(parts[1], "suggest", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!string.Equals(parts[2], "experts", StringComparison.OrdinalIgnoreCase))
                return false;

            if (parts.Length == 5)
            {
                if (!string.Equals(parts[3], "top", StringComparison.OrdinalIgnoreCase))
                    return false;

                if (!int.TryParse(parts[4], out _))
                    return false;
            }

            return true;
        }

        public async Task Execute(string action,string[] parts, string authorAssociation, EventContext eventContext, SofiaDbContext dbContext)
        {

            var issueNumber = (int)eventContext.WebHookEvent.GetPayload().issue.number;
            var repositoryId = (long)eventContext.WebHookEvent.GetPayload().repository.id;
            var subscription = await dbContext.Subscriptions.SingleOrDefaultAsync(q => q.RepositoryId == repositoryId);

            var top = 10;

            if (parts.Length == 5)
            {
                top = int.Parse(parts[4]);
            }

            if (subscription == null)
            {

                var commentResponse = await eventContext.InstallationContext.Client.Issue.Comment
                    .Create(repositoryId, issueNumber,
                    "You have not registered the repository. First, you need to ask SofiaRec to scan it before asking for suggestions.");
                return;
            }

            if (subscription.ScanningStatus != SubscriptionStatus.Completed)
            {

                var commentResponse = await eventContext.InstallationContext.Client.Issue.Comment
                    .Create(repositoryId, issueNumber,
                    "SofiaRec has not yet finished scanning the repository. You can ask for suggestion once it is done.");
                return;
            }

            try
            {
                await GetCandidates(eventContext, dbContext, issueNumber, repositoryId, subscription, top);
            }
            catch (NotFoundException e)
            {
                await eventContext.InstallationContext.Client.Issue.Comment
                    .Create(repositoryId, issueNumber,
                    "It's not a pull request. Sofia suggests reviewers for pull requests.");
            }

        }

        private async Task GetCandidates(EventContext eventContext, SofiaDbContext dbContext, int issueNumber, long repositoryId, Data.Models.Subscription subscription, int topCandidatesLength)
        {
            var installationClient = eventContext.InstallationContext.Client;
            var recommender = new CodeReviewerRecommender(RecommenderType.Chrev,dbContext);

            var pullRequest = await installationClient.PullRequest.Get(repositoryId, issueNumber);
            var pullRequestFiles = await installationClient.PullRequest.Files(repositoryId, issueNumber);

            var candidates = await recommender.Recommend(subscription.Id, pullRequest, pullRequestFiles, topCandidatesLength);

            await SaveCandidates(dbContext, candidates, pullRequest, subscription);

            var message = GenerateMessage(candidates,pullRequestFiles);

            await installationClient.Issue.Comment.Create(repositoryId, issueNumber, message);
        }

        private async Task SaveCandidates(SofiaDbContext dbContext, IEnumerable<Recommending.Candidate> candidates, Octokit.PullRequest pullRequest, Data.Models.Subscription subscription)
        {
            var dateTime = DateTimeOffset.UtcNow;

            dbContext.AddRange(candidates.Select(q => new Data.Models.Candidate()
            {
                GitHubLogin = q.Contributor.GitHubLogin,
                PullRequestNumber = pullRequest.Number,
                Rank = q.Rank,
                RecommenderType = RecommenderType.Chrev,
                SubscriptionId = subscription.Id,
                SuggestionDateTime = dateTime
            }));

            await dbContext.SaveChangesAsync();

        }

        private string GenerateMessage(IEnumerable<Recommending.Candidate> candidates, IReadOnlyList<Octokit.PullRequestFile> pullRequestFiles)
        {
            if (candidates.Count() == 0)
            {
                return "Sorry! Sofia couldn't find any potential reviewer.";
            }

            var totalFiles = pullRequestFiles.Count();
            var message = "Sofia's suggestions are as below" + Environment.NewLine + Environment.NewLine;

            if (candidates.Count() > 0)
            {
                message += "Sofia has found following **_Potential Experts_**." + Environment.NewLine + Environment.NewLine;

                message += "| Rank | Name | Commit Ownership | Review Ownership | Learning | Active Months |" + Environment.NewLine; ;
                message += "| - | - | - | - | - | - |" + Environment.NewLine;

                foreach (var candidate in candidates.Cast<ChrevCandidate>())
                {
                    message += $"| {candidate.Rank} | {candidate.Contributor.GitHubLogin} | {candidate.Meta.TotalModifiedFiles} / {totalFiles} | {candidate.Meta.TotalReviewedFiles} / {totalFiles} | {candidate.Meta.TotalNewFiles} / {totalFiles} | {candidate.Meta.TotalActiveMonths} / 12 |" + Environment.NewLine;
                }
            }

            return message;
        }

        
    }
}
