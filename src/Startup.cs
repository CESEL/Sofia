using System;
using System.Linq;
using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Kaftar.Core.Cqrs;
using Kaftar.Core.Cqrs.CommandStack;
using Kaftar.Core.CQRS.QueryStack.QueryHandler;
using Kaftar.Core.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Hangfire;
using Hangfire.SqlServer;
using Sofia.Data.Contexts;
using Sofia.InformationGathering.GitHub;
using Polly;
using Sofia.WebHooksHandling;
using Microsoft.Extensions.Options;
using Sofia.Jobs;
using System.Data.SqlClient;
using Octokit.Bot;
using Sofia.WebHooksHandling.Commands;
using System.Net.Http;
using Polly.Extensions.Http;
using System.Threading.Tasks;
using Seq.Extensions.Logging;
using Microsoft.Extensions.Logging;
using Sofia.Jobs.ScaningSteps;

namespace Sofia
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.Configure<GitHubOption>(Configuration.GetSection("github"));
            RepositoryCloningStep.SetBasePath(Configuration.GetValue<string>("git:baseClonePath"));


            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddSeq(Configuration.GetSection("Seq"));
            });

            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddDbContext<SofiaDbContext>(
                options => options.UseSqlServer(Configuration.GetConnectionString("SofiaConnection"),
                providerOptions => providerOptions.EnableRetryOnFailure()));

            services.AddHttpClient<GitHubRepositoryPullRequestService>()
                .AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(10, _ => TimeSpan.FromMilliseconds(600)))
                .AddPolicyHandler(GetRetryPolicy());

            services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(Configuration.GetConnectionString("HangfireConnection"), new SqlServerStorageOptions
                {
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.Zero,
                    UseRecommendedIsolationLevel = true,
                    UsePageLocksOnDequeue = true,
                    DisableGlobalLocks = true
                }));

            services.AddScoped<IssueCommentEventHandler>();
            services.AddScoped<PullRequestEventHandler>();
            services.AddScoped<PushEventHandler>();
            services.AddScoped<GitHubWebHookHandler>();

            services.AddGitHubWebHookHandler(registry=>  registry
                .RegisterHandler<IssueCommentEventHandler>("issue_comment")
                .RegisterHandler<PullRequestEventHandler>("pull_request")
                .RegisterHandler<PushEventHandler>("push"));

            CommandHandlerFactory.RegisterCommandHandlers();

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            var containerBuilder = new ContainerBuilder();
            containerBuilder.Populate(services);
            containerBuilder.RegisterModule<DefaultModule>();
            containerBuilder.RegisterModule<KaftarBootstrapModule>();

            var container = containerBuilder.Build();
            GlobalConfiguration.Configuration.UseAutofacActivator(container, false);
            return new AutofacServiceProvider(container);
        }

        static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return Policy
                .HandleResult<HttpResponseMessage>(m => m.StatusCode == System.Net.HttpStatusCode.Forbidden)
                .RetryAsync(5, onRetryAsync: async (delegateResult, retryCount) =>
               {
                   var millisecondsToWait = (int)delegateResult.Result.Headers.RetryAfter.Delta.Value.TotalMilliseconds
                   + 1500;
                   await Task.Delay(millisecondsToWait).ConfigureAwait(false);
               });
            
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
                app.UseHttpsRedirection();
            }

            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseHangfireServer();
            app.UseHangfireDashboard();

            app.UseMvc();

            RecurringJob.AddOrUpdate<ScanRepositoriesJob>(job => job.Scan(), Cron.Minutely);
            RecurringJob.AddOrUpdate<ApplyPullRequestsJob>(job => job.Apply(), Cron.Minutely);
        }

        public class DefaultModule : Autofac.Module
        {
            protected override void Load(ContainerBuilder builder)
            {
                builder.RegisterType<SofiaDbContext>().As<DbContext>()
                    .InstancePerLifetimeScope();

                builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                     .AsClosedTypesOf(typeof(ICommandHandler<,>)).AsImplementedInterfaces();

                builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                     .AsClosedTypesOf(typeof(IQueryHandler<,>)).AsImplementedInterfaces();
            }
        }
    }
}
