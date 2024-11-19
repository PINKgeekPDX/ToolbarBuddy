using System.Text.Json.Serialization;

namespace ToolBarApp.Models
{
    public class SingleToolbarConfig
    {
        public bool IsAlwaysOnTop { get; init; }
        public bool IsPinned { get; init; }

        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;

        [JsonPropertyName("position")]
        public string Position { get; init; } = "Top";

        [JsonPropertyName("isVisible")]
        public bool IsVisible { get; init; } = true;

        [JsonPropertyName("buttons")]
        public List<ButtonConfig> Buttons { get; init; } = new();

        [JsonPropertyName("theme")]
        public string Theme { get; init; } = "default";
    }
}