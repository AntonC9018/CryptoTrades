using System;

public sealed class Trade
{
    public bool IsBuy { get; set; }
    public decimal Price { get; set; }
    public decimal Amount { get; set; }
    public DateTime DateTime { get; set; }
}