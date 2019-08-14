using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Sophia.Data.Models;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Sophia.Data.Contexts
{
    public class SophiaDbContext :DbContext
    {
        public SophiaDbContext(DbContextOptions<SophiaDbContext> options) : base(options)
        {
            Database.SetCommandTimeout(TimeSpan.FromHours(2));
        }

        public DbSet<Subscription> Subscriptions { get; set; }

        public DbSet<Commit> Commits { get; set; }

        public DbSet<SubscriptionEvent> SubscriptionEvents { get; set; }

        public DbSet<Contributor> Contributors { get; set; }

        public DbSet<Contribution> Contributions { get; set; }

        public DbSet<File> Files { get; set; }

        public DbSet<FileHistory> FileHistories { get; set; }

        public DbSet<PullRequest> PullRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Subscription>()
                .HasAlternateKey(k => new { k.RepositoryId, k.Branch });

            modelBuilder.Entity<PullRequest>()
                .Property(b => b.PullRequestInfo)
                .HasConversion(
                    v => JsonConvert.SerializeObject(v),
                    v => JsonConvert.DeserializeObject<PullRequestInfo>(v));

            modelBuilder.Entity<FileHistory>()
                .Property(b => b.FileHistoryType)
                .HasConversion<string>();

            modelBuilder.Entity<FileHistory>()
                .HasIndex(b => b.FileId);

            modelBuilder.Entity<Contribution>()
                .Property(b => b.ContributionType)
                .HasConversion<string>();

            modelBuilder.Entity<Contribution>()
                .HasIndex(b => b.ActivityId);

            modelBuilder.Entity<Contribution>()
                .HasIndex(b => b.FileId);

            modelBuilder.Entity<Contribution>()
                .HasIndex(b => b.ContributorId);

            modelBuilder.Entity<Contributor>()
                .HasIndex(b => b.CanonicalName);

            modelBuilder.Entity<Contributor>()
                .HasIndex(b => b.SubscriptionId);

            modelBuilder.Entity<PullRequest>()
                .Property(p => p.PullRequestAnalyzeStatus)
                .HasConversion<string>();

            modelBuilder.Entity<PullRequest>()
                .HasIndex(p => p.SubscriptionId);

            modelBuilder.Entity<PullRequest>()
                .HasIndex(p => p.Number);

            modelBuilder.Entity<Subscription>()
                .Property(p => p.ScanningStatus)
                .HasConversion<string>();

            modelBuilder.Entity<Candidate>()
                .Property(p => p.RecommenderType)
                .HasConversion<string>();

            modelBuilder.Entity<Candidate>()
                .HasIndex(p => p.PullRequestNumber);

            modelBuilder.Entity<Candidate>()
                .HasIndex(p => p.SubscriptionId);

            modelBuilder.Entity<Commit>().Ignore(p => p.Changes);
            modelBuilder.Entity<Commit>().Ignore(p => p.AuthorEmail);
            modelBuilder.Entity<Commit>().Ignore(p => p.AuthorGitHubLogin);
            modelBuilder.Entity<Commit>().Ignore(p => p.AuthorName);
        }

    }
}
