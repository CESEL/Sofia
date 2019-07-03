using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Sofia.Models
{
    public class RepositoryUrl
    {
        readonly string _branchPattern = @"https://github.com/(?<owner>[A-Za-z0-9.-]+)/(?<repo>[A-Za-z0-9.-]+)/tree/(?<branch>[A-Za-z0-9.-]+)/?";
        readonly string _pattern = @"https://github.com/(?<owner>[A-Za-z0-9.-]+)/(?<repo>[A-Za-z0-9.-]+)/?";
        public RepositoryUrl(string url)
        {
            if (IsBranchUrl(url))
            {
                var matche = Regex.Matches(url, _branchPattern)[0];

                Owner = matche.Groups["owner"].Value;
                Branch = matche.Groups["branch"]?.Value;
                Repo = matche.Groups["repo"].Value;
            }
            else
            {
                var matche = Regex.Matches(url, _pattern)[0];

                Owner = matche.Groups["owner"].Value;
                Branch = "master";
                Repo = matche.Groups["repo"].Value;
            }
           
        }

        private bool IsBranchUrl(string url)
        {
            return url.Contains("/tree/");
        }

        public string Branch { get; set; }
        public string Owner { get; set; }
        public string Repo { get; set; }

        public static implicit operator string(RepositoryUrl url)
        {
            return $@"https://github.com/{url.Owner}/{url.Repo}/tree/{url.Branch}";
        }
    }
}
