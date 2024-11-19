using System.Text.Json.Serialization;

namespace ToolBarApp.Models
{
    public class ToolbarConfig
    {
        [JsonPropertyName("toolbars")]
        public List<SingleToolbarConfig> Toolbars { get; init; } = new();

        [JsonPropertyName("buttons")]
        public List<ButtonConfig> Buttons { get; init; } = new();

        [JsonPropertyName("globalSettings")]
        public Dictionary<string, object> GlobalSettings { get; init; } = new();

        [JsonPropertyName("version")]
        public string Version { get; init; } = "1.0";
    }
}
