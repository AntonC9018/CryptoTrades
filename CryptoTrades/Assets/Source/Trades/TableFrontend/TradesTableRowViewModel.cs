using MVVMToolkit;
using UnityEngine.UIElements;

public sealed class TradesTableRowViewModel
{
    public string TradePrice { get; set; }
    public string TradeAmount { get; set; }
    public bool IsBuy { get; set; }
    public string DateTime { get; set; }

    public string TradeKind => IsBuy ? "Buy" : "Sell";
    public string ColorClass => IsBuy ? "buy" : "sell";

    private static readonly string[] _ColorClasses = {"buy", "sell"};
    private void SetColorClass(VisualElement e)
    {
        foreach (var colorClass in _ColorClasses)
            e.RemoveFromClassList(colorClass);
        e.AddToClassList(ColorClass);
    }
    
    public void BindView(VisualElement root)
    {
        var tradePrice = root.Q<Label>("TradePrice"); 
        tradePrice.text = TradePrice;

        var tradeKind = root.Q<Label>("TradeKind");
        tradeKind.text = TradeKind;
        SetColorClass(tradeKind);
        
        root.Q<Label>("TradeAmount").text = TradeAmount;
        root.Q<Label>("DateTime").text = DateTime;
    }
}