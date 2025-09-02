using MercadoBitcoin.Client;
using MercadoBitcoin.Client.Generated;
using System.Reflection;

var client = new MercadoBitcoinClient();

// Get properties of CandleData
var candleType = typeof(CandleData);
var candleProps = candleType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

Console.WriteLine("CandleData properties:");
foreach (var prop in candleProps)
{
    Console.WriteLine($"  {prop.Name} ({prop.PropertyType.Name})");
}

// Get properties of ListCandlesResponse
var responseType = typeof(ListCandlesResponse);
var responseProps = responseType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

Console.WriteLine("\nListCandlesResponse properties:");
foreach (var prop in responseProps)
{
    Console.WriteLine($"  {prop.Name} ({prop.PropertyType.Name})");
}
