using MVVMToolkit;
using UnityEngine.UIElements;

public sealed class TradesTableRowViewModel
{
    public string TradePrice { get; set; }
    public string TradeAmount { get; set; }
    public bool IsBuy { get; set; }
    public string DateTime { get; set; }

    public string ColorClass => IsBuy ? "buy" : "sell";
}