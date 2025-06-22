using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Documents;

namespace ClaudeAI
{
    /// <summary>
    /// User control for the chat interface
    /// </summary>
    public partial class ChatWindowControl : UserControl
    {
        private RichTextBox chatDisplay;
        private TextBox messageInput;
        private Button sendButton;
        private Button settingsButton;
        private Button analyzeButton;
        private Button uploadButton;
        private ComboBox analysisTypeCombo;
        private ScrollViewer scrollViewer;
        private ClaudeApiService apiService;
        private VSThemeColors currentTheme;
        private PairProgrammingPanel pairProgrammingPanel;

        public ChatWindowControl()
        {
            currentTheme = ThemeHelper.GetCurrentTheme();
            InitializeComponent();
            InitializeApiService();
        }

        private void InitializeComponent()
        {
            this.Background = currentTheme.BackgroundBrush;

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Analysis toolbar
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Pair programming panel
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Chat display
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Input area
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Status bar

            CreateToolbar(mainGrid);
            CreatePairProgrammingPanel(mainGrid);
            CreateChatDisplay(mainGrid);
            CreateInputArea(mainGrid);
            CreateStatusBar(mainGrid);

            this.Content = mainGrid;
            SetupEventHandlers();
            messageInput.Focus();
        }

        private void CreateToolbar(Grid mainGrid)
        {
            var toolbarGrid = new Grid();
            toolbarGrid.Background = currentTheme.ToolWindowBackgroundBrush;
            toolbarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            toolbarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            toolbarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
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
            analysisTypeCombo.Items.Add("Git Repository Status");
            analysisTypeCombo.Items.Add("Recent Git Changes");
            analysisTypeCombo.SelectedIndex = 0;

            analyzeButton = new Button
            {
                Content = "🔍 Analyze Code",
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

            settingsButton = new Button
            {
                Content = "⚙",
                Width = 30,
                Height = 25,
                Background = currentTheme.ButtonBackgroundBrush,
                Foreground = currentTheme.ToolWindowTextBrush,
                BorderThickness = new Thickness(1, 1, 1, 1),
                BorderBrush = currentTheme.AccentBrush,
                Margin = new Thickness(0, 5, 10, 5),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = "API Settings"
            };

            Grid.SetColumn(analyzeLabel, 0);
            Grid.SetColumn(analysisTypeCombo, 1);
            Grid.SetColumn(analyzeButton, 2);
            Grid.SetColumn(settingsButton, 3);
            toolbarGrid.Children.Add(analyzeLabel);
            toolbarGrid.Children.Add(analysisTypeCombo);
            toolbarGrid.Children.Add(analyzeButton);
            toolbarGrid.Children.Add(settingsButton);

            Grid.SetRow(toolbarGrid, 0);
            mainGrid.Children.Add(toolbarGrid);
        }

        private void CreatePairProgrammingPanel(Grid mainGrid)
        {
            pairProgrammingPanel = new PairProgrammingPanel(this, currentTheme);

            var border = new Border
            {
                Child = pairProgrammingPanel,
                BorderBrush = currentTheme.AccentBrush,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Background = currentTheme.ToolWindowBackgroundBrush
            };

            Grid.SetRow(border, 1);
            mainGrid.Children.Add(border);
        }

        private void CreateChatDisplay(Grid mainGrid)
        {
            scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Margin = new Thickness(10, 10, 10, 10)
            };

            chatDisplay = new RichTextBox
            {
                IsReadOnly = true,
                Background = currentTheme.TextBoxBackgroundBrush,
                Foreground = currentTheme.ToolWindowTextBrush,
                BorderThickness = new Thickness(1, 1, 1, 1),
                BorderBrush = currentTheme.AccentBrush,
                Padding = new Thickness(10, 10, 10, 10),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12,
                AllowDrop = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden
            };

            // Initialize with welcome message
            var welcomeDoc = new FlowDocument();
            AddFormattedText(welcomeDoc, "Welcome to Claude AI Assistant!\nType your message below, analyze code, or upload files for review.\n\n", false);
            chatDisplay.Document = welcomeDoc;

            scrollViewer.Content = chatDisplay;
            Grid.SetRow(scrollViewer, 2);
            mainGrid.Children.Add(scrollViewer);
        }

        private void CreateInputArea(Grid mainGrid)
        {
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

            uploadButton = new Button
            {
                Content = "📁",
                Width = 35,
                Height = 30,
                Background = currentTheme.ButtonBackgroundBrush,
                Foreground = currentTheme.ToolWindowTextBrush,
                BorderThickness = new Thickness(1, 1, 1, 1),
                BorderBrush = currentTheme.AccentBrush,
                Margin = new Thickness(0, 5, 5, 10),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Bottom,
                ToolTip = "Upload files for analysis"
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
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Bottom
            };

            Grid.SetColumn(messageInput, 0);
            Grid.SetColumn(uploadButton, 1);
            Grid.SetColumn(sendButton, 2);
            inputGrid.Children.Add(messageInput);
            inputGrid.Children.Add(uploadButton);
            inputGrid.Children.Add(sendButton);

            Grid.SetRow(inputGrid, 3);
            mainGrid.Children.Add(inputGrid);
        }

