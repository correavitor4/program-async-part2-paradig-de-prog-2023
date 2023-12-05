﻿// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text.Json;
using program_paralel_paradigs_de_prog_2023.types;

//Const config
const bool shouldCopyMemoryArray = true;
const int numberOfCopies = 10;

const string baseUrl = "https://api.coinranking.com/v2";
const int maxNumberOfCoinsInOneRequest = 100;
const int numberOfCoins = 4000;
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
            SparkLine = coinDto.SparkLine is null ? null : StringSparkLineToDecimal(coinDto.SparkLine)
        }).ToList();
}

List<decimal>? StringSparkLineToDecimal(List<string> sparkline)
{
    var listFloat = new List<decimal>();

    foreach (var spark in sparkline)
    {
        if (string.IsNullOrEmpty(spark)) continue;
        
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
    var memoryArraySize = memoryArray.Count;
    
    // Cluster will start with a random coin value position
    var randomCoinPosition = GetRandomNumber(0, memoryArraySize);
    Coin coin;
    
    do 
    {
        randomCoinPosition = GetRandomNumber(0, memoryArraySize);
        coin = memoryArray[randomCoinPosition];
    } while (coin.SparkLine is null);
    
    if (coin.SparkLine is null) throw new Exception("Coin sparkline is null");
    
    return new Cluster(coin.SparkLine);

}

int GetRandomNumber(int min, int max)
{
    var random = new Random();
    return random.Next(min, max);
}

async Task<List<Cluster>> AssociateCoinsToClusters(List<Cluster> clustersList)
{
    //Will associate each coin to the nearest cluster
    foreach (var coin in memoryArray)
    {
        var tasksList = new List<Task<decimal>>();
        foreach (var cluster in clustersList)
        {
            if (coin.SparkLine is null) throw new Exception("Coin sparkline is null");
            
            var task = cluster.CalculateDistanceToCoin(coin.SparkLine);
            tasksList.Add(task);
        }
        
        await Task.WhenAll(tasksList);
        var distances = tasksList.Select(task => task.Result).ToList();
        if (distances is null) throw new Exception("Distances is null");
        if (distances.Count == 0) throw new Exception("Distances is empty");
        var minDistance = distances.Min();
        var clusterIndex = distances.IndexOf(minDistance);
        var clusterToModify = clustersList[clusterIndex];
        if (clusterToModify.Coins is null) clusterToModify.Coins = new List<Coin>();
        clusterToModify.Coins.Add(coin);
        clustersList[clusterIndex] = clusterToModify;
    }

    return clustersList;
}

async Task<Tuple<List<Cluster>, bool>> CalculateNewCentroidsOfEachCluster(List<Cluster> clusters)
{
    var newClusters = new List<Cluster>();
    var hasChanged = false;
    var taskList = new List<Task>();
    foreach (var cluster in clusters)
    {
        var task = new Task(() =>
        {
            if (cluster.Coins is null) throw new Exception("Cluster coins is null");

            var nDimensions = cluster.Coins[0].SparkLine!.Count;
            var newCentroid = new List<decimal>();
        
            for (var i = 0; i < nDimensions; i++)
            {
                newCentroid.Add(0);
            }
        
            foreach (var coin in cluster.Coins)
            {
                if (coin.SparkLine is null) throw new Exception("Coin sparkline is null");
            
                for (var i = 0; i < nDimensions; i++)
                {
                    newCentroid[i] += coin.SparkLine[i];
                }
            }
        
            for (var i = 0; i < nDimensions; i++)
            {
                newCentroid[i] /= cluster.Coins.Count;
            }
            
            // Compare each centroid of old and new clusters
            hasChanged = !cluster.CentroidSparkLine.SequenceEqual(newCentroid);
        
            newClusters.Add(new Cluster(newCentroid));
        });
    }
    
    foreach (var task in taskList)
    {
        task.Start();
    }

    await Task.WhenAll();

    return new Tuple<List<Cluster>, bool>(newClusters, hasChanged);
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

// Remove coins with null sparklines (if exist)
memoryArray = memoryArray.Where(coin => coin.SparkLine is not null).ToList();

//Remove coins with sparklines with size is not 24
memoryArray = memoryArray.Where(coin => coin.SparkLine.Count == 24).ToList();

if (shouldCopyMemoryArray)
{
    for (var i = 0; i < numberOfCopies; i++)
    {
        memoryArray.AddRange(memoryArray);
    }
}

#region Kmeans

//. Start K-means
// 1. Start clusters with random positions
var clustersList = StartClustersList(numberOfClusters: 10);

// 2. Start iterations
var hasChanged = false;
do
{
    // 2.1 Associate each coin to the nearest cluster
    clustersList = await AssociateCoinsToClusters(clustersList);
    
    // 2.2 Calculate new centroids
    var response = await CalculateNewCentroidsOfEachCluster(clustersList);
    clustersList = response.Item1;
    hasChanged = response.Item2;
} 
while (hasChanged);

Console.WriteLine("Complete");
#endregion