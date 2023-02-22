using Binance.Net.Interfaces;

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