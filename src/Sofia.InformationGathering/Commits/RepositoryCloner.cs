using Hangfire;
using LibGit2Sharp;
using Sofia.Data.Contexts;
using Sofia.Data.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Sofia.InformationGathering
{
    public class RepositoryCloner
    {
        public RepositoryCloner()
        {
        }

        public void Clone(string repositoryLocalPath, string gitHubRepositoryUrl)
        {
            Repository.Clone(gitHubRepositoryUrl, repositoryLocalPath);
        }
        
    }
}
