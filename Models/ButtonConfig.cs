// Models/ButtonConfig.cs
using System.Collections.Generic;
using System.Text.Json.Serialization;
public class ButtonConfig
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("config")]
    public Dictionary<string, object> Config { get; init; } = new();

    [JsonPropertyName("tooltip")]
    public string Tooltip { get; init; } = string.Empty;
}