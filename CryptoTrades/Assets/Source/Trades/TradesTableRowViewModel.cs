using MVVMToolkit;
using UnityEngine.UIElements;

public sealed class TradesTableRowViewModel
{
    public static readonly string[] ColorClasses = {"buy", "sell"};
    public string TradePrice { get; set; }
    public string TradeAmount { get; set; }
    public bool IsBuy { get; set; }
    public string DateTime { get; set; }

    public string TradeKind => IsBuy ? "Buy" : "Sell";
    public string ColorClass => IsBuy ? "buy" : "sell";

    private static void SetColorClass(VisualElement e, string klass)
    {
        foreach (var colorClass in ColorClasses)
            e.RemoveFromClassList(colorClass);
        e.AddToClassList(klass);
    }
    
    public static void Bind(VisualElement root, TradesTableRowViewModel viewModel)
    {
        var tradePrice = root.Q<Label>("TradePrice"); 
        tradePrice.text = viewModel.TradePrice;

        var tradeKind = root.Q<Label>("TradeKind");
        tradeKind.text = viewModel.TradeKind;
        SetColorClass(tradeKind, viewModel.ColorClass);
        
        root.Q<Label>("TradeAmount").text = viewModel.TradeAmount;
        root.Q<Label>("DateTime").text = viewModel.DateTime;
    }
}