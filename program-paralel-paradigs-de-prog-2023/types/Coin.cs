using System.Text.Json.Serialization;

namespace program_paralel_paradigs_de_prog_2023.types;

public class Coin
{
    public string Uuid { get; set; }
    public string Symbol { get; set; }
    public string Name { get; set; }
    public float Price { get; set; }
    public List<decimal>? SparkLine { get; set; }
}