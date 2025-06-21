using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Linq;

namespace ClaudeAI
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    [Guid("13E4C59E-4C9F-4F92-9C4F-2F5C8D3E1A2B")]
    public class ChatWindow : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChatWindow"/> class.
        /// </summary>
        public ChatWindow() : base(null)
        {
            this.Caption = "Claude AI Assistant";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new ChatWindowControl();
        }
    }

    /// <summary>
    /// User control for the chat interface
    /// </summary>
    public partial class ChatWindowControl : UserControl
    {
        private TextBox chatDisplay;
        private TextBox messageInput;
        private Button sendButton;
        private Button settingsButton;
        private Button analyzeButton;
        private ComboBox analysisTypeCombo;
        private ScrollViewer scrollViewer;
        private ClaudeApiService apiService;
        private VSThemeColors currentTheme;

        public ChatWindowControl()
        {
            // Get current theme before initializing components
            currentTheme = ThemeHelper.GetCurrentTheme();
            InitializeComponent();
            InitializeApiService();
        }

        private void InitializeComponent()
        {
            this.Background = currentTheme.BackgroundBrush;

            // Main grid layout
            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Analysis toolbar
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Chat display
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Input area
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Status bar

            // Analysis toolbar
            var toolbarGrid = new Grid();
            toolbarGrid.Background = currentTheme.ToolWindowBackgroundBrush;
            toolbarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            toolbarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            toolbarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var analyzeLabel = new Label
            {
                Content = "Analyze:",
                Foreground = currentTheme.ToolWindowTextBrush,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 10,
                Padding = new Thickness(10, 5, 5, 5),
                VerticalAlignment = VerticalAlignment.Center
            };

            analysisTypeCombo = new ComboBox
            {
                Width = 150,
                Height = 25,
                Background = currentTheme.TextBoxBackgroundBrush,
                Foreground = currentTheme.ToolWindowTextBrush,
                BorderBrush = currentTheme.AccentBrush,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 10,
                Margin = new Thickness(0, 5, 10, 5),
                VerticalAlignment = VerticalAlignment.Center
            };

            analysisTypeCombo.Items.Add("Active Document");
            analysisTypeCombo.Items.Add("Selected Text");
            analysisTypeCombo.Items.Add("Solution Structure");
            analysisTypeCombo.Items.Add("Current Project");
            analysisTypeCombo.SelectedIndex = 0;

            analyzeButton = new Button
            {
                Content = "📁 Analyze Code",
                Height = 25,
                Background = currentTheme.AccentBrush,
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0, 0, 0, 0),
                Margin = new Thickness(0, 5, 10, 5),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 10,
                Padding = new Thickness(8, 3, 8, 3),
                VerticalAlignment = VerticalAlignment.Center
            };

            Grid.SetColumn(analyzeLabel, 0);
            Grid.SetColumn(analysisTypeCombo, 1);
            Grid.SetColumn(analyzeButton, 2);
            toolbarGrid.Children.Add(analyzeLabel);
            toolbarGrid.Children.Add(analysisTypeCombo);
            toolbarGrid.Children.Add(analyzeButton);

            Grid.SetRow(toolbarGrid, 0);
            mainGrid.Children.Add(toolbarGrid);

            // Chat display area with scroll viewer
            scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Margin = new Thickness(10, 10, 10, 10)
            };

            chatDisplay = new TextBox
            {
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                Background = currentTheme.TextBoxBackgroundBrush,
                Foreground = currentTheme.ToolWindowTextBrush,
                BorderThickness = new Thickness(1, 1, 1, 1),
                BorderBrush = currentTheme.AccentBrush,
                Padding = new Thickness(10, 10, 10, 10),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                Text = "Welcome to Claude AI Assistant!\nType your message below and press Enter or click Send.\n\n"
            };

            scrollViewer.Content = chatDisplay;
            Grid.SetRow(scrollViewer, 1);
            mainGrid.Children.Add(scrollViewer);

            // Input area
            var inputGrid = new Grid();
            inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            messageInput = new TextBox
            {
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = 100,
                Background = currentTheme.TextBoxBackgroundBrush,
                Foreground = currentTheme.ToolWindowTextBrush,
                BorderThickness = new Thickness(1, 1, 1, 1),
                BorderBrush = currentTheme.AccentBrush,
                Padding = new Thickness(8, 8, 8, 8),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 11,
                Margin = new Thickness(10, 5, 5, 10)
            };

            settingsButton = new Button
            {
                Content = "⚙",
                Width = 30,
                Height = 30,
                Background = currentTheme.ButtonBackgroundBrush,
                Foreground = currentTheme.ToolWindowTextBrush,
                BorderThickness = new Thickness(1, 1, 1, 1),
                BorderBrush = currentTheme.AccentBrush,
                Margin = new Thickness(0, 5, 5, 10),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 14,
                ToolTip = "API Settings"
            };

            sendButton = new Button
            {
                Content = "Send",
                Width = 60,
                Height = 30,
                Background = currentTheme.AccentBrush,
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0, 0, 0, 0),
                Margin = new Thickness(0, 5, 10, 10),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 11
            };

            Grid.SetColumn(messageInput, 0);
            Grid.SetColumn(settingsButton, 1);
            Grid.SetColumn(sendButton, 2);
            inputGrid.Children.Add(messageInput);
            inputGrid.Children.Add(settingsButton);
            inputGrid.Children.Add(sendButton);

            Grid.SetRow(inputGrid, 2);
            mainGrid.Children.Add(inputGrid);

            // Status bar
            var statusLabel = new Label
            {
                Content = "Ready - Enter your message above or analyze code",
                Background = currentTheme.AccentBrush,
                Foreground = new SolidColorBrush(Colors.White),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 10,
                Padding = new Thickness(10, 3, 10, 3),
                HorizontalContentAlignment = HorizontalAlignment.Left
            };

            Grid.SetRow(statusLabel, 3);
            mainGrid.Children.Add(statusLabel);

            this.Content = mainGrid;

            // Event handlers
            sendButton.Click += SendButton_Click;
            settingsButton.Click += SettingsButton_Click;
            analyzeButton.Click += AnalyzeButton_Click;
            messageInput.KeyDown += MessageInput_KeyDown;

            // Focus on input
            messageInput.Focus();
        }

        private void InitializeApiService()
        {
            var apiKey = SettingsManager.GetApiKey();
            if (!string.IsNullOrEmpty(apiKey))
            {
                apiService = new ClaudeApiService(apiKey);
                UpdateWelcomeMessage();
            }
            else
            {
                ShowApiKeyDialog();
            }
        }

        private void UpdateWelcomeMessage()
        {
            var hasApiKey = SettingsManager.HasApiKey();
            var welcomeMessage = hasApiKey
                ? "Welcome to Claude AI Assistant!\nYour API key is configured. Type your message below and press Enter or click Send.\n\n"
                : "Welcome to Claude AI Assistant!\nPlease configure your API key using the settings button (⚙) to start chatting.\n\n";

            chatDisplay.Text = welcomeMessage;
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            ShowApiKeyDialog();
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
                    // Dispose old service if exists
                    apiService?.Dispose();

                    // Create new service with new API key
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

        private void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (apiService == null)
            {
                AppendToChatDisplay("Please configure your API key first by clicking the settings button (⚙).\n\n");
                return;
            }

            var analysisType = analysisTypeCombo.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(analysisType))
            {
                AppendToChatDisplay("Please select an analysis type.\n\n");
                return;
            }

            AppendToChatDisplay($"🔍 Analyzing: {analysisType}\n\n");

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
                            AppendToChatDisplay($"Error: {activeDoc.ErrorMessage}\n\n");
                            return;
                        }
                        contextPrompt = $"Please analyze this {activeDoc.Language} file '{activeDoc.FileName}':";
                        codeContent = activeDoc.Content;
                        break;

                    case "Selected Text":
                        var selectedText = SolutionAnalyzer.GetSelectedText();
                        if (string.IsNullOrEmpty(selectedText))
                        {
                            AppendToChatDisplay("No text is currently selected in the active document.\n\n");
                            return;
                        }
                        contextPrompt = "Please analyze this selected code snippet:";
                        codeContent = selectedText;
                        break;

                    case "Solution Structure":
                        var solutionInfo = SolutionAnalyzer.GetSolutionInfo();
                        if (!string.IsNullOrEmpty(solutionInfo.ErrorMessage))
                        {
                            AppendToChatDisplay($"Error: {solutionInfo.ErrorMessage}\n\n");
                            return;
                        }
                        contextPrompt = "Please analyze this solution structure and provide insights:";
                        codeContent = solutionInfo.GetSummary();
                        break;

                    case "Current Project":
                        var currentProjectInfo = SolutionAnalyzer.GetSolutionInfo();
                        if (!string.IsNullOrEmpty(currentProjectInfo.ErrorMessage))
                        {
                            AppendToChatDisplay($"Error: {currentProjectInfo.ErrorMessage}\n\n");
                            return;
                        }
                        if (currentProjectInfo.Projects.Count == 0)
                        {
                            AppendToChatDisplay("No projects found in the current solution.\n\n");
                            return;
                        }
                        var firstProject = currentProjectInfo.Projects.FirstOrDefault();
                        if (firstProject == null)
                        {
                            AppendToChatDisplay("No projects found in the current solution.\n\n");
                            return;
                        }
                        contextPrompt = $"Please analyze this project '{firstProject.Name}' structure:";
                        codeContent = $"Project: {firstProject.Name}\nFiles ({firstProject.Files.Count}):\n" +
                                    string.Join("\n", firstProject.Files.Select(f => $"  - {f.RelativePath} ({f.Language})"));
                        break;
                }

                if (string.IsNullOrEmpty(codeContent))
                {
                    AppendToChatDisplay("No content found to analyze.\n\n");
                    return;
                }

                // Combine context and code for Claude
                var fullPrompt = $"{contextPrompt}\n\n```\n{codeContent}\n```\n\n" +
                               "Please provide a comprehensive analysis including:\n" +
                               "1. Code quality and structure\n" +
                               "2. Potential improvements\n" +
                               "3. Best practices recommendations\n" +
                               "4. Any issues or concerns you notice";

                // Send to Claude
                SendAnalysisMessage(fullPrompt);
            }
            catch (Exception ex)
            {
                AppendToChatDisplay($"Error during analysis: {ex.Message}\n\n");
            }
        }

        private async void SendAnalysisMessage(string message)
        {
            // Add analysis message indicator
            AppendToChatDisplay("Claude is analyzing your code...\n");

            // Disable controls during analysis
            analyzeButton.IsEnabled = false;
            sendButton.IsEnabled = false;
            messageInput.IsEnabled = false;

            try
            {
                // Get response from Claude
                var response = await apiService.SendMessageAsync(message);

                // Remove analysis indicator and add response
                var currentText = chatDisplay.Text;
                if (currentText.EndsWith("Claude is analyzing your code...\n"))
                {
                    chatDisplay.Text = currentText.Substring(0, currentText.Length - "Claude is analyzing your code...\n".Length);
                }

                AppendToChatDisplay($"Claude's Analysis:\n{response}\n\n");

                // Process any code blocks in the response
                ProcessClaudeResponse(response);
            }
            catch (Exception ex)
            {
                // Remove analysis indicator
                var currentText = chatDisplay.Text;
                if (currentText.EndsWith("Claude is analyzing your code...\n"))
                {
                    chatDisplay.Text = currentText.Substring(0, currentText.Length - "Claude is analyzing your code...\n".Length);
                }

                AppendToChatDisplay($"Error during analysis: {ex.Message}\n\n");
            }
            finally
            {
                // Re-enable controls
                analyzeButton.IsEnabled = true;
                sendButton.IsEnabled = true;
                messageInput.IsEnabled = true;
                messageInput.Focus();
            }
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

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private async void SendMessage()
        {
            var message = messageInput.Text.Trim();
            if (string.IsNullOrEmpty(message))
                return;

            if (apiService == null)
            {
                AppendToChatDisplay("Please configure your API key first by clicking the settings button (⚙).\n\n");
                return;
            }

            // Add user message to chat
            AppendToChatDisplay($"You: {message}\n\n");

            // Clear input and disable controls
            messageInput.Text = string.Empty;
            sendButton.IsEnabled = false;
            messageInput.IsEnabled = false;

            try
            {
                // Show typing indicator
                AppendToChatDisplay("Claude is typing...\n");

                // Get response from Claude
                var response = await apiService.SendMessageAsync(message);

                // Remove typing indicator and add response
                var currentText = chatDisplay.Text;
                if (currentText.EndsWith("Claude is typing...\n"))
                {
                    chatDisplay.Text = currentText.Substring(0, currentText.Length - "Claude is typing...\n".Length);
                }

                AppendToChatDisplay($"Claude: {response}\n\n");

                // Process any code blocks in the response
                ProcessClaudeResponse(response);
            }
            catch (Exception ex)
            {
                // Remove typing indicator
                var currentText = chatDisplay.Text;
                if (currentText.EndsWith("Claude is typing...\n"))
                {
                    chatDisplay.Text = currentText.Substring(0, currentText.Length - "Claude is typing...\n".Length);
                }

                AppendToChatDisplay($"Error: {ex.Message}\n\n");
            }
            finally
            {
                // Re-enable controls
                sendButton.IsEnabled = true;
                messageInput.IsEnabled = true;
                messageInput.Focus();
            }
        }

        private void ProcessClaudeResponse(string response)
        {
            try
            {
                // Extract code blocks from Claude's response
                var codeBlocks = FileManager.ExtractCodeBlocks(response);

                if (codeBlocks.Count > 0)
                {
                    // Process the code blocks and create/update files
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

        private void AppendToChatDisplay(string text)
        {
            chatDisplay.AppendText(text);
            scrollViewer.ScrollToEnd();
        }
    }
}