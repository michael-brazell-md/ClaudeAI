using Microsoft.VisualStudio.Shell;
using System;
using System.Linq;

namespace ClaudeAI
{
    /// <summary>
    /// Handles code analysis operations
    /// </summary>
    public class CodeAnalyzer
    {
        private readonly ChatWindowControl chatControl;
        private readonly ClaudeApiService apiService;

        public CodeAnalyzer(ChatWindowControl chatControl, ClaudeApiService apiService)
        {
            this.chatControl = chatControl;
            this.apiService = apiService;
        }

        public void PerformAnalysis(string analysisType)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (apiService == null)
            {
                chatControl.AppendToChatDisplay("Please configure your API key first by clicking the settings button (⚙).\n\n");
                return;
            }

            if (string.IsNullOrEmpty(analysisType))
            {
                chatControl.AppendToChatDisplay("Please select an analysis type.\n\n");
                return;
            }

            chatControl.AppendToChatDisplay($"🔍 Analyzing: {analysisType}\n\n");

            try
            {
                string contextPrompt = "";
                string codeContent = "";

                switch (analysisType)
                {
                    case "Active Document":
                        var activeDoc = SolutionAnalyzer.GetActiveDocumentContent();
                        if (!string.IsNullOrEmpty(activeDoc.ErrorMessage))
                        {
                            chatControl.AppendToChatDisplay($"Error: {activeDoc.ErrorMessage}\n\n");
                            return;
                        }
                        contextPrompt = $"Please analyze this {activeDoc.Language} file '{activeDoc.FileName}':";
                        codeContent = activeDoc.Content;
                        break;

                    case "Selected Text":
                        var selectedText = SolutionAnalyzer.GetSelectedText();
                        if (string.IsNullOrEmpty(selectedText))
                        {
                            chatControl.AppendToChatDisplay("No text is currently selected in the active document.\n\n");
                            return;
                        }
                        contextPrompt = "Please analyze this selected code snippet:";
                        codeContent = selectedText;
                        break;

                    case "Solution Structure":
                        var solutionInfo = SolutionAnalyzer.GetSolutionInfo();
                        if (!string.IsNullOrEmpty(solutionInfo.ErrorMessage))
                        {
                            chatControl.AppendToChatDisplay($"Error: {solutionInfo.ErrorMessage}\n\n");
                            return;
                        }
                        contextPrompt = "Please analyze this solution structure and provide insights:";
                        codeContent = solutionInfo.GetSummary();
                        break;

                    case "Current Project":
                        var currentProjectInfo = SolutionAnalyzer.GetSolutionInfo();
                        if (!string.IsNullOrEmpty(currentProjectInfo.ErrorMessage))
                        {
                            chatControl.AppendToChatDisplay($"Error: {currentProjectInfo.ErrorMessage}\n\n");
                            return;
                        }
                        if (currentProjectInfo.Projects.Count == 0)
                        {
                            chatControl.AppendToChatDisplay("No projects found in the current solution.\n\n");
                            return;
                        }
                        var firstProject = currentProjectInfo.Projects.FirstOrDefault();
                        if (firstProject == null)
                        {
                            chatControl.AppendToChatDisplay("No projects found in the current solution.\n\n");
                            return;
                        }
                        contextPrompt = $"Please analyze this project '{firstProject.Name}' structure:";
                        codeContent = $"Project: {firstProject.Name}\nFiles ({firstProject.Files.Count}):\n" +
                                    string.Join("\n", firstProject.Files.Select(f => $"  - {f.RelativePath} ({f.Language})"));
                        break;

                    case "Git Repository Status":
                        var gitContext = GitService.GetGitContext();
                        contextPrompt = "Please analyze the current Git repository status and provide insights about the development workflow, recent changes, and recommendations:";
                        codeContent = gitContext;
                        break;

                    case "Recent Git Changes":
                        var recentFiles = GitService.GetRecentlyChangedFiles(20);
                        if (recentFiles.Count == 0)
                        {
                            chatControl.AppendToChatDisplay("No recent Git changes found or Git repository not available.\n\n");
                            return;
                        }
                        contextPrompt = "Please analyze these recently changed files in the Git repository and provide insights about recent development activity:";
                        codeContent = $"Recently changed files:\n{string.Join("\n", recentFiles.Select(f => $"  - {f}"))}";

                        var gitInfo = GitService.GetGitContext();
                        codeContent = $"{gitInfo}\n\n{codeContent}";
                        break;
                }

                if (string.IsNullOrEmpty(codeContent))
                {
                    chatControl.AppendToChatDisplay("No content found to analyze.\n\n");
                    return;
                }

                var fullPrompt = $"{contextPrompt}\n\n```\n{codeContent}\n```\n\n" +
                               "Please provide a comprehensive analysis including:\n" +
                               "1. Code quality and structure\n" +
                               "2. Potential improvements\n" +
                               "3. Best practices recommendations\n" +
                               "4. Any issues or concerns you notice";

                SendAnalysisMessage(fullPrompt);
            }
            catch (Exception ex)
            {
                chatControl.AppendToChatDisplay($"Error during analysis: {ex.Message}\n\n");
            }
        }

        private async void SendAnalysisMessage(string analysisMessage)
        {
            chatControl.SetTypingIndicator();

            try
            {
                var response = await apiService.SendMessageAsync(analysisMessage);

                chatControl.ClearTypingIndicator();
                chatControl.AppendToChatDisplay($"Claude's Analysis:\n{response}\n\n");

                // Process any code blocks in the response
                var codeBlocks = FileManager.ExtractCodeBlocks(response);
                if (codeBlocks.Count > 0)
                {
                    var results = FileManager.ProcessCodeBlocks(codeBlocks);
                    if (!string.IsNullOrEmpty(results))
                    {
                        chatControl.AppendToChatDisplay(results);
                    }
                }
            }
            catch (Exception ex)
            {
                chatControl.ClearTypingIndicator();
                chatControl.AppendToChatDisplay($"Error during analysis: {ex.Message}\n\n");
            }
        }
    }
}