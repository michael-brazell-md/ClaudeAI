using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;

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
            this.Content = new ChatWindowControl();
        }
    }
}