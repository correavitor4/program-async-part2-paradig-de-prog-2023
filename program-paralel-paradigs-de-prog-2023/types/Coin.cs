using System.Text.Json.Serialization;

namespace program_paralel_paradigs_de_prog_2023.types;

public class Coin
{
    [JsonPropertyName("uuid")]
    public string Uuid { get; set; }
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("price")]
    public float Price { get; set; }
}