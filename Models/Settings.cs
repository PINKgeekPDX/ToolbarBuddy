namespace ToolBarApp.Models
{
    public class Settings
    {
        public bool IsToolbarPinned { get; init; } = false;
        public bool IsAlwaysOnTop { get; init; } = false;
        public string TerminalPosition { get; init; } = "Top";
        public ToolbarConfig ToolbarConfig { get; init; } = new();
        public TerminalSettings TerminalSettings { get; init; } = new();
        public string SelectedTheme { get; init; } = "Default";
    }
    public class TerminalSettings
    {
        public bool ShowInfo { get; init; } = true;
        public bool ShowScript { get; init; } = true;
        public bool ShowWarning { get; init; } = true;
        public bool ShowError { get; init; } = true;
    }
}