        private void CreateStatusBar(Grid mainGrid)
        {
            var statusLabel = new Label
            {
                Content = "Ready - Enter your message above, analyze code, or upload files",
                Background = currentTheme.AccentBrush,
                Foreground = new SolidColorBrush(Colors.White),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 10,
                Padding = new Thickness(10, 3, 10, 3),
                HorizontalContentAlignment = HorizontalAlignment.Left
            };

            Grid.SetRow(statusLabel, 4);
            mainGrid.Children.Add(statusLabel);
        }

        private void SetupEventHandlers()
        {
            sendButton.Click += SendButton_Click;
            settingsButton.Click += SettingsButton_Click;
            analyzeButton.Click += AnalyzeButton_Click;
            uploadButton.Click += UploadButton_Click;
            messageInput.KeyDown += MessageInput_KeyDown;
            messageInput.TextChanged += MessageInput_TextChanged;
            chatDisplay.DragEnter += ChatDisplay_DragEnter;
            chatDisplay.DragOver += ChatDisplay_DragOver;
            chatDisplay.Drop += ChatDisplay_Drop;
        }

        private void MessageInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Notify pair programming service about user typing
            pairProgrammingPanel?.NotifyUserTyping();
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
                ? "Welcome to Claude AI Assistant!\nYour API key is configured. Type your message below, analyze code, or upload files for review.\n\n"
                : "Welcome to Claude AI Assistant!\nPlease configure your API key using the settings button (⚙) to start chatting.\n\n";

            var welcomeDoc = new FlowDocument();
            AddFormattedText(welcomeDoc, welcomeMessage, false);
            chatDisplay.Document = welcomeDoc;
        }

        public void AppendToChatDisplay(string text)
        {
            AppendFormattedText(text, false);
            scrollViewer.ScrollToEnd();
        }

        public void AppendCodeToChatDisplay(string code, string language = "")
        {
            AppendFormattedText(code, true);
            scrollViewer.ScrollToEnd();
        }

        private void AppendFormattedText(string text, bool isCode)
        {
            var document = chatDisplay.Document;

            // Parse text for code blocks
            if (!isCode && text.Contains("```"))
            {
                AppendMixedContent(document, text);
            }
            else
            {
                AddFormattedText(document, text, isCode);
            }
        }

        private void AppendMixedContent(FlowDocument document, string text)
        {
            var parts = text.Split(new string[] { "```" }, StringSplitOptions.None);
            bool isCodeBlock = false;

            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part))
                {
                    isCodeBlock = !isCodeBlock;
                    continue;
                }

                if (isCodeBlock)
                {
                    // Extract language if present (first line)
                    var lines = part.Split('\n');
                    var codeText = lines.Length > 1 ? string.Join("\n", lines, 1, lines.Length - 1) : part;

                    if (!string.IsNullOrWhiteSpace(codeText))
                    {
                        AddFormattedText(document, codeText.Trim() + "\n", true);
                    }
                }
                else
                {
                    AddFormattedText(document, part, false);
                }

                isCodeBlock = !isCodeBlock;
            }
        }

        private void AddFormattedText(FlowDocument document, string text, bool isCode)
        {
            var paragraph = new Paragraph();
            var run = new Run(text);

            if (isCode)
            {
                // Code formatting - monospace font, different background
                run.FontFamily = new FontFamily("Consolas, Monaco, 'Courier New', monospace");
                run.FontSize = 11;
                run.Background = new SolidColorBrush(currentTheme.IsLightTheme ?
                    Color.FromRgb(245, 245, 245) : Color.FromRgb(20, 20, 20));
                paragraph.Background = run.Background;
                paragraph.Padding = new Thickness(8);
                paragraph.Margin = new Thickness(0, 4, 0, 4);
            }
            else
            {
                // Prose formatting - proportional font
                run.FontFamily = new FontFamily("Segoe UI");
                run.FontSize = 12;
            }

            run.Foreground = currentTheme.ToolWindowTextBrush;
            paragraph.Inlines.Add(run);
            document.Blocks.Add(paragraph);
        }

        public void ClearTypingIndicator(string indicatorText = "Claude is analyzing your code...\n")
        {
            var document = chatDisplay.Document;
            var lastBlock = document.Blocks.LastBlock as Paragraph;

            if (lastBlock != null)
            {
                var lastRun = lastBlock.Inlines.LastInline as Run;
                if (lastRun != null && lastRun.Text.EndsWith(indicatorText))
                {
                    var newText = lastRun.Text.Substring(0, lastRun.Text.Length - indicatorText.Length);
                    if (string.IsNullOrEmpty(newText))
                    {
                        document.Blocks.Remove(lastBlock);
                    }
                    else
                    {
                        lastRun.Text = newText;
                    }
                }
            }
        }

        public void SetTypingIndicator()
        {
            AppendToChatDisplay("Claude is analyzing your code...\n");
        }
    }
}