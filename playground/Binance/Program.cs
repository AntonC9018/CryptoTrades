using Binance.Net.Clients;
using Binance.Net.Objects;
using CryptoExchange.Net;
using Microsoft.Extensions.Logging;

if (false)
{
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
}

if (true)
{
    var client = new BinanceSocketClient(new BinanceSocketClientOptions
    {
        LogLevel = LogLevel.Debug,
        SpotStreamsOptions = new()
        {
            AutoReconnect = false,
        },
    });
    var result = await client.SpotStreams.SubscribeToTradeUpdatesAsync("BNBBTC", (e) =>
    {
        var data = e.Data;
        Console.WriteLine($"Price: {data.Price}, Quantity: {data.Quantity}, Time: {data.TradeTime}, {data.BuyerIsMaker}");
    });
    if (result.Success)
    {
        Console.WriteLine("Subscribed");
    }
    else
    {
        Console.WriteLine($"Error: {result.Error}");
    }
}