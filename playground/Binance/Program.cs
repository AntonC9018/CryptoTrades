using Binance.Net.Clients;
using Binance.Net.Objects;
using CryptoExchange.Net;

var client = new BinanceClient(BinanceClientOptions.Default);
var result = await client.SpotApi.ExchangeData.GetRecentTradesAsync("BNBBTC");
if (result.Success)
{
    foreach (var trade in result.Data)
    {
        Console.WriteLine($"Price: {trade.Price}, Quantity: {trade.BaseQuantity}, Time: {trade.TradeTime}, {trade.BuyerIsMaker}");
    }
}
else
{
    Console.WriteLine($"Error: {result.Error}");
}