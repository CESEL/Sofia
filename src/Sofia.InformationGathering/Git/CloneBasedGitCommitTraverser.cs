using LibGit2Sharp;
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

namespace Sofia.InformationGathering
{
    public class CloneBasedGitCommitTraverser
    {
        #region Fields

        private readonly Repository _gitRepo;
        private readonly string _branchName;
        private readonly long _subscriptionId;
        private List<Data.Commit> _commits = new List<Data.Commit>();

        #endregion

        public CloneBasedGitCommitTraverser(Subscription subscription)
        {
            _gitRepo = new Repository(subscription.LocalRepositoryPath);
            _branchName = subscription.Branch;
            _subscriptionId = subscription.Id;
        }

        public IReadOnlyCollection<Data.Commit> Commits => new ReadOnlyCollection<Data.Commit>(_commits);

        #region Public Interface

        public async Task Traverse(Func<Data.Commit,Task> analyzeFunc)
        {
            var orderedCommits = ExtractCommitsFromBranch();

            for (int i = 0; i < orderedCommits.Length; i++)
            {
                var commit = new Data.Commit()
                {
                    Sha = orderedCommits[i].Sha,
                    AuthorEmail = orderedCommits[i].Author.Email,
                    AuthorName = orderedCommits[i].Author.Name,
                    DateTime = orderedCommits[i].Author.When,
                    Changes = LoadChangesOfCommit(orderedCommits[i]).ToArray().Select(q => new Data.CommitChange()
                    {
                        Path=q.Path,
                        Status= GetFileHistoryType(q.Status),
                        OldExists=q.OldExists,
                        OldPath = q.OldPath
                        
                    }).ToArray(),
                    SubscriptionId=_subscriptionId
                };

                _commits.Add(commit);

                await analyzeFunc(commit);
            }
        }

        #endregion

        private Commit[] ExtractCommitsFromBranch()
        {
            var branch = _gitRepo.Branches[_branchName];

            var filter = new CommitFilter
            {
                SortBy = CommitSortStrategies.Topological
                | CommitSortStrategies.Time
                | CommitSortStrategies.Reverse,
                IncludeReachableFrom = branch
            };

            return _gitRepo.Commits.QueryBy(filter).ToArray();
        }

        private FileHistoryType GetFileHistoryType(ChangeKind status)
        {
            if (status == ChangeKind.Added)
                return FileHistoryType.Added;


            if (status == ChangeKind.Copied)
                return FileHistoryType.Copied;


            if (status == ChangeKind.Deleted)
                return FileHistoryType.Deleted;


            if (status == ChangeKind.Modified)
                return FileHistoryType.Modified;


            if (status == ChangeKind.Renamed)
                return FileHistoryType.Renamed;

            return FileHistoryType.Unknown;
        }

        private IEnumerable<TreeEntryChanges> LoadChangesOfCommit(Commit commit)
        {
            var compareOptions = new CompareOptions
            {
                Algorithm = DiffAlgorithm.Minimal,
                Similarity = new SimilarityOptions
                {
                    RenameDetectionMode = RenameDetectionMode.Renames,
                }
            };

            if (commit.Parents.Count() <= 1)
            {
                return GetDiffOfTrees(_gitRepo, commit.Parents.SingleOrDefault()?.Tree, commit.Tree, compareOptions);
            }
            else
            {
                return GetDiffOfMergedTrees(_gitRepo, commit.Parents, commit.Tree, compareOptions);
            }
        }

        private IEnumerable<TreeEntryChanges> GetDiffOfMergedTrees(Repository gitRepo, IEnumerable<Commit> parents, Tree tree, CompareOptions compareOptions)
        {
            var firstParent = parents.ElementAt(0);
            var secondParent = parents.ElementAt(1);

            var firstChanges = GetDiffOfTrees(gitRepo, firstParent.Tree, tree, compareOptions);
            var secondChanges = GetDiffOfTrees(gitRepo, secondParent.Tree, tree, compareOptions);

            return firstChanges.Where(c1 => secondChanges.Any(c2 => c2.Oid == c1.Oid));
        }

        private IEnumerable<TreeEntryChanges> GetDiffOfTrees(Repository repo, Tree oldTree,Tree newTree, CompareOptions compareOptions)
        {
            return repo.Diff.Compare<TreeChanges>(oldTree, newTree, compareOptions).Where(q => q.Status != ChangeKind.Unmodified);
        }
    }
}
