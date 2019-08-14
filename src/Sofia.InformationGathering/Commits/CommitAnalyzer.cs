using Diacritics.Extensions;
using Microsoft.EntityFrameworkCore;
using Sophia.Data;
using Sophia.Data.Contexts;
using Sophia.Data.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Sophia.InformationGathering
{
    public class CommitAnalyzer
    {
        private SophiaDbContext _dbContext;
        private Dictionary<string, string> _fileCanonicalMapper = new Dictionary<string, string>();
        private Dictionary<string, string> _contributorCanonicalMapper = new Dictionary<string, string>();
        private Dictionary<string, File> _files = new Dictionary<string, File>();
        private Dictionary<string, Contributor> _contributors = new Dictionary<string, Contributor>();

        private readonly Subscription _subscription;
        private readonly Octokit.GitHubClient _installationClient;

        public CommitAnalyzer(Subscription subscription, Octokit.GitHubClient installationClient, Dictionary<string, Contributor> contributors=null, Dictionary<string, string> fileCanonicalMapper = null, SophiaDbContext dbContext=null)
        {
            _fileCanonicalMapper = fileCanonicalMapper ?? new Dictionary<string, string>();
            _contributors = contributors ?? new Dictionary<string, Contributor>();

            _dbContext = dbContext;
            _subscription = subscription;
            _installationClient = installationClient;
        }

        public IReadOnlyCollection<Contributor> Contributors =>
            new ReadOnlyCollection<Contributor>(_contributors.Where(q => q.Value != null).Select(q => q.Value).ToArray());

        public async Task AnalayzeCommit(Commit commit)
        {
            var contributor = await GetContributor(commit);

            foreach (var change in commit.Changes)
            {
                var canonicalName = GetCanonicalName(change);
                AddContribution(commit, canonicalName, change, contributor);
            }

            contributor.AssignCommits(commit);
            commit.SubscriptionId = _subscription.Id;
        }

        private void AddContribution(Commit commit, string canonicalName, CommitChange change, Contributor contributor)
        {
            var file = GetFile(canonicalName);

            var contribution = new Contribution()
            {
                ActivityId = commit.Sha,
                ContributionType = ContributionType.Commit,
                ContributorEmail = commit.AuthorEmail,
                ContributorName = commit.AuthorName,
                ContributorGithubLogin = commit.AuthorGitHubLogin?? contributor.GitHubLogin,
                File = file,
                SubscriptionId = _subscription.Id,
                Contributor = contributor,
                DateTime = commit.DateTime
            };

            file.AddHistory(commit, change, contribution);
            contributor.AssignContribution(contribution);
        }

        public IEnumerable<Contributor> GetNewContributors()
        {
           return _contributors.Where(q => q.Value != null && q.Value.Id==0).Select(q => q.Value);
        }

        /*private async Task<Contributor> GetContributor(Commit commit)
        {
            string normalizedName = NormalizeName(commit.AuthorName);
            var normalizedEmail = NormalizeName(commit.AuthorEmail);

            var exisitingKeyForNormalizedName = _contributorCanonicalMapper.GetValueOrDefault(normalizedName);
            var exisitingKeyForNormalizedEmail = _contributorCanonicalMapper.GetValueOrDefault(normalizedEmail);

            var canonicalValue = "";

            if (exisitingKeyForNormalizedName == null && exisitingKeyForNormalizedEmail == null)
            {
                canonicalValue = normalizedName;
                var gitHubLogin = commit.AuthorGitHubLogin ?? await GetGitHubLoginOfAuthor(commit, _subscription.Owner, _subscription.Repo);
                var contributor = _contributors.Values.FirstOrDefault(q => q?.GitHubLogin == gitHubLogin);

                if (gitHubLogin != null && contributor != null) // for some contributors githublogin==null
                {
                    canonicalValue = contributor.CanonicalName;
                }
                else if (_contributors.ContainsKey(canonicalValue))
                {
                    return _contributors[canonicalValue];
                }
                else
                {

                    if (_dbContext != null)
                    {
                        var existingCanonicalValue = await _dbContext
                            .Contributions
                            .Where(q => q.SubscriptionId == _subscription.Id && (q.ContributorEmail == commit.AuthorEmail || q.ContributorName == commit.AuthorName))
                            .Select(q => q.Contributor.CanonicalName)
                            .FirstOrDefaultAsync();

                        if (existingCanonicalValue != null)
                        {
                            return _contributors[existingCanonicalValue];
                        }
                    }

                    _contributors[canonicalValue] = new Contributor()
                    {
                        CanonicalName = canonicalValue,
                        SubscriptionId = _subscription.Id,
                        GitHubLogin = gitHubLogin
                    };
                }
            }
            else if (exisitingKeyForNormalizedName == null || exisitingKeyForNormalizedEmail == null)
            {
                canonicalValue = exisitingKeyForNormalizedName ?? exisitingKeyForNormalizedEmail;
            }
            else if (exisitingKeyForNormalizedName == exisitingKeyForNormalizedEmail)
            {
                canonicalValue = exisitingKeyForNormalizedName;
            }
            else
            {
                canonicalValue = exisitingKeyForNormalizedName;
                var contributor = _contributors[canonicalValue];

                foreach (var kv in _contributorCanonicalMapper.ToArray())
                {
                    if (kv.Key == exisitingKeyForNormalizedEmail)
                    {
                        var shouldBeChangedContributor = _contributors[kv.Value];

                        if (shouldBeChangedContributor != null)
                        {
                            foreach (var contribution in shouldBeChangedContributor.Contributions)
                            {
                                contributor.AssignContribution(contribution);
                            }
                        }

                        _contributors.Remove()[kv.Value] = null;
                        _contributorCanonicalMapper[kv.Key] = canonicalValue;
                        _contributorCanonicalMapper[kv.Value] = canonicalValue;
                    }
                }
            }

            _contributorCanonicalMapper[normalizedName] = canonicalValue;
            _contributorCanonicalMapper[normalizedEmail] = canonicalValue;

            if (_contributors[canonicalValue] != null && _contributors[canonicalValue].GitHubLogin == null)
            {
                var gitHubLogin = await GetGitHubLoginOfAuthor(commit, _subscription.Owner, _subscription.Repo);
                _contributors[canonicalValue].GitHubLogin = gitHubLogin;
            }

            return _contributors[canonicalValue];
        }*/


        private async Task<Contributor> GetContributor(Commit commit)
        {
            Contributor contributor = null;

            string normalizedName = NormalizeName(commit.AuthorName);
            var normalizedEmail = NormalizeName(commit.AuthorEmail);

            var nameContributor = GetContributor(normalizedName);
            var emailContributor = GetContributor(normalizedEmail);

            if(nameContributor==null && emailContributor==null)
            {

                if (commit.AuthorGitHubLogin!=null)
                {
                    contributor = await GetContributorFromDb(commit, normalizedName);

                    if(contributor!=null)
                        return contributor;
                }

                var gitHubLogin = commit.AuthorGitHubLogin ?? await GetGitHubLogin(commit);
                if (gitHubLogin!=null && (contributor = GetContributor(gitHubLogin)) != null)
                {
                    _contributorCanonicalMapper[normalizedName] = contributor.CanonicalName;
                    _contributorCanonicalMapper[normalizedEmail] = contributor.CanonicalName;
                }
                else
                {
                    contributor = new Contributor()
                    {
                        CanonicalName = normalizedName,
                        SubscriptionId = _subscription.Id,
                        GitHubLogin = gitHubLogin
                    };

                    _contributors[contributor.CanonicalName] = contributor;
                    _contributorCanonicalMapper[normalizedName] = contributor.CanonicalName;
                    _contributorCanonicalMapper[normalizedEmail] = contributor.CanonicalName;

                    if (contributor.GitHubLogin != null)
                        _contributorCanonicalMapper[contributor.GitHubLogin] = contributor.CanonicalName;
                }
            }
            else if (nameContributor==null || emailContributor==null)
            {
                contributor = nameContributor ?? emailContributor;
                contributor = await GetAndMergeGitHubLogin(commit, contributor);

                _contributorCanonicalMapper[normalizedName] = contributor.CanonicalName;
                _contributorCanonicalMapper[normalizedEmail] = contributor.CanonicalName;

            }
            else if (nameContributor == emailContributor)
            {
                contributor = nameContributor;
                contributor = await GetAndMergeGitHubLogin(commit, contributor);
            }
            else if (nameContributor != emailContributor)
            {
                contributor = MergeContributors(nameContributor, emailContributor);
                contributor = await GetAndMergeGitHubLogin(commit, contributor);
            }

            return contributor;

        }

        private async Task<Contributor> GetContributorFromDb(Commit commit, string normalizedName)
        {
            Contributor contributor = _contributors.Values.SingleOrDefault(q => q.GitHubLogin == commit.AuthorGitHubLogin);
            if (contributor == null)
            {
                contributor = _contributors.Values.SingleOrDefault(q => q.CanonicalName == normalizedName);
            }

            if (contributor == null)
            {
                var canonicalName = await _dbContext
                   .Contributions
                   .Where(q => q.SubscriptionId == _subscription.Id && (q.ContributorEmail == commit.AuthorEmail || q.ContributorName == commit.AuthorName))
                   .Select(q => q.Contributor.CanonicalName)
                   .FirstOrDefaultAsync();

                if (canonicalName != null)
                    contributor = _contributors[canonicalName];
            }

            return contributor;
        }

        private async Task<Contributor> GetAndMergeGitHubLogin(Commit commit, Contributor contributor)
        {
            if (contributor.GitHubLogin == null)
            {
                Contributor gitHubContributor = null;
                var gitHubLogin = await GetGitHubLogin(commit);

                if (gitHubLogin!=null && (gitHubContributor= GetContributor(gitHubLogin)) != null)
                {
                    contributor = MergeContributors(contributor, gitHubContributor);
                }
                else if (gitHubLogin != null)
                {
                    contributor.GitHubLogin = gitHubLogin;
                    _contributorCanonicalMapper[contributor.GitHubLogin] = contributor.CanonicalName;
                }
            }

            return contributor;
        }

        private Contributor MergeContributors(Contributor nameContributor, Contributor emailContributor)
        {
            var contributions = emailContributor.Contributions.ToArray();

            foreach (var contribution in contributions)
            {
                nameContributor.AssignContribution(contribution);
            }

            emailContributor.RemoveContributions();

            var commits = emailContributor.Commits.ToArray();

            foreach (var commit in commits)
            {
                nameContributor.AssignCommits(commit);
            }

            emailContributor.RemoveCommits();

            _contributors.Remove(emailContributor.CanonicalName);

            var kvs = _contributorCanonicalMapper.Where(q=>q.Value==emailContributor.CanonicalName).ToArray();

            foreach (var kv in kvs)
            {
                _contributorCanonicalMapper[kv.Key] = nameContributor.CanonicalName;
            }

            if (emailContributor.GitHubLogin != null && nameContributor.GitHubLogin == null)
                nameContributor.GitHubLogin = emailContributor.GitHubLogin;

            return nameContributor;
        }

        private Contributor GetContributor(string identity)
        {
            var key = _contributorCanonicalMapper.GetValueOrDefault(identity);

            if (key == null)
                return null;

            return _contributors.GetValueOrDefault(key);
        }

        private async Task<string> GetGitHubLogin(Commit commit)
        {
            return commit.AuthorGitHubLogin ?? await GetGitHubLoginOfAuthor(commit, _subscription.Owner, _subscription.Repo);
        }

        private static string NormalizeName(string name)
        {
            return name
                        .Replace(" ", string.Empty)
                        .Replace(".", string.Empty)
                        .Replace("[", string.Empty)
                        .Replace("]", string.Empty)
                        .Replace("_", string.Empty)
                        .Replace("-", string.Empty)
                        .Replace("(", string.Empty)
                        .Replace(")", string.Empty)
                        .Trim()
                        .ToLower()
                        .RemoveDiacritics();
        }

        private async Task<string> GetGitHubLoginOfAuthor(Commit commit, string owner, string repo)
        {
            var gitHubCommit = await _installationClient.Repository.Commit.Get(owner, repo, commit.Sha);
            return gitHubCommit?.Author?.Login;
        }

        private File GetFile(string canonicalName)
        {
            if (!_files.ContainsKey(canonicalName))
            {
                if (_dbContext != null)
                {
                    var file = _dbContext.Files.SingleOrDefault(q=>q.CanonicalName==canonicalName);

                    if (file != null)
                    {
                        _files[canonicalName] = file;
                        return file;
                    }
                }

                // the file is new and not in the database

                _files[canonicalName] = new File()
                {
                    CanonicalName = canonicalName,
                    SubscriptionId = _subscription.Id,
                };
            }

            return _files[canonicalName];
        }

        private string GetCanonicalName(CommitChange change)
        {
            var path = change.Path;
            var oldPath = change.OldPath;

            if (change.Status == FileHistoryType.Added)
            {
                _fileCanonicalMapper[path] = path;
            }
            else if (change.OldExists.HasValue && change.OldExists.Value && oldPath != path)
            {
                _fileCanonicalMapper[path] = _fileCanonicalMapper[oldPath];
            }

            return _fileCanonicalMapper[path];
        }
    }
}
