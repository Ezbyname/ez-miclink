namespace BluetoothMicrophoneApp.UI;

/// <summary>
/// E-z MicLink Design System - Consistent colors, styles, and spacing
/// </summary>
public static class DesignSystem
{
    // Color Palette
    public static class Colors
    {
        // Backgrounds
        public const string DarkBackground = "#0F0F1E";
        public const string CardBackground = "#1A1A2E";
        public const string GlassBackground = "#1E1E38";
        public const string DialogBackground = "#0F0F1E";
        public const string DialogOverlay = "#000000CC";

        // Accents
        public const string PrimaryBlue = "#4A90E2";
        public const string NeonBlue = "#5B9CFF";
        public const string NeonPink = "#FF5B9C";
        public const string NeonPurple = "#A855F7";
        public const string SuccessGreen = "#4CAF50";
        public const string WarningOrange = "#FF9800";
        public const string ErrorRed = "#FF5252";

        // Text
        public const string TextPrimary = "#FFFFFF";
        public const string TextSecondary = "#8E8E93";
        public const string TextTertiary = "#666666";

        // Borders & Shadows
        public const string BorderBlue = "#4A90E2";
        public const string BorderGreen = "#4CAF50";
        public const string BorderOrange = "#FF9800";
        public const string BorderRed = "#FF5252";
        public const string ShadowColor = "#000000";
    }

    // Typography
    public static class Typography
    {
        public const double TitleLarge = 32;
        public const double TitleMedium = 24;
        public const double TitleSmall = 20;
        public const double BodyLarge = 18;
        public const double BodyMedium = 16;
        public const double BodySmall = 14;
        public const double CaptionLarge = 13;
        public const double CaptionSmall = 12;
        public const double Tiny = 10;
    }

    // Spacing
    public static class Spacing
    {
        public const double XXSmall = 4;
        public const double XSmall = 8;
        public const double Small = 12;
        public const double Medium = 16;
        public const double Large = 20;
        public const double XLarge = 24;
        public const double XXLarge = 32;
    }

    // Corner Radius
    public static class CornerRadius
    {
        public const double Small = 12;
        public const double Medium = 15;
        public const double Large = 20;
        public const double XLarge = 25;
        public const double Round = 30;
    }

    // Icons (emoji alternatives for consistent design)
    public static class Icons
    {
        public const string Bluetooth = "🔵";
        public const string BluetoothConnected = "🔗";
        public const string Scan = "🔍";
        public const string Speaker = "🔊";
        public const string Microphone = "🎤";
        public const string Battery = "🔋";
        public const string Signal = "📶";
        public const string Warning = "⚠️";
        public const string Error = "❌";
        public const string Success = "✓";
        public const string Info = "ℹ️";
        public const string Disconnect = "⛓️‍💥";
        public const string Home = "🏠";
        public const string Settings = "⚙️";
        public const string Diagnostics = "🔧";
        public const string Logs = "📋";
    }

    // Animation Durations
    public static class Animation
    {
        public const uint Fast = 150;
        public const uint Normal = 250;
        public const uint Slow = 350;
        public const uint VerySlow = 500;
    }
}
