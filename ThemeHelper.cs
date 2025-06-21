using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Windows.Media;

namespace ClaudeAI
{
    /// <summary>
    /// Helper class to detect and provide Visual Studio theme colors
    /// </summary>
    public static class ThemeHelper
    {
        /// <summary>
        /// Gets the current Visual Studio theme colors
        /// </summary>
        public static VSThemeColors GetCurrentTheme()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                // Get the VS environment colors using the simpler approach
                var vsUIShell = ServiceProvider.GlobalProvider.GetService(typeof(SVsUIShell)) as IVsUIShell2;

                if (vsUIShell != null)
                {
                    // Get basic colors that are available in VS SDK
                    var backgroundColor = GetVSColor(vsUIShell, __VSSYSCOLOREX.VSCOLOR_TOOLWINDOW_BACKGROUND);
                    var foregroundColor = GetVSColor(vsUIShell, __VSSYSCOLOREX.VSCOLOR_TOOLWINDOW_TEXT);

                    // Determine if it's a light theme
                    bool isLightTheme = IsLightColor(backgroundColor);

                    // Create appropriate colors based on the detected theme
                    var buttonBackground = isLightTheme ?
                        Color.FromRgb(225, 225, 225) : Color.FromRgb(62, 62, 64);
                    var textBoxBackground = isLightTheme ?
                        Color.FromRgb(255, 255, 255) : Color.FromRgb(51, 51, 55);

                    return new VSThemeColors
                    {
                        BackgroundColor = backgroundColor,
                        ForegroundColor = foregroundColor,
                        AccentColor = Color.FromRgb(0, 122, 204),
                        ToolWindowBackground = backgroundColor,
                        ToolWindowText = foregroundColor,
                        ButtonBackground = buttonBackground,
                        TextBoxBackground = textBoxBackground,
                        IsLightTheme = isLightTheme
                    };
                }
            }
            catch (Exception)
            {
                // Fallback to default theme detection
            }

            // Try to detect theme by checking if we're in dark mode
            bool isDarkTheme = IsSystemInDarkMode();

            if (isDarkTheme)
            {
                // VS Dark theme colors
                return new VSThemeColors
                {
                    BackgroundColor = Color.FromRgb(37, 37, 38),
                    ForegroundColor = Color.FromRgb(241, 241, 241),
                    AccentColor = Color.FromRgb(0, 122, 204),
                    ToolWindowBackground = Color.FromRgb(45, 45, 48),
                    ToolWindowText = Color.FromRgb(241, 241, 241),
                    ButtonBackground = Color.FromRgb(62, 62, 64),
                    TextBoxBackground = Color.FromRgb(51, 51, 55),
                    IsLightTheme = false
                };
            }
            else
            {
                // VS Light theme colors
                return new VSThemeColors
                {
                    BackgroundColor = Color.FromRgb(238, 238, 242),
                    ForegroundColor = Color.FromRgb(30, 30, 30),
                    AccentColor = Color.FromRgb(0, 122, 204),
                    ToolWindowBackground = Color.FromRgb(246, 246, 246),
                    ToolWindowText = Color.FromRgb(30, 30, 30),
                    ButtonBackground = Color.FromRgb(225, 225, 225),
                    TextBoxBackground = Color.FromRgb(255, 255, 255),
                    IsLightTheme = true
                };
            }
        }

        private static Color GetVSColor(IVsUIShell2 vsUIShell, __VSSYSCOLOREX colorId)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                uint colorValue;
                if (vsUIShell.GetVSSysColorEx((int)colorId, out colorValue) == 0)
                {
                    // Convert from COLORREF (0x00BBGGRR) to Color
                    byte r = (byte)(colorValue & 0xFF);
                    byte g = (byte)((colorValue >> 8) & 0xFF);
                    byte b = (byte)((colorValue >> 16) & 0xFF);
                    return Color.FromRgb(r, g, b);
                }
            }
            catch (Exception)
            {
                // Ignore and use fallback
            }

            // Fallback color
            return Color.FromRgb(45, 45, 48);
        }

        private static bool IsLightColor(Color color)
        {
            // Calculate luminance to determine if it's a light or dark color
            double luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255;
            return luminance > 0.5;
        }

        private static bool IsSystemInDarkMode()
        {
            try
            {
                // Try to detect Windows 10/11 dark mode
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    var value = key?.GetValue("AppsUseLightTheme");
                    if (value is int intValue)
                    {
                        return intValue == 0; // 0 = dark mode, 1 = light mode
                    }
                }
            }
            catch (Exception)
            {
                // Fallback to dark theme if we can't detect
            }

            return true; // Default to dark theme
        }
    }

    /// <summary>
    /// Container for Visual Studio theme colors
    /// </summary>
    public class VSThemeColors
    {
        public Color BackgroundColor { get; set; }
        public Color ForegroundColor { get; set; }
        public Color AccentColor { get; set; }
        public Color ToolWindowBackground { get; set; }
        public Color ToolWindowText { get; set; }
        public Color ButtonBackground { get; set; }
        public Color TextBoxBackground { get; set; }
        public bool IsLightTheme { get; set; }

        public SolidColorBrush BackgroundBrush => new SolidColorBrush(BackgroundColor);
        public SolidColorBrush ForegroundBrush => new SolidColorBrush(ForegroundColor);
        public SolidColorBrush AccentBrush => new SolidColorBrush(AccentColor);
        public SolidColorBrush ToolWindowBackgroundBrush => new SolidColorBrush(ToolWindowBackground);
        public SolidColorBrush ToolWindowTextBrush => new SolidColorBrush(ToolWindowText);
        public SolidColorBrush ButtonBackgroundBrush => new SolidColorBrush(ButtonBackground);
        public SolidColorBrush TextBoxBackgroundBrush => new SolidColorBrush(TextBoxBackground);
    }
}