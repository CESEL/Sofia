
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using RelationalGit;
using Sofia.Data.Models;
using System.Collections.ObjectModel;
using Octokit.Bot;
using Sofia.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using Sofia.Data;

namespace Sofia.InformationGathering
{
    public class GitHubBasedGitCommitTraverser
    {

        #region Fields

        private readonly SofiaDbContext _sofiaDbContext;
        private readonly string[] _commitShas;
        private readonly Subscription _subscription;

        #endregion

        public GitHubBasedGitCommitTraverser(SofiaDbContext dbContext,Subscription subscription, string[] commitShas)
        {
            _sofiaDbContext = dbContext;
            _commitShas = commitShas;
            _subscription = subscription;
        }

        public async Task Traverse(EventContext eventContext,long repositoryId)
        {
            var sortedCommits = await SortCommits(eventContext, repositoryId);
            await AnalyzeCommits(eventContext, sortedCommits);

            _sofiaDbContext.SaveChanges();
        }

        private async Task AnalyzeCommits(EventContext eventContext, Data.Commit[] sortedCommits)
        {
            var contributors = await GetContributors(_subscription.Id);
            var fileCanonicalMapper = await GetFileCanonicalMapper(_subscription.Id);
            var commitAnalyzer = new CommitAnalyzer(_subscription,
                eventContext.InstallationContext.Client,
                contributors,
                fileCanonicalMapper,
                _sofiaDbContext);

            foreach (var commit in sortedCommits)
            {
                await commitAnalyzer.AnalayzeCommit(commit);
            }

            await _sofiaDbContext.AddRangeAsync(commitAnalyzer.GetNewContributors());
            await _sofiaDbContext.AddRangeAsync(sortedCommits);
        }

        private async Task<Data.Commit[]> SortCommits(EventContext eventContext, long repositoryId)
        {
            var dicCommits = new Dictionary<string, CommitNode>();

            for (int i = 0; i < _commitShas.Length; i++)
            {
                await AddCommitToTree(eventContext, dicCommits, _subscription.Id, repositoryId, _commitShas[i],null);
            }

            var sortedCommits = TopologicalSort(dicCommits);
            return sortedCommits;
        }

        private async Task AddCommitToTree(EventContext eventContext,Dictionary<string, CommitNode> dicCommits, long subscriptionId, long repositoryId, string commitSha, CommitNode childNode)
        {
            if (dicCommits.ContainsKey(commitSha))
            {                
                return;
            }

            bool isAlreadyAnalyzed = IsCommitAlreadyAnalyzed(subscriptionId, commitSha);

            if (isAlreadyAnalyzed)
            {
                return;
            }

            var githubCommit = await eventContext.InstallationContext.Client.Repository.Commit.Get(repositoryId, commitSha);
            var commitNode = new CommitNode(githubCommit);

            if (childNode!=null)
            {
                commitNode.IsRoot = false;
                dicCommits[childNode.GitHubCommit.Sha].Parents.Add(dicCommits[githubCommit.Sha]);
            }

            foreach (var parent in githubCommit.Parents)
            {
                if (!dicCommits.ContainsKey(parent.Sha))
                {
                    await AddCommitToTree(eventContext, dicCommits, subscriptionId, repositoryId, parent.Sha, dicCommits[githubCommit.Sha]);
                }
                else
                {
                    dicCommits[parent.Sha].IsRoot = false;
                    dicCommits[commitNode.GitHubCommit.Sha].Parents.Add(dicCommits[parent.Sha]);
                }
            }
        }

        private bool IsCommitAlreadyAnalyzed(long subscriptionId, string commitSha)
        {
            return _sofiaDbContext
                            .Contributions
                            .Any(q => q.ActivityId == commitSha && q.SubscriptionId == subscriptionId && q.ContributionType == ContributionType.Commit);
        }

        private async Task<Dictionary<string, Data.Models.Contributor>> GetContributors(long subscriptionId)
        {
            return await _sofiaDbContext.Contributors
                 .Where(q => q.SubscriptionId == subscriptionId)
                 .ToDictionaryAsync(q => q.CanonicalName);
        }

        private async Task<Dictionary<string, string>> GetFileCanonicalMapper(long subscriptionId)
        {
            var fileCanonicalMapper = new Dictionary<string, string>();

            var fileHistories = await _sofiaDbContext.FileHistories
                .AsNoTracking()
                .Include(q => q.File)
                .Where(q => q.SubscriptionId == subscriptionId)
                .OrderBy(q => q.Id)
                .ToArrayAsync();

            foreach (var fileHistory in fileHistories)
            {
                fileCanonicalMapper[fileHistory.Path] = fileHistory.File.CanonicalName;
            }

            return fileCanonicalMapper;
        }

        private Commit[] TopologicalSort(Dictionary<string, CommitNode> dicCommits)
        {
            var root = dicCommits.Single(q => q.Value.IsRoot);
            var sortedCommits = new List<Octokit.GitHubCommit>();
            var visitedNodes = new HashSet<string>();

            TopologicalSort(root.Value, sortedCommits, visitedNodes);

            return sortedCommits.Select(q => new Commit()
            {
                AuthorEmail = q.Commit.Author?.Email,
                AuthorName = q.Commit.Author?.Name,
                AuthorGitHubLogin = q.Author?.Login,
                DateTime = q.Commit.Author.Date,
                Sha = q.Sha,
                Changes = q.Files.Select(f => new CommitChange()
                {
                    Path = f.Filename,
                    OldPath = f.PreviousFileName,
                    OldExists = f.PreviousFileName != null,
                    Status = GetStatus(f.Status)
                }).ToArray()
            }).ToArray();
        }

        private FileHistoryType GetStatus(string status)
        {
            if (status == "renamed")
                return FileHistoryType.Renamed;

            if (status == "modified")
                return FileHistoryType.Modified;

            if (status == "removed")
                return FileHistoryType.Deleted;

            if (status == "added")
                return FileHistoryType.Added;

            return FileHistoryType.Unknown;
        }

        private void TopologicalSort(CommitNode node, List<Octokit.GitHubCommit> sortedCommits, HashSet<string> visitedNodes)
        {
            if (visitedNodes.Contains(node.GitHubCommit.Sha))
                return;

            visitedNodes.Add(node.GitHubCommit.Sha);

            foreach (var parent in node.Parents)
            {
                TopologicalSort(parent, sortedCommits, visitedNodes);
            }

            sortedCommits.Add(node.GitHubCommit);
        }
    }

    internal class CommitNode
    {

        public CommitNode(Octokit.GitHubCommit githubCommit)
        {
            IsRoot = true;
            GitHubCommit = githubCommit;
        }

        public List<CommitNode> Parents { get; set; } = new List<CommitNode>();

        public bool IsRoot { get; set; }

        public Octokit.GitHubCommit GitHubCommit { get; set; }
    }
}
