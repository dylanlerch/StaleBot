using Octokit;

namespace StaleBot
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var pat = await File.ReadAllTextAsync("pat.txt");
            var owner = await File.ReadAllTextAsync("user.txt");
            var repo = await File.ReadAllTextAsync("repo.txt");

            var client = new GitHubClient(new ProductHeaderValue("StaleBot"))
            {
                Credentials = new Credentials(pat)
            };

            var repository = await client.Repository.Get(owner, repo);
            var openPullRequests = await client
                .Repository
                .PullRequest
                .GetAllForRepository(owner, repo, new PullRequestRequest {State = ItemStateFilter.Open});

            foreach (var pullRequest in openPullRequests)
            {
                var currentBase = await client.Repository.Commit.Get(owner, repo, $"refs/heads/{pullRequest.Base.Ref}");
                var difference = await client.Repository.Commit.Compare(owner, repo, pullRequest.Head.Sha, currentBase.Sha);

                var pullRequestCommitsInDifference = new List<GitHubCommit>();

                foreach (var commit in difference.Commits)
                {
                    if (await IsPrCommit(repository, client, commit))
                    {
                        pullRequestCommitsInDifference.Add(commit);
                    }
                }
            }
        }

        static async Task<bool> IsPrCommit(Repository repo, GitHubClient client, GitHubCommit commit)
        {
            if (commit.Parents.Count == 2)
            {
                var results = await client.Search.SearchIssues(new SearchIssuesRequest
                {
                    Type = IssueTypeQualifier.PullRequest,
                    
                    Parameters =
                    {
                        // { "repository_id", repo.Id.ToString() },
                        // { "q", commit.Sha }
                    },
                });

                return results.TotalCount > 0;
            }

            return false;
        }
    }
}