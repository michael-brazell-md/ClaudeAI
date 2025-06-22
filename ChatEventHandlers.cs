using System;
using System.Windows;
using System.Linq;

namespace ClaudeAI
{
    /// <summary>
    /// Event handlers for chat window controls
    /// </summary>
    public partial class ChatWindowControl
    {
        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            ShowApiKeyDialog();
        }

        private void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            var analyzer = new CodeAnalyzer(this, apiService);
            analyzer.PerformAnalysis(analysisTypeCombo.SelectedItem?.ToString());
        }

        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            var fileHandler = new FileUploadHandler(this, apiService);
            fileHandler.ShowFileDialog();
        }

        private void MessageInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter &&
                (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Shift) == 0)
            {
                e.Handled = true;
                SendMessage();
            }
        }

        private void ChatDisplay_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void ChatDisplay_DragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        private void ChatDisplay_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                var fileHandler = new FileUploadHandler(this, apiService);
                fileHandler.ProcessFiles(files);
            }
        }

        private void ShowApiKeyDialog()
        {
            var currentApiKey = SettingsManager.GetApiKey();
            var dialog = new ApiKeyDialog(currentApiKey);

            if (Application.Current.MainWindow != null)
            {
                dialog.Owner = Application.Current.MainWindow;
            }

            if (dialog.ShowDialog() == true)
            {
                var success = SettingsManager.SaveApiKey(dialog.ApiKey);
                if (success)
                {
                    apiService?.Dispose();
                    apiService = new ClaudeApiService(dialog.ApiKey);
                    AppendToChatDisplay("✓ API key configured successfully!\n\n");
                    UpdateWelcomeMessage();
                }
                else
                {
                    AppendToChatDisplay("✗ Failed to save API key. Please try again.\n\n");
                }
            }
        }

        private async void SendMessage()
        {
            var userMessage = messageInput.Text.Trim();
            if (string.IsNullOrEmpty(userMessage))
                return;

            if (apiService == null)
            {
                AppendToChatDisplay("Please configure your API key first by clicking the settings button (⚙).\n\n");
                return;
            }

            var enhancedMessage = EnhanceMessageWithGitContext(userMessage);

            AppendToChatDisplay($"You: {userMessage}\n\n");

            messageInput.Text = string.Empty;
            sendButton.IsEnabled = false;
            messageInput.IsEnabled = false;

            try
            {
                AppendToChatDisplay("Claude is typing...\n");

                var response = await apiService.SendMessageAsync(enhancedMessage);

                ClearTypingIndicator("Claude is typing...\n");
                AppendToChatDisplay($"Claude: {response}\n\n");
                ProcessClaudeResponse(response);
            }
            catch (Exception ex)
            {
                ClearTypingIndicator("Claude is typing...\n");
                AppendToChatDisplay($"Error: {ex.Message}\n\n");
            }
            finally
            {
                sendButton.IsEnabled = true;
                messageInput.IsEnabled = true;
                messageInput.Focus();
            }
        }

        private string EnhanceMessageWithGitContext(string userMessage)
        {
            var gitKeywords = new[] {
                "git", "commit", "branch", "merge", "pull", "push", "repository", "repo",
                "version", "change", "diff", "history", "recent", "latest", "current work",
                "working on", "in progress", "refactor", "modify", "update", "create file",
                "new file", "add file"
            };

            var lowerMessage = userMessage.ToLowerInvariant();
            var needsGitContext = gitKeywords.Any(keyword => lowerMessage.Contains(keyword));

            if (needsGitContext)
            {
                try
                {
                    var gitContext = GitService.GetGitContext();
                    if (!gitContext.StartsWith("Git Status: No") && !gitContext.StartsWith("Git Status: Error"))
                    {
                        return $"{gitContext}\n\nUser Question: {userMessage}";
                    }
                }
                catch (Exception)
                {
                    // If Git context fails, just use original message
                }
            }

            return userMessage;
        }

        private void ProcessClaudeResponse(string response)
        {
            try
            {
                var codeBlocks = FileManager.ExtractCodeBlocks(response);

                if (codeBlocks.Count > 0)
                {
                    var results = FileManager.ProcessCodeBlocks(codeBlocks);

                    if (!string.IsNullOrEmpty(results))
                    {
                        AppendToChatDisplay(results);
                    }
                }
            }
            catch (Exception ex)
            {
                AppendToChatDisplay($"⚠️ Error processing code blocks: {ex.Message}\n\n");
            }
        }
    }
}