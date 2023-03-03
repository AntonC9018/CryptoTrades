using System;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Cysharp.Threading.Tasks;
using Utils;

public sealed partial class TradesModel : ObservableObject
{
    [ObservableProperty] private bool _tradesAreLoading;
    public SemaphoreSlim TradesAreLoadingSemaphore { get; } = new(initialCount: 1, maxCount: 1);
    
    public async UniTask SetTradesAreLoadingAsync(bool value, CancellationToken token)
    {
        await TradesAreLoadingSemaphore.WaitAsync(token);
        TradesAreLoading = value;
        TradesAreLoadingSemaphore.Release();
    }
    
     
    [ObservableProperty] private (string, string) _currencyNames = ("", "");
    public ObservableCircularBuffer<Trade> Trades { get; }
    public SemaphoreSlim TradesSemaphore { get; } = new(initialCount: 1, maxCount: 1);
    
    public async UniTask ModifyTradesAsync(Action<ObservableCircularBuffer<Trade>> action, CancellationToken token)
    {
        await TradesSemaphore.WaitAsync(token);
        action(Trades);
        TradesSemaphore.Release();
    }
    
    public TradesModel(ObservableCircularBuffer<Trade> trades)
    {
        Trades = trades;
    }
}