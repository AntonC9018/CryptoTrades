using MVVMToolkit;

public sealed class TradesTableRowViewModel : ViewModel
{
    public string TradePrice { get; set; }
    public string TradeAmount { get; set; }
    public bool IsBuy { get; set; }
    public string DateTime { get; set; }
}