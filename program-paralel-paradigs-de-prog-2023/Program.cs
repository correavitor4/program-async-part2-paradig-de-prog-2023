// See https://aka.ms/new-console-template for more information

using System.Net;
using System.Text.Json;
using program_paralel_paradigs_de_prog_2023.types;

const string baseUrl = "https://api.coinranking.com/v2";
const int maxNumberOfCoinsInOneRequest = 100;
const int numberOfCoins = 2000;
const int numberOfThreads = 5;
const int numberOfIterations = numberOfCoins / maxNumberOfCoinsInOneRequest / numberOfThreads;

//MemoryArray
var memoryArray = new List<Coin>();
var memoryArrayLock = new object();

//Define functions 
#region Funtions

// async Task<int> GetNumberOfCoinsAsync()
// {
//     const string url = baseUrl + "/coins";
//     var httpClient = new HttpClient();
//     var response = await httpClient.GetAsync(url);
//     if (response.StatusCode != HttpStatusCode.OK)
//     {
//         throw new Exception("Error getting number of coins");
//     }
//     var responseString = await response.Content.ReadAsStringAsync();
//     var getCoinsDto = JsonSerializer.Deserialize<GetCoinsDto>(responseString);
//     
//     return getCoinsDto.Data.Stats.TotalCoins;
// }

async Task<List<Coin>> GetCoins(int limit = 50, int offset = 0)
{
    var url = baseUrl + $"/coins?limit={limit}&offset={offset}";
    var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Add("x-rapidapi-key", "e124e6813amshb1ba840f6ba34f3p172562jsn119ca6eab0e4");
    httpClient.DefaultRequestHeaders.Add("x-rapidapi-host", "coinranking1.p.rapidapi.com");

    try
    {
        var response = await httpClient.GetAsync(url);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            if (response.StatusCode != HttpStatusCode.TooManyRequests) throw new Exception("Error getting coins");
            
            // Console.WriteLine("Too many requests, waiting 1 second");
            // await Task.Delay(1000);
            return await GetCoins(limit, offset);
        }
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
    var coins = new List<Coin>();
    foreach (var coinDto in coinsDto)
    {
        var coin = new Coin
        {
            Name = coinDto.Name,
            Symbol = coinDto.Symbol,
            Price = float.Parse(coinDto.Price)
        };
        coins.Add(coin);
    }
    
    return coins;
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


//2. sort coins
foreach (var coin in memoryArray)
{
    Console.WriteLine($"{coin.Name} - {coin.Symbol} - {coin.Price}");
}