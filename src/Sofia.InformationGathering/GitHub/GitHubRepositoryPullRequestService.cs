using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Sophia.Data.Models;
using System.Net;

namespace Sophia.InformationGathering.GitHub
{
    public class GitHubRepositoryPullRequestService
    {
        private HttpClient Client { get; }

        public GitHubRepositoryPullRequestService(HttpClient client)
        {
            client.BaseAddress = new Uri("https://api.github.com");
            client.DefaultRequestHeaders.Add("User-Agent", "SophiaApp");
            Client = client;
        }

        public async Task<GetPullRequestsOfRepositoryQueryResponse> GetPullRequestsOfRepository(string token, string owner, string repo,string cursor)
        {
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            var query = GitHubAllPullRequestGraphQLQuery(owner, repo, cursor);
            var response = await Execute(query, ParseGetPullRequestsOfRepositoryQueryResponse);

            return response;
        }

        public async Task<PullRequestInfo> GetPullRequest(string token, int pullRequestnumber, string owner, string repo)
        {
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var query = GitHubPullRequestGraphQLQuery(pullRequestnumber, owner, repo);
            var response = await Execute(query, ParseGetPullRequestQueryResponse);

            return response.PullRequestInfo;
        }

        private async Task<TParseType> Execute<TParseType>(PullRequestQuery pullRequestQuery,Func<JsonTextReader, TParseType> parser)
        {
            var jsonQuery = JsonConvert.SerializeObject(pullRequestQuery);

            using (var content = new StringContent(jsonQuery, Encoding.UTF8, "application/json"))
            {
                using (var response = await Client.PostAsync("/graphql", content))
                {
                    if (response.StatusCode==HttpStatusCode.Unauthorized)
                    {
                        throw new GitHubUnauthorizedException();
                    }

                    using (var responseStream = await response.Content.ReadAsStreamAsync())
                    {
                        using (var sr = new StreamReader(responseStream))
                        {
                            using (var reader = new JsonTextReader(sr))
                            {
                                return parser(reader);
                            }
                        }
                    }
                }
            }
        }

        private GetPullRequestsOfRepositoryQueryResponse ParseGetPullRequestsOfRepositoryQueryResponse(JsonTextReader reader)
        {
            var response = new GetPullRequestsOfRepositoryQueryResponse();

            JsonSerializer serializer = new JsonSerializer();
            var rawPullRequests = serializer.Deserialize<JObject>(reader);

            try
            {
                response.Cursor = rawPullRequests["data"]["repository"]["pullRequests"]["pageInfo"]["endCursor"].Value<string>();
                response.HasNextPage = rawPullRequests["data"]["repository"]["pullRequests"]["pageInfo"]["hasNextPage"].Value<bool>();

                var edges = rawPullRequests["data"]["repository"]["pullRequests"]["edges"];

                var rawPullRequest = edges.First;

                while (rawPullRequest != null)
                {
                    ParseRecord(response, rawPullRequest);
                    rawPullRequest = rawPullRequest.Next;
                }

            }
            catch (Exception e)
            {

                throw;
            }

            return response;
        }

        private static void ParseRecord(GetPullRequestsOfRepositoryQueryResponse response, JToken rawPullRequest)
        {
            try
            {

                var pullRequest = new PullRequestInfo();
                pullRequest.Number = rawPullRequest["node"].Value<int>("number");
                pullRequest.TotalPullRequestFiles = rawPullRequest["node"]["files"]["totalCount"].Value<int>();
                pullRequest.SubmitterLogin = rawPullRequest["node"]["author"].HasValues ? rawPullRequest["node"]["author"]["login"].ToString() : null;
                pullRequest.MergedDateTime = rawPullRequest["node"].Value<DateTime?>("mergedAt");
                pullRequest.MergeCommitSha = rawPullRequest["node"]["mergeCommit"].HasValues ? rawPullRequest["node"]["mergeCommit"]["oid"].ToString() : null;

                pullRequest.PullRequestReviewers = rawPullRequest["node"]["reviews"]["nodes"].Children().Select(c => new PullRequestReviewer()
                {
                    Login = c["author"].HasValues ? c["author"]["login"].Value<string>() : null,
                }).Distinct().ToArray();

                pullRequest.PullRequestFiles = rawPullRequest["node"]["files"]["nodes"].Children().Select(c => new PullRequestFile()
                {
                    Path = c.Value<string>("path"),
                    Additions = c.Value<int>("additions"),
                    Deletions = c.Value<int>("deletions"),
                }).ToArray();

                response.AddPullRequestInfo(pullRequest);
                rawPullRequest = rawPullRequest.Next;
            }
            catch (Exception e)
            {
                // TODO 
            }
        }

