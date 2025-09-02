using MercadoBitcoin.Client;
using MercadoBitcoin.Client.Generated;
using System.Reflection;

// List all types from MercadoBitcoin.Client.Generated that contain "Candle"
var assembly = typeof(ListCandlesResponse).Assembly;
var types = assembly.GetTypes().Where(t => t.Name.Contains("Candle", StringComparison.OrdinalIgnoreCase)).ToList();

Console.WriteLine($"Types containing 'Candle': {types.Count}");
foreach (var type in types)
{
    Console.WriteLine($"  {type.FullName}");

    var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
    foreach (var prop in props)
    {
        Console.WriteLine($"    - {prop.Name} ({prop.PropertyType.Name})");
    }
    Console.WriteLine();
}
