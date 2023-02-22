using System;
using Binance.Net.Interfaces;

public sealed class Trade : ITrade
{
    public bool IsBuy { get; set; }
    public decimal Price { get; set; }
    public decimal Amount { get; set; }
    public DateTime DateTime { get; set; }
}

public static class TradeExtensions
{
    // https://money.stackexchange.com/q/90686
    public static Trade ToTrade(this IBinanceTrade data)
    {
        return new Trade
        {
            IsBuy = data.BuyerIsMaker == false,
            Price = data.Price,
            Amount = data.Quantity,
            DateTime = data.TradeTime,
        };
    }

    public static Trade ToTrade(this IBinanceRecentTrade data)
    {
        return new Trade
        {
            IsBuy = data.BuyerIsMaker == false,
            Price = data.Price,
            Amount = data.BaseQuantity,
            DateTime = data.TradeTime,
        };
    }
}

public interface ITrade
{
    decimal Price { get; }
    decimal Amount { get; }
    bool IsBuy { get; }
    DateTime DateTime { get; }
}