        private GetPullRequestQueryResponse ParseGetPullRequestQueryResponse(JsonTextReader reader)
        {
            var response = new GetPullRequestQueryResponse();

            var serializer = new JsonSerializer();
            var rawPullRequest = serializer.Deserialize<JObject>(reader)["data"]["repository"]["pullRequest"];

            try
            {
                var pullRequest = new PullRequestInfo();
                pullRequest.TotalPullRequestFiles = rawPullRequest["files"]["totalCount"].Value<int>();
                pullRequest.SubmitterLogin = rawPullRequest["author"].HasValues ? rawPullRequest["author"]["login"].ToString() : null;
                pullRequest.MergedDateTime = rawPullRequest.Value<DateTime?>("mergedAt");
                pullRequest.MergeCommitSha = rawPullRequest["mergeCommit"].HasValues ? rawPullRequest["mergeCommit"]["oid"].ToString():null;
                pullRequest.Number = rawPullRequest.Value<int>("number");

                pullRequest.PullRequestReviewers = rawPullRequest["reviews"]["nodes"].Children().Select(c => new PullRequestReviewer()
                {
                    Login = c["author"].HasValues ? c["author"]["login"].Value<string>() : null,
                }).Distinct().ToArray();

                pullRequest.PullRequestFiles = rawPullRequest["files"]["nodes"].Children().Select(c => new PullRequestFile()
                {
                    Path = c.Value<string>("path"),
                    Additions = c.Value<int>("additions"),
                    Deletions = c.Value<int>("deletions"),
                }).ToArray();

                response.PullRequestInfo = pullRequest;
                return response;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        private PullRequestQuery GitHubPullRequestGraphQLQuery(int pullRequestnumber,string owner, string repo)
        {
            
            var query = $@"query  {{
                         repository(owner: ""{ owner }"", name: ""{ repo }"") {{
                            pullRequest(number: {pullRequestnumber}) {{
                                author {{
                                    login
                                }}
                                number
                                mergedAt
                                mergeCommit{{
                                    oid
                                }}
                                reviews(first: 100) {{
                                nodes {{
                                    author {{
                                        login
                                    }}
                                }}
                            }}
                            files(first: 50) {{
                                nodes {{
                                    additions
                                    deletions
                                    path
                                }}
                                totalCount
                            }}
                        }}
                    }}
                }}";

            return new PullRequestQuery()
            {
                Query = query.Replace(Environment.NewLine, string.Empty)
            };
        }

        private PullRequestQuery GitHubAllPullRequestGraphQLQuery(string owner, string repo, string cursor=null)
        {
            var afterCursor = "";

            if (cursor == null)
            {
                afterCursor = $@"after: null";
            }
            else
            {
                afterCursor = $@"after:""{cursor}""";
            }

            var query =  $@"query  {{
                        repository(owner: ""{owner}"", name: ""{repo}"") {{
                            pullRequests(first: 100, states: MERGED, {afterCursor},orderBy:{{direction:ASC,field:CREATED_AT}}) {{
                                pageInfo{{
                                    hasNextPage,
                                    endCursor
                                }}
                                edges {{
                                    node {{
                                        author {{
                                            login
                                        }}
                                        number
                                        mergedAt
                                        mergeCommit{{
                                            oid
                                        }}
                                        reviews(first: 100){{
                                            nodes{{
                                                 author {{
                                                    login
                                                }}
                                            }}
                                        }}
                                        files(first: 50) {{
                                            nodes {{
                                                additions
                                                deletions
                                                path
                                            }}
                                            totalCount
                                    }}
                                }}
                            }}
                        }}
                    }}
                }}";

            return new PullRequestQuery()
            {
                Query = query.Replace(Environment.NewLine,string.Empty)
            };
        }
    }

    class PullRequestQuery
    {
        [JsonProperty("query")]
        public string Query { get; set; }
    }

    public class GetPullRequestsOfRepositoryQueryResponse
    {
        private List<PullRequestInfo> _pullRequests = new List<PullRequestInfo>();
        public IReadOnlyCollection<PullRequestInfo> PullRequests => _pullRequests.AsReadOnly();
        public string Cursor { get; set; }
        public bool HasNextPage { get; set; }
        public void AddPullRequestInfo(PullRequestInfo pullRequestInfo)
        {
            _pullRequests.Add(pullRequestInfo);
        }
    }

    class GetPullRequestQueryResponse
    {
        public PullRequestInfo PullRequestInfo { get; set; }
    }

    public class GitHubUnauthorizedException : Exception
    {

    }
}
