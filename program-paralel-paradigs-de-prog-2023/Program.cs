// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text.Json;
using program_paralel_paradigs_de_prog_2023.types;

const string baseUrl = "https://api.coinranking.com/v2";
const int maxNumberOfCoinsInOneRequest = 100;
const int numberOfCoins = 2000;
const int numberOfThreads = 4;
const int numberOfIterations = numberOfCoins / maxNumberOfCoinsInOneRequest / numberOfThreads;

//MemoryArray
var memoryArray = new List<Coin>();
var memoryArrayLock = new object();

//Define functions 
#region Funtions

async Task<List<Coin>> GetCoins(int limit = 50, int offset = 0)
{
    var url = baseUrl + $"/coins?limit={limit}&offset={offset}";
    var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Add("x-access-token", "coinranking30fe1ce33b2846e20bafba208fbf4ee4ac40e3eb891bac29");

    try
    {
        var sw = new Stopwatch();
        sw.Start();
        var response = await httpClient.GetAsync(url);
        sw.Stop();
        if (response.StatusCode != HttpStatusCode.OK)
        {
            if (response.StatusCode != HttpStatusCode.TooManyRequests) throw new Exception("Error getting coins: to many requests");
        }
        
        if (sw.ElapsedMilliseconds > 1000) Console.WriteLine($"Coin ranking demorou {sw.ElapsedMilliseconds}ms para responder");
        
        var responseString = await response.Content.ReadAsStringAsync();
        var getCoinsDto = JsonSerializer.Deserialize<GetCoinsDto>(responseString);

        var coinsDto = getCoinsDto.Data.Coins 
                    ?? throw new Exception("Error getting coins: null");
        var coins = GetCoinsDtoDataCoinsToCoin(coinsDto);
        lock (memoryArrayLock)
        {
            memoryArray.AddRange(coins); 
        }
        
        return coins;
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        throw;
    }
}

List<Coin> GetCoinsDtoDataCoinsToCoin(GetCoinsDtoDataCoin[] coinsDto)
{
    return coinsDto.Select(
        coinDto => new Coin
        {
            Name = coinDto.Name, 
            Symbol = coinDto.Symbol, 
            Price = float.Parse(coinDto.Price), 
            SparkLine = coinDto.SparkLine is null || coinDto.SparkLine.Any(string.IsNullOrEmpty) ? null : StringSparkLineToDecimal(coinDto.SparkLine)
        }).ToList();
}

List<decimal>? StringSparkLineToDecimal(List<string> sparkline)
{
    var listFloat = new List<decimal>();

    foreach (var spark in sparkline)
    {
        decimal valueInDecimal;
        if (decimal.TryParse(spark, NumberStyles.Float, CultureInfo.InvariantCulture, out valueInDecimal))
        {
            // Adicionar à lista
            listFloat.Add(valueInDecimal);
        }
        else
        {
            return null;
        }
    }

    return listFloat;
}

List<Cluster> StartClustersList(int numberOfClusters)
{
    var clustersList = new List<Cluster>();
    for (var i = 0; i < numberOfClusters; i++)
    {
        clustersList.Add(StartClusterWithRandomPosition());
    }

    return clustersList;
}

Cluster StartClusterWithRandomPosition()
{
    decimal minValue = 0;
    decimal maxValue = 0;
    
    foreach (var coin in memoryArray)
    {
        if (coin.MinValueInPeriodTime < minValue)
        {
            minValue = coin.MinValueInPeriodTime;
        }

        if (coin.MaxValueInPeriodTime > maxValue)
        {
            maxValue = coin.MaxValueInPeriodTime;
        }
    }
    
    return new Cluster()
    {
        MinValue = minValue,
        MaxValue = maxValue,
    };
}

#endregion

//1. get coins
for (var i=0; i< numberOfIterations; i++)
{
    var tasks = new List<Task<List<Coin>>>();
    for (var j=0; j< numberOfThreads; j++)
    {
        var task = GetCoins(maxNumberOfCoinsInOneRequest, i * numberOfThreads * maxNumberOfCoinsInOneRequest + j * maxNumberOfCoinsInOneRequest);
        tasks.Add(task);
    }
    await Task.WhenAll(tasks);
    await Task.Delay(1000);
}

// 2. Remove coins with null sparklines (if exist)
memoryArray = memoryArray.Where(coin => coin.SparkLine is not null).ToList();

#region Kmeans

//. Start K-means
var numberOfClusters = 10;
var clustersList = StartClustersList();

//Define clusters random position

#endregion