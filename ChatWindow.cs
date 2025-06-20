using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
        private ScrollViewer scrollViewer;

        public ChatWindowControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Background = new SolidColorBrush(Color.FromRgb(37, 37, 38)); // VS Dark theme background

            // Main grid layout
            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

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
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                BorderThickness = new Thickness(1, 1, 1, 1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(63, 63, 70)),
                Padding = new Thickness(10, 10, 10, 10),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                Text = "Welcome to Claude AI Assistant!\nType your message below and press Enter or click Send.\n\n"
            };

            scrollViewer.Content = chatDisplay;
            Grid.SetRow(scrollViewer, 0);
            mainGrid.Children.Add(scrollViewer);

            // Input area
            var inputGrid = new Grid();
            inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            messageInput = new TextBox
            {
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = 100,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                BorderThickness = new Thickness(1, 1, 1, 1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(63, 63, 70)),
                Padding = new Thickness(8, 8, 8, 8),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 11,
                Margin = new Thickness(10, 5, 5, 10)
            };

            sendButton = new Button
            {
                Content = "Send",
                Width = 60,
                Height = 30,
                Background = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0, 0, 0, 0),
                Margin = new Thickness(5, 5, 10, 10),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 11
            };

            Grid.SetColumn(messageInput, 0);
            Grid.SetColumn(sendButton, 1);
            inputGrid.Children.Add(messageInput);
            inputGrid.Children.Add(sendButton);

            Grid.SetRow(inputGrid, 1);
            mainGrid.Children.Add(inputGrid);

            // Status bar
            var statusLabel = new Label
            {
                Content = "Ready - Enter your message above",
                Background = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
                Foreground = new SolidColorBrush(Colors.White),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 10,
                Padding = new Thickness(10, 3, 10, 3),
                HorizontalContentAlignment = HorizontalAlignment.Left
            };

            Grid.SetRow(statusLabel, 2);
            mainGrid.Children.Add(statusLabel);

            this.Content = mainGrid;

            // Event handlers
            sendButton.Click += SendButton_Click;
            messageInput.KeyDown += MessageInput_KeyDown;

            // Focus on input
            messageInput.Focus();
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

        private void SendMessage()
        {
            var message = messageInput.Text.Trim();
            if (string.IsNullOrEmpty(message))
                return;

            // Add user message to chat
            AppendToChatDisplay($"You: {message}\n\n");

            // Clear input
            messageInput.Text = string.Empty;

            // TODO: Implement Claude AI API call here
            // For now, just show a placeholder response
            AppendToChatDisplay("Claude: Thank you for your message! I'm a placeholder response. " +
                              "To make me functional, you'll need to integrate the Claude AI API.\n\n");

            messageInput.Focus();
        }

        private void AppendToChatDisplay(string text)
        {
            chatDisplay.AppendText(text);
            scrollViewer.ScrollToEnd();
        }
    }
}