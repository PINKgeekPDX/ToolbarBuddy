using System.Text.Json.Serialization;

public class AppConfig
{
    [JsonPropertyName("isAlwaysOnTop")]
    public bool IsAlwaysOnTop { get; init; }

    [JsonPropertyName("areAlertsEnabled")]
    public bool AreAlertsEnabled { get; init; }

    [JsonPropertyName("theme")]
    public string Theme { get; init; } = "default";

    [JsonPropertyName("toolbarConfig")]
    public ToolbarConfig ToolbarConfig { get; init; } = new();

    [JsonPropertyName("pluginsEnabled")]
    public bool PluginsEnabled { get; init; }

    [JsonPropertyName("pluginDirectory")]
    public string PluginDirectory { get; init; } = "plugins";

    [JsonPropertyName("logLevel")]
    public string LogLevel { get; init; } = "Information";

    [JsonPropertyName("autoUpdate")]
    public bool AutoUpdate { get; init; } = true;

    [JsonPropertyName("hotkeys")]
    public Dictionary<string, string> Hotkeys { get; init; } = new();

    [JsonPropertyName("windowPosition")]
    public WindowPosition WindowPosition { get; init; } = new();

    public AppConfig()
    {
        IsAlwaysOnTop = false;
        AreAlertsEnabled = true;
        ToolbarConfig = new ToolbarConfig();
        Hotkeys = new Dictionary<string, string>
        {
            { "ToggleToolbar", "Alt+T" },
            { "ShowSettings", "Alt+S" },
            { "ShowTerminal", "Alt+`" }
        };
    }
}

