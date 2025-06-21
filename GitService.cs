using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ClaudeAI
{
    /// <summary>
    /// Service for Git repository integration and context gathering
    /// </summary>
    public static class GitService
    {
        /// <summary>
        /// Gets comprehensive Git repository information for the current solution
        /// </summary>
        public static GitRepositoryInfo GetRepositoryInfo()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var dte = ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as DTE;
                if (dte?.Solution == null)
                {
                    return new GitRepositoryInfo { ErrorMessage = "No solution is currently open." };
                }

                var solutionPath = Path.GetDirectoryName(dte.Solution.FullName);
                var gitRepoPath = FindGitRepository(solutionPath);

                if (string.IsNullOrEmpty(gitRepoPath))
                {
                    return new GitRepositoryInfo { ErrorMessage = "No Git repository found in solution directory or parent directories." };
                }

                var repoInfo = new GitRepositoryInfo
                {
                    RepositoryPath = gitRepoPath,
                    SolutionPath = solutionPath
                };

                // Gather Git information
                PopulateBasicInfo(repoInfo);
                PopulateBranchInfo(repoInfo);
                PopulateRecentCommits(repoInfo);
                PopulateWorkingDirectoryStatus(repoInfo);
                PopulateRemoteInfo(repoInfo);

                return repoInfo;
            }
            catch (Exception ex)
            {
                return new GitRepositoryInfo { ErrorMessage = $"Error accessing Git repository: {ex.Message}" };
            }
        }

        /// <summary>
        /// Gets Git context summary for Claude
        /// </summary>
        public static string GetGitContext()
        {
            var repoInfo = GetRepositoryInfo();

            if (!string.IsNullOrEmpty(repoInfo.ErrorMessage))
            {
                return $"Git Status: {repoInfo.ErrorMessage}";
            }

            var context = new StringBuilder();
            context.AppendLine("=== GIT REPOSITORY CONTEXT ===");
            context.AppendLine($"Repository: {Path.GetFileName(repoInfo.RepositoryPath)}");
            context.AppendLine($"Current Branch: {repoInfo.CurrentBranch}");

            if (repoInfo.RemoteUrl != null)
            {
                context.AppendLine($"Remote: {repoInfo.RemoteUrl}");
            }

            if (repoInfo.WorkingDirectoryStatus?.Count > 0)
            {
                context.AppendLine("\nWorking Directory Changes:");
                foreach (var change in repoInfo.WorkingDirectoryStatus.Take(10))
                {
                    context.AppendLine($"  {change.Status}: {change.FilePath}");
                }
                if (repoInfo.WorkingDirectoryStatus.Count > 10)
                {
                    context.AppendLine($"  ... and {repoInfo.WorkingDirectoryStatus.Count - 10} more files");
                }
            }

            if (repoInfo.RecentCommits?.Count > 0)
            {
                context.AppendLine("\nRecent Commits:");
                foreach (var commit in repoInfo.RecentCommits.Take(5))
                {
                    context.AppendLine($"  {commit.ShortHash}: {commit.Message} ({commit.Author}, {commit.Date:yyyy-MM-dd})");
                }
            }

            if (repoInfo.Branches?.Count > 1)
            {
                context.AppendLine($"\nOther Branches: {string.Join(", ", repoInfo.Branches.Where(b => b != repoInfo.CurrentBranch).Take(5))}");
            }

            context.AppendLine("=== END GIT CONTEXT ===\n");

            return context.ToString();
        }

        /// <summary>
        /// Gets files changed in recent commits (useful for understanding recent work)
        /// </summary>
        public static List<string> GetRecentlyChangedFiles(int commitCount = 10)
        {
            var repoInfo = GetRepositoryInfo();
            if (!string.IsNullOrEmpty(repoInfo.ErrorMessage))
                return new List<string>();

            try
            {
                var output = ExecuteGitCommand(repoInfo.RepositoryPath, $"log --name-only --pretty=format: -n {commitCount}");
                return output.Split('\n')
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Distinct()
                    .ToList();
            }
            catch (Exception)
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// Gets the diff for a specific file
        /// </summary>
        public static string GetFileDiff(string filePath, bool staged = false)
        {
            var repoInfo = GetRepositoryInfo();
            if (!string.IsNullOrEmpty(repoInfo.ErrorMessage))
                return "";

            try
            {
                var command = staged ? $"diff --cached \"{filePath}\"" : $"diff \"{filePath}\"";
                return ExecuteGitCommand(repoInfo.RepositoryPath, command);
            }
            catch (Exception)
            {
                return "";
            }
        }

        private static string FindGitRepository(string startPath)
        {
            var currentPath = startPath;

            while (!string.IsNullOrEmpty(currentPath))
            {
                var gitPath = Path.Combine(currentPath, ".git");
                if (Directory.Exists(gitPath) || File.Exists(gitPath))
                {
                    return currentPath;
                }

                var parentPath = Path.GetDirectoryName(currentPath);
                if (parentPath == currentPath) // Reached root
                    break;

                currentPath = parentPath;
            }

            return null;
        }

        private static void PopulateBasicInfo(GitRepositoryInfo repoInfo)
        {
            try
            {
                // Get current branch
                repoInfo.CurrentBranch = ExecuteGitCommand(repoInfo.RepositoryPath, "branch --show-current").Trim();

                // Get all branches
                var branchOutput = ExecuteGitCommand(repoInfo.RepositoryPath, "branch -a");
                repoInfo.Branches = branchOutput.Split('\n')
                    .Select(b => b.Trim().TrimStart('*').Trim())
                    .Where(b => !string.IsNullOrWhiteSpace(b) && !b.StartsWith("remotes/"))
                    .ToList();
            }
            catch (Exception ex)
            {
                repoInfo.ErrorMessage = $"Error getting basic Git info: {ex.Message}";
            }
        }

        private static void PopulateBranchInfo(GitRepositoryInfo repoInfo)
        {
            try
            {
                // Get tracking branch
                var trackingBranch = ExecuteGitCommand(repoInfo.RepositoryPath, "rev-parse --abbrev-ref @{upstream}");
                if (!string.IsNullOrWhiteSpace(trackingBranch))
                {
                    repoInfo.TrackingBranch = trackingBranch.Trim();
                }

                // Get ahead/behind count
                if (!string.IsNullOrEmpty(repoInfo.TrackingBranch))
                {
                    var aheadBehind = ExecuteGitCommand(repoInfo.RepositoryPath, $"rev-list --left-right --count {repoInfo.TrackingBranch}...HEAD");
                    var parts = aheadBehind.Trim().Split('\t');
                    if (parts.Length == 2)
                    {
                        int behindCount, aheadCount;
                        if (int.TryParse(parts[0], out behindCount))
                            repoInfo.BehindCount = behindCount;
                        if (int.TryParse(parts[1], out aheadCount))
                            repoInfo.AheadCount = aheadCount;
                    }
                }
            }
            catch (Exception)
            {
                // Continue without tracking info
            }
        }

        private static void PopulateRecentCommits(GitRepositoryInfo repoInfo)
        {
            try
            {
                var output = ExecuteGitCommand(repoInfo.RepositoryPath, "log --oneline --pretty=format:\"%H|%h|%an|%ad|%s\" --date=short -n 10");
                repoInfo.RecentCommits = new List<GitCommitInfo>();

                foreach (var line in output.Split('\n'))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var parts = line.Trim('"').Split('|');
                    if (parts.Length >= 5)
                    {
                        repoInfo.RecentCommits.Add(new GitCommitInfo
                        {
                            Hash = parts[0],
                            ShortHash = parts[1],
                            Author = parts[2],
                            Date = DateTime.TryParse(parts[3], out var date) ? date : DateTime.MinValue,
                            Message = string.Join("|", parts.Skip(4))
                        });
                    }
                }
            }
            catch (Exception)
            {
                repoInfo.RecentCommits = new List<GitCommitInfo>();
            }
        }

        private static void PopulateWorkingDirectoryStatus(GitRepositoryInfo repoInfo)
        {
            try
            {
                var output = ExecuteGitCommand(repoInfo.RepositoryPath, "status --porcelain");
                repoInfo.WorkingDirectoryStatus = new List<GitFileStatus>();

                foreach (var line in output.Split('\n'))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var statusCode = line.Substring(0, 2);
                    var filePath = line.Substring(3);

                    repoInfo.WorkingDirectoryStatus.Add(new GitFileStatus
                    {
                        Status = GetStatusDescription(statusCode),
                        FilePath = filePath,
                        StatusCode = statusCode
                    });
                }
            }
            catch (Exception)
            {
                repoInfo.WorkingDirectoryStatus = new List<GitFileStatus>();
            }
        }

        private static void PopulateRemoteInfo(GitRepositoryInfo repoInfo)
        {
            try
            {
                var remotes = ExecuteGitCommand(repoInfo.RepositoryPath, "remote -v");
                var lines = remotes.Split('\n');

                foreach (var line in lines)
                {
                    if (line.Contains("origin") && line.Contains("(fetch)"))
                    {
                        var parts = line.Split('\t');
                        if (parts.Length >= 2)
                        {
                            repoInfo.RemoteUrl = parts[1].Replace("(fetch)", "").Trim();
                            break;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Continue without remote info
            }
        }

        private static string GetStatusDescription(string statusCode)
        {
            switch (statusCode)
            {
                case "M ": return "Modified";
                case " M": return "Modified (unstaged)";
                case "A ": return "Added";
                case " A": return "Added (unstaged)";
                case "D ": return "Deleted";
                case " D": return "Deleted (unstaged)";
                case "R ": return "Renamed";
                case " R": return "Renamed (unstaged)";
                case "C ": return "Copied";
                case " C": return "Copied (unstaged)";
                case "U ": return "Unmerged";
                case " U": return "Unmerged (unstaged)";
                case "??": return "Untracked";
                case "!!": return "Ignored";
                default: return statusCode.Trim();
            }
        }

        private static string ExecuteGitCommand(string workingDirectory, string arguments)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = System.Diagnostics.Process.Start(startInfo))
                {
                    var output = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();

                    process.WaitForExit();

                    if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
                    {
                        throw new Exception($"Git command failed: {error}");
                    }

                    return output;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to execute git command '{arguments}': {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Git repository information
    /// </summary>
    public class GitRepositoryInfo
    {
        public string RepositoryPath { get; set; }
        public string SolutionPath { get; set; }
        public string CurrentBranch { get; set; }
        public string TrackingBranch { get; set; }
        public List<string> Branches { get; set; } = new List<string>();
        public List<GitCommitInfo> RecentCommits { get; set; } = new List<GitCommitInfo>();
        public List<GitFileStatus> WorkingDirectoryStatus { get; set; } = new List<GitFileStatus>();
        public string RemoteUrl { get; set; }
        public int AheadCount { get; set; }
        public int BehindCount { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Git commit information
    /// </summary>
    public class GitCommitInfo
    {
        public string Hash { get; set; }
        public string ShortHash { get; set; }
        public string Author { get; set; }
        public DateTime Date { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// Git file status information
    /// </summary>
    public class GitFileStatus
    {
        public string Status { get; set; }
        public string FilePath { get; set; }
        public string StatusCode { get; set; }
    }
}