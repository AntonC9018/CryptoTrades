using CommunityToolkit.Mvvm.ComponentModel;
using Utils;

public sealed partial class TradesModel : ObservableObject
{
    [ObservableProperty] private bool _tradesAreLoading;
    [ObservableProperty] private (string, string) _currencyNames = ("", "");
    public ObservableCircularBuffer<Trade> Trades { get; }
    
    public TradesModel(ObservableCircularBuffer<Trade> trades)
    {
        Trades = trades;
    }
}