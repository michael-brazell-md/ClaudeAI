using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ClaudeAI
{
    /// <summary>
    /// Dialog for entering and managing the Anthropic API key
    /// </summary>
    public class ApiKeyDialog : Window
    {
        private TextBox apiKeyTextBox;
        private PasswordBox apiKeyPasswordBox;
        private CheckBox showPasswordCheckBox;
        private Button okButton;
        private Button cancelButton;
        private TextBlock statusText;

        public string ApiKey { get; private set; }

        public ApiKeyDialog(string currentApiKey = "")
        {
            InitializeDialog();
            if (!string.IsNullOrEmpty(currentApiKey))
            {
                apiKeyPasswordBox.Password = currentApiKey;
                apiKeyTextBox.Text = currentApiKey;
            }
        }

        private void InitializeDialog()
        {
            // Window properties
            Title = "Claude AI - API Key Configuration";
            Width = 500;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush(Color.FromRgb(37, 37, 38));

            // Main grid
            var mainGrid = new Grid();
            mainGrid.Margin = new Thickness(20, 20, 20, 20);
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Title
            var titleLabel = new Label
            {
                Content = "Claude AI API Configuration",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            Grid.SetRow(titleLabel, 0);
            mainGrid.Children.Add(titleLabel);

            // Instructions
            var instructionsText = new TextBlock
            {
                Text = "To use Claude AI, you need an API key from Anthropic. Follow these steps:",
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(instructionsText, 1);
            mainGrid.Children.Add(instructionsText);

            // Steps
            var stepsText = new TextBlock
            {
                Text = "1. Visit https://console.anthropic.com/\n" +
                       "2. Sign up or log in to your account\n" +
                       "3. Navigate to API Keys section\n" +
                       "4. Create a new API key\n" +
                       "5. Copy and paste the key below",
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                Margin = new Thickness(0, 0, 0, 20)
            };
            Grid.SetRow(stepsText, 2);
            mainGrid.Children.Add(stepsText);

            // API Key label
            var apiKeyLabel = new Label
            {
                Content = "Anthropic API Key:",
                Foreground = new SolidColorBrush(Colors.White),
                Margin = new Thickness(0, 0, 0, 5)
            };
            Grid.SetRow(apiKeyLabel, 3);
            mainGrid.Children.Add(apiKeyLabel);

            // API Key input (PasswordBox - hidden by default)
            apiKeyPasswordBox = new PasswordBox
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Color.FromRgb(63, 63, 70)),
                BorderThickness = new Thickness(1, 1, 1, 1),
                Padding = new Thickness(8, 8, 8, 8),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10,
                Margin = new Thickness(0, 0, 0, 5)
            };
            Grid.SetRow(apiKeyPasswordBox, 4);
            mainGrid.Children.Add(apiKeyPasswordBox);

            // API Key input (TextBox - shown when "show password" is checked)
            apiKeyTextBox = new TextBox
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Color.FromRgb(63, 63, 70)),
                BorderThickness = new Thickness(1, 1, 1, 1),
                Padding = new Thickness(8, 8, 8, 8),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10,
                Margin = new Thickness(0, 0, 0, 5),
                Visibility = Visibility.Collapsed
            };
            Grid.SetRow(apiKeyTextBox, 4);
            mainGrid.Children.Add(apiKeyTextBox);

            // Show/Hide password checkbox
            showPasswordCheckBox = new CheckBox
            {
                Content = "Show API Key",
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                Margin = new Thickness(0, 0, 0, 10)
            };
            showPasswordCheckBox.Checked += ShowPasswordCheckBox_Checked;
            showPasswordCheckBox.Unchecked += ShowPasswordCheckBox_Unchecked;
            Grid.SetRow(showPasswordCheckBox, 5);
            mainGrid.Children.Add(showPasswordCheckBox);

            // Status text
            statusText = new TextBlock
            {
                Text = "",
                Foreground = new SolidColorBrush(Colors.Red),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 10, 0, 0)
            };
            Grid.SetRow(statusText, 6);
            mainGrid.Children.Add(statusText);

            // Buttons
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0)
            };

            cancelButton = new Button
            {
                Content = "Cancel",
                Width = 80,
                Height = 30,
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(1, 1, 1, 1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                Margin = new Thickness(0, 0, 10, 0)
            };
            cancelButton.Click += CancelButton_Click;

            okButton = new Button
            {
                Content = "Save",
                Width = 80,
                Height = 30,
                Background = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0, 0, 0, 0)
            };
            okButton.Click += OkButton_Click;

            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(okButton);

            Grid.SetRow(buttonPanel, 8);
            mainGrid.Children.Add(buttonPanel);

            Content = mainGrid;
        }

        private void ShowPasswordCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            apiKeyTextBox.Text = apiKeyPasswordBox.Password;
            apiKeyTextBox.Visibility = Visibility.Visible;
            apiKeyPasswordBox.Visibility = Visibility.Collapsed;
        }

        private void ShowPasswordCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            apiKeyPasswordBox.Password = apiKeyTextBox.Text;
            apiKeyPasswordBox.Visibility = Visibility.Visible;
            apiKeyTextBox.Visibility = Visibility.Collapsed;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var key = showPasswordCheckBox.IsChecked == true ? apiKeyTextBox.Text : apiKeyPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(key))
            {
                statusText.Text = "Please enter an API key.";
                return;
            }

            if (!key.StartsWith("sk-"))
            {
                statusText.Text = "API key should start with 'sk-'. Please check your key.";
                return;
            }

            ApiKey = key.Trim();
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}