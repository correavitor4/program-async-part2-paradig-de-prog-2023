using System.Text.Json.Serialization;

namespace program_paralel_paradigs_de_prog_2023.types;

public struct GetCoinsDto
{
    [JsonPropertyName("data")]
    public GetCoinsDtoData Data { get; set; }
}

public struct GetCoinsDtoData
{
    [JsonPropertyName("stats")]
    public GetCoinsDtoDataStats Stats { get; set; }
    [JsonPropertyName("coins")]
    public GetCoinsDtoDataCoin[] Coins { get; set; }
}

public struct GetCoinsDtoDataStats
{
    [JsonPropertyName("totalCoins")]
    public int TotalCoins { get; set; }
}

public struct GetCoinsDtoDataCoin
{
    [JsonPropertyName("uuid")]
    public string Uuid { get; set; }
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("price")]
    public string Price { get; set; }
    [JsonPropertyName("sparkline")]
    public List<string>? SparkLine { get; set; }
}