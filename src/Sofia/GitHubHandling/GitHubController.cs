using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using GitHubJwt;
using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Octokit.Bot;
using Sophia.Jobs;

namespace Sophia.GitHubHandling
{
    [ApiController]
    [Route("github")]
    public class GitHubController : ControllerBase
    {
        private readonly ILogger<GitHubController> _logger;
        private readonly GitHubWebHookHandler _gitHubWebHookHandler;

        public GitHubController(GitHubWebHookHandler gitHubWebHookHandler,ILogger<GitHubController> logger)
        {
            _logger = logger;
            _gitHubWebHookHandler = gitHubWebHookHandler;
        }

        [HttpPost("hooks")]
        public async Task<IActionResult> GenerateJWTToken(WebHookEvent webHookEvent)
        {
            await _gitHubWebHookHandler.Handle(webHookEvent);

            return Ok();
        }
    }
}