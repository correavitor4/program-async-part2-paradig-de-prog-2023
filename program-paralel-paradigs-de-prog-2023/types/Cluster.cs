using System.Diagnostics.CodeAnalysis;

namespace program_paralel_paradigs_de_prog_2023.types;

public struct Cluster
{
    public Cluster(
        List<decimal> centroidSparkLine
    )
    {
        CentroidSparkLine = centroidSparkLine;
        Coins = null;
    }
    public List<decimal> CentroidSparkLine { get; set; }
    public List<Coin>? Coins { get; set; }
    
    public Task<decimal> CalculateDistanceToCoin([NotNull] List<decimal> coinSparkLine)
    {
        var centroidSparkLine = CentroidSparkLine ?? throw new Exception("Centroid sparkline is null");
        
        if (coinSparkLine is null) throw new Exception("Coin sparkline is null");
        if (coinSparkLine.Count != 24) throw new Exception("Coin sparkline is not 24");
        
        //Its a euclidean distance
        var task = Task.Run(() =>
        {
            var distance = 
                coinSparkLine
                    .Select((t, i) => 
                        (decimal)Math.Pow((double)(t - centroidSparkLine[i]), 2)).Sum();
            
            return (decimal)Math.Sqrt((double) distance);
        });
        
        return task;
    }
    
    // public Task<List<decimal>> CalculateDistanceToEachPoint(List<Coin> coins)
    // {
    //     var centroidSparkLine = CentroidSparkLine;
    //     var task = Task.Run(() =>
    //     {
    //         var distances = new List<decimal>();
    //         foreach (var coinSparkLine in coins.Select(coin => coin.SparkLine))
    //         {
    //             if (coinSparkLine is null) throw new Exception("Coin sparkline is null");
    //             
    //             //Its a euclidean distance
    //             decimal distance = 
    //                 centroidSparkLine
    //                     .Select((t, i) => 
    //                         (decimal)Math.Pow((double)(t - coinSparkLine[i]), 2)).Sum();
    //
    //             distances.Add((decimal)Math.Sqrt((double) distance));
    //         }
    //         
    //         return distances;
    //     });
    //     
    //     return task;
    // }
}