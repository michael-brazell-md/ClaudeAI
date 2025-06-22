using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ClaudeAI
{
    /// <summary>
    /// UI panel for controlling AI pair programming features
    /// </summary>
    public partial class PairProgrammingPanel : UserControl
    {
        private readonly ChatWindowControl chatControl;
        private readonly VSThemeColors currentTheme;
        private PairProgrammingService pairProgrammingService;

        private Button toggleButton;
        private Button settingsButton;
        private Button testButton;
        private ComboBox sensitivityCombo;
        private CheckBox autoSuggestCheckBox;
        private CheckBox contextAwareCheckBox;
        private Label statusLabel;

        public PairProgrammingPanel(ChatWindowControl chatControl, VSThemeColors theme)
        {
            this.chatControl = chatControl;
            this.currentTheme = theme;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Background = currentTheme.ToolWindowBackgroundBrush;

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            CreateHeaderSection(mainGrid);
            CreateControlsSection(mainGrid);
            CreateStatusSection(mainGrid);

            this.Content = mainGrid;
        }

        private void CreateHeaderSection(Grid mainGrid)
        {
            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleLabel = new Label
            {
                Content = "🤖 AI Pair Programming",
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = currentTheme.ToolWindowTextBrush,
                Padding = new Thickness(8, 4, 4, 4),
                VerticalAlignment = VerticalAlignment.Center
            };

            testButton = new Button
            {
                Content = "🧪",
                Width = 24,
                Height = 24,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 10,
                Background = new SolidColorBrush(Color.FromRgb(0, 180, 0)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0, 0, 0, 0),
                Margin = new Thickness(2, 2, 2, 2),
                ToolTip = "Test analysis system"
            };

            toggleButton = new Button
            {
                Content = "▶ Start",
                Width = 60,
                Height = 24,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 9,
                Background = currentTheme.AccentBrush,
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0, 0, 0, 0),
                Margin = new Thickness(2, 2, 2, 2),
                ToolTip = "Start/Stop AI pair programming assistance"
            };

            settingsButton = new Button
            {
                Content = "⚙",
                Width = 24,
                Height = 24,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 10,
                Background = currentTheme.ButtonBackgroundBrush,
                Foreground = currentTheme.ToolWindowTextBrush,
                BorderThickness = new Thickness(1, 1, 1, 1),
                BorderBrush = currentTheme.AccentBrush,
                Margin = new Thickness(2, 2, 2, 2),
                ToolTip = "Pair programming settings"
            };

            Grid.SetColumn(titleLabel, 0);
            Grid.SetColumn(testButton, 1);
            Grid.SetColumn(toggleButton, 2);
            Grid.SetColumn(settingsButton, 3);
            headerGrid.Children.Add(titleLabel);
            headerGrid.Children.Add(testButton);
            headerGrid.Children.Add(toggleButton);
            headerGrid.Children.Add(settingsButton);

            Grid.SetRow(headerGrid, 0);
            mainGrid.Children.Add(headerGrid);

            // Event handlers
            testButton.Click += TestButton_Click;
            toggleButton.Click += ToggleButton_Click;
            settingsButton.Click += SettingsButton_Click;
        }

        private void CreateControlsSection(Grid mainGrid)
        {
            var controlsPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(8, 4, 8, 4),
                Visibility = Visibility.Collapsed // Hidden by default
            };

            // Sensitivity setting
            var sensitivityPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 2, 0, 2)
            };

            var sensitivityLabel = new Label
            {
                Content = "Sensitivity:",
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 9,
                Foreground = currentTheme.ToolWindowTextBrush,
                Padding = new Thickness(0, 2, 4, 2),
                VerticalAlignment = VerticalAlignment.Center
            };

            sensitivityCombo = new ComboBox
            {
                Width = 80,
                Height = 20,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 9,
                Background = currentTheme.TextBoxBackgroundBrush,
                Foreground = currentTheme.ToolWindowTextBrush,
                BorderBrush = currentTheme.AccentBrush
            };

            sensitivityCombo.Items.Add("Low");
            sensitivityCombo.Items.Add("Medium");
            sensitivityCombo.Items.Add("High");
            sensitivityCombo.SelectedIndex = 1; // Default to Medium

            sensitivityPanel.Children.Add(sensitivityLabel);
            sensitivityPanel.Children.Add(sensitivityCombo);

            // Auto-suggest checkbox
            autoSuggestCheckBox = new CheckBox
            {
                Content = "Auto-suggest improvements",
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 9,
                Foreground = currentTheme.ToolWindowTextBrush,
                Margin = new Thickness(0, 2, 0, 2),
                IsChecked = true
            };

            // Context-aware checkbox
            contextAwareCheckBox = new CheckBox
            {
                Content = "Context-aware analysis",
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 9,
                Foreground = currentTheme.ToolWindowTextBrush,
                Margin = new Thickness(0, 2, 0, 2),
                IsChecked = true
            };

            controlsPanel.Children.Add(sensitivityPanel);
            controlsPanel.Children.Add(autoSuggestCheckBox);
            controlsPanel.Children.Add(contextAwareCheckBox);

            // Store reference for toggling visibility
            controlsPanel.Tag = "SettingsPanel";

            Grid.SetRow(controlsPanel, 1);
            mainGrid.Children.Add(controlsPanel);
        }

        private void CreateStatusSection(Grid mainGrid)
        {
            statusLabel = new Label
            {
                Content = "⏸️ Ready to start pair programming",
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 9,
                Foreground = currentTheme.ToolWindowTextBrush,
                Padding = new Thickness(8, 2, 8, 4),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            Grid.SetRow(statusLabel, 2);
            mainGrid.Children.Add(statusLabel);
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            if (pairProgrammingService != null)
            {
                chatControl.AppendToChatDisplay("🧪 Running manual test analysis...\n");
                pairProgrammingService.TestAnalysis();
            }
            else
            {
                chatControl.AppendToChatDisplay("❌ Start pair programming first before testing.\n");
            }
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (pairProgrammingService == null)
            {
                StartPairProgramming();
            }
            else
            {
                StopPairProgramming();
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsPanel = FindChildByTag("SettingsPanel") as StackPanel;
            if (settingsPanel != null)
            {
                settingsPanel.Visibility = settingsPanel.Visibility == Visibility.Visible
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            }
        }

        private void StartPairProgramming()
        {
            try
            {
                // Get API service from chat control
                var apiService = GetApiServiceFromChatControl();
                if (apiService == null)
                {
                    chatControl.AppendToChatDisplay("❌ Please configure your API key first to enable pair programming.\n\n");
                    return;
                }

                pairProgrammingService = new PairProgrammingService(chatControl, apiService);
                pairProgrammingService.SuggestionReady += OnSuggestionReady;
                pairProgrammingService.IsPairProgrammingEnabled = true;

                // Update UI
                toggleButton.Content = "⏸ Pause";
                toggleButton.Background = new SolidColorBrush(Color.FromRgb(255, 140, 0)); // Orange for pause
                statusLabel.Content = "🤖 Active - monitoring your code";
                statusLabel.Foreground = currentTheme.AccentBrush;

                chatControl.AppendToChatDisplay("🚀 AI Pair Programming started! I'll provide contextual suggestions as you code.\n\n");
                chatControl.AppendToChatDisplay("🧪 TEST: Click the green 🧪 test button to verify the system is working.\n\n");
            }
            catch (Exception ex)
            {
                chatControl.AppendToChatDisplay($"❌ Failed to start pair programming: {ex.Message}\n\n");
            }
        }

        private void StopPairProgramming()
        {
            if (pairProgrammingService != null)
            {
                pairProgrammingService.IsPairProgrammingEnabled = false;
                pairProgrammingService.SuggestionReady -= OnSuggestionReady;
                pairProgrammingService.Dispose();
                pairProgrammingService = null;
            }

            // Update UI
            toggleButton.Content = "▶ Start";
            toggleButton.Background = currentTheme.AccentBrush;
            statusLabel.Content = "⏸️ Paused";
            statusLabel.Foreground = currentTheme.ToolWindowTextBrush;

            chatControl.AppendToChatDisplay("⏸️ AI Pair Programming paused.\n\n");
        }

        private void OnSuggestionReady(object sender, PairProgrammingEventArgs e)
        {
            // Update status to show last suggestion time
            statusLabel.Content = $"💡 Last suggestion: {e.Timestamp:HH:mm:ss}";
        }

        private ClaudeApiService GetApiServiceFromChatControl()
        {
            // Use reflection to access the private apiService field from ChatWindowControl
            var field = typeof(ChatWindowControl).GetField("apiService",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(chatControl) as ClaudeApiService;
        }

        private FrameworkElement FindChildByTag(string tag)
        {
            return FindChildByTag(this, tag);
        }

        private FrameworkElement FindChildByTag(DependencyObject parent, string tag)
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is FrameworkElement element && element.Tag?.ToString() == tag)
                {
                    return element;
                }

                var result = FindChildByTag(child, tag);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        public void NotifyUserTyping()
        {
            pairProgrammingService?.OnUserTyping();
        }

        public void Cleanup()
        {
            pairProgrammingService?.Dispose();
        }
    }
}