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
using Sofia.Recommending.RecommendationStraregies.PersistBasedSpreadingRecommendationStrategy;

namespace Sofia.WebHooksHandling.Commands
{
    public class SpreadingRecommendCommand : ICommandHandler
    {

        public bool IsMatch(string action, string[] parts, string authorAssociation, EventContext eventContext)
        {
            if (parts.Length != 2)
                return false;

            if (action != "created")
                return false;

            if (parts[0] != "@SofiaRec")
                return false;

            if (authorAssociation != "OWNER")
                return false;

            if (parts[1] != "suggest")
                return false;

            return true;
        }

        public async Task Execute(string action,string[] parts, string authorAssociation, EventContext eventContext, SofiaDbContext dbContext)
        {

            var issueNumber = (int)eventContext.WebHookEvent.GetPayload().issue.number;
            var repositoryId = (long)eventContext.WebHookEvent.GetPayload().repository.id;
            var subscription = await dbContext.Subscriptions.SingleOrDefaultAsync(q => q.RepositoryId == repositoryId);

            if (subscription == null)
            {

                var commentResponse = await eventContext.InstallationContext.Client.Issue.Comment
                    .Create(repositoryId, issueNumber, 
                    "You have not registered the repository. First, you need to ask SofiaRec to scan it before asking for suggestions.");
                return;
            }
                

            if(subscription.ScanningStatus!=SubscriptionStatus.Completed)
            {

                var commentResponse = await eventContext.InstallationContext.Client.Issue.Comment
                    .Create(repositoryId, issueNumber,
                    "SofiaRec has not yet finished scanning the repository. You can ask for suggestion once it is done.");
                return;
            }

            var installationClient = eventContext.InstallationContext.Client;
            var recommender = new CodeReviewerRecommender(dbContext);
            
            var pullRequest = await installationClient.PullRequest.Get(repositoryId, issueNumber);
            var pullRequestFiles = await installationClient.PullRequest.Files(repositoryId, issueNumber);

            var candidates = await recommender.Recommend(subscription.Id, pullRequest, pullRequestFiles);

            await SaveCandidates(dbContext,candidates,pullRequest, subscription);

            var message = GenerateMessage(candidates);

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
                RecommenderType = RecommenderType.Spreading,
                SubscriptionId = subscription.Id,
                SuggestionDateTime = dateTime
            }));

            await dbContext.SaveChangesAsync();

        }

        private string GenerateMessage(IEnumerable<Recommending.Candidate> candidates)
        {
            var message = "Sofia's suggestions are as below. Candidates are sorted in a way to maximize knowledge **_spreading_** and **_retention_**." + Environment.NewLine + Environment.NewLine; 

            message += "| Rank | Name | Knows | Learns | L-Commits | L-Review | G-Commits | G-Reviews | A-Months |" + Environment.NewLine; ;
            message += "| - | - | - | - | - | - | - | - | - |" + Environment.NewLine;

            foreach (var candidate in candidates.Cast<PersistBasedSpreadingCandidate>())
            {
                message +=  $"| {candidate.Rank} | {candidate.Contributor.GitHubLogin} | {candidate.Meta.TotalTouchedFiles} | {candidate.Meta.TotalNewFiles} | {candidate.Meta.LocalTotalCommits} | {candidate.Meta.LocalTotalReviews} | {candidate.Meta.GlobalTotalCommits} | {candidate.Meta.GlobalTotalReviews} | {candidate.Meta.TotalActiveMonths} |" + Environment.NewLine;
            }


            message += Environment.NewLine;
            message += "**Knows**: the number of pull request files the candidate knows about (commit/review)." + Environment.NewLine;
            message += "**Learns**: the number of pull request files the candidate learns about if reviews this pull request." + Environment.NewLine;
            message += "**L-Commits**: the number of commits the candidate made to the files they know about." + Environment.NewLine;
            message += "**L-Review**: the number of reviews the candidate has done to the files they know about." + Environment.NewLine;
            message += "**G-Commits**: the number commits the candidate has contributed to the project during the last year." + Environment.NewLine;
            message += "**G-Reviews**: the number reviews the candidate has contributed to the project during the last year." + Environment.NewLine;
            message += "**A-Months**: the number of months during the last year that the candidate has been active (commit/review)" + Environment.NewLine;

            return message;
        }

        
    }
}
