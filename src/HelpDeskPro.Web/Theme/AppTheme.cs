namespace HelpDeskPro.Web.Theme;

using MudBlazor;

public static class AppTheme
{
    public const string Background = "#F7F7F5";
    public const string Surface = "#FFFFFF";
    public const string Border = "#E4E4E0";
    public const string TextPrimary = "#1F1F1F";
    public const string TextSecondary = "#6B6B6B";
    public const string Primary = "#1F4E79";
    public const string PrimaryHover = "#173B5C";

    // Priority Colors
    public const string PriorityCriticalText = "#B42318";
    public const string PriorityCriticalBg = "#FEE4E2";
    public const string PriorityHighText = "#C4320A";
    public const string PriorityHighBg = "#FEEDE6";
    public const string PriorityMediumText = "#B54708";
    public const string PriorityMediumBg = "#FEF6EE";
    public const string PriorityLowText = "#475467";
    public const string PriorityLowBg = "#F2F4F7";

    // Status Colors
    public const string StatusOpenText = "#1F4E79";
    public const string StatusOpenBg = "#EBF3FA";
    public const string StatusInProgressText = "#6941C6";
    public const string StatusInProgressBg = "#F4F0FA";
    public const string StatusPendingText = "#B54708";
    public const string StatusPendingBg = "#FEF6EE";
    public const string StatusResolvedText = "#067647";
    public const string StatusResolvedBg = "#EDF7EE";
    public const string StatusClosedText = "#475467";
    public const string StatusClosedBg = "#F2F4F7";
    public const string StatusCancelledText = "#98A2B3";
    public const string StatusCancelledBg = "#F8F9FA";

    public const string SlaBreachedText = "#B42318";

    public static MudTheme Default => new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = Primary,
            PrimaryDarken = PrimaryHover,
            Background = Background,
            Surface = Surface,
            LinesDefault = Border,
            Divider = Border,
            TextPrimary = TextPrimary,
            TextSecondary = TextSecondary,
            AppbarBackground = Surface,
            AppbarText = TextPrimary,
            DrawerBackground = Surface,
            DrawerText = TextPrimary,
            DrawerIcon = TextSecondary,
            ActionDefault = TextSecondary,
            ActionDisabled = Border,
            TableLines = Border,
            TableStriped = Background
        },
        Typography = new Typography
        {
            Default = new()
            {
                FontFamily = ["IBM Plex Sans", "-apple-system", "BlinkMacSystemFont", "Segoe UI", "Roboto", "sans-serif"],
                FontSize = "14px",
                LineHeight = 1.5
            },
            H6 = new()
            {
                FontFamily = ["IBM Plex Sans", "sans-serif"],
                FontSize = "20px",
                FontWeight = 600,
                LineHeight = 1.3
            },
            Body1 = new()
            {
                FontFamily = ["IBM Plex Sans", "sans-serif"],
                FontSize = "14px",
                LineHeight = 1.5
            },
            Body2 = new()
            {
                FontFamily = ["IBM Plex Sans", "sans-serif"],
                FontSize = "13px",
                LineHeight = 1.4
            },
            Caption = new()
            {
                FontFamily = ["IBM Plex Sans", "sans-serif"],
                FontSize = "12px",
                LineHeight = 1.3
            },
            Button = new()
            {
                FontFamily = ["IBM Plex Sans", "sans-serif"],
                FontSize = "14px",
                FontWeight = 500,
                TextTransform = "none"
            }
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "4px"
        }
    };
}

