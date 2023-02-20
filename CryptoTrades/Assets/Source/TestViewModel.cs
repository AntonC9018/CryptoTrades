// The thread synchronization gets really complicated and can be entirely avoided
// as long as one never calls the Update method when TradesAreLoading is true,
// and as long as the Update is started in a single thread with the UI
// (the task creation is done in the same thread).
// In theory it should be fine without this.
// #define THREAD_SYNCH

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Binance.Net.Clients;
using Binance.Net.Interfaces;
using Binance.Net.Objects.Models.Spot.Socket;
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoExchange.Net.Sockets;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using MVVMToolkit;
using UnityEngine;
using System.Linq;

public partial class RowViewModel : ViewModel
{
    public string TradePrice { get; set; }
    public string TradeAmount { get; set; }
    public bool IsBuy { get; set; }
    public string DateTime { get; set; }
}

public class Trade : ITrade
{
    public bool IsBuy { get; set; }
    public decimal Price { get; set; }
    public decimal Amount { get; set; }
    public DateTime DateTime { get; set; }
}

public static class TradeExtensions
{
    public static Trade ToTrade(this IBinanceTrade data, bool isBuy)
    {
        return new Trade
        {
            IsBuy = isBuy,
            Price = data.Price,
            Amount = data.Quantity,
            DateTime = data.TradeTime,
        };
    }

    public static Trade ToTrade(this IBinanceRecentTrade data, bool isBuy)
    {
        return new Trade
        {
            IsBuy = isBuy,
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

public class TradesConfiguration
{
    public int TradesCountLimit { get; set; } = 1000;
}

public class TradesService
{
    private readonly TradesConfiguration _config;
    private readonly BinanceClient _binanceClient;
    private readonly BinanceSocketClient _binanceSocketClient;
    private readonly CancellationToken _cancellationToken;
    private readonly TradesModel _model;
    
#if THREAD_SYNCH
    private readonly SemaphoreSlim _semaphoreTakeOver = new(initialCount: 0, maxCount: 1); 
    private readonly SemaphoreSlim _semaphoreInitializeTrades = new(initialCount: 0, maxCount: 1); 
#endif    
    
    [CanBeNull] private CancellationTokenSource _cts;
    
    public TradesService(
        TradesConfiguration tradesConfig,
        BinanceSocketClient binanceSocketClient,
        BinanceClient binanceClient,
        TradesModel model,
        CancellationToken cancellationToken)
    {
        _config = tradesConfig;
        _binanceClient = binanceClient;
        _binanceSocketClient = binanceSocketClient;
        _cancellationToken = cancellationToken;
        _model = model;
    }
    
    public TradesService(TradesModel model)
    {
        model.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName is not nameof(model.CurrencyName1) or nameof(model.CurrencyName2))
                return;
        };
    }
    
    public async UniTask UpdateTrades()
    {
        // If we hit this one it's pretty bad.
        if (_model.TradesAreLoading)
            throw new InvalidOperationException("Trades are already loading.");
        
        CancellationToken token;
        _model.TradesAreLoading = true;
        
#if THREAD_SYNCH
        // We assume it has been called previously, which means the current data is fresher,
        // which means we should stop the current call.
        // This also closes all dangling websocket streams.
        try
        {
            await _semaphoreTakeOver.WaitAsync(_cancellationToken);

            _cts?.Cancel();

            var cts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken);
            token = cts.Token;
            _cts = cts;
        }
        catch (Exception)
        {
            _model.TradesAreLoading = false;
            throw;
        }
        finally
        {
            _semaphoreTakeOver.Release();
        }
#else
        _cts?.Cancel();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken);
        token = _cts.Token;
#endif

        try
        {
#if THREAD_SYNCH
            await _semaphoreInitializeTrades.WaitAsync(token);
#endif
            await UpdateTradesInternal(token);
        }
        catch (Exception)
        {
            _model.Trades.Clear();
            throw;
        }
        finally
        {
            _model.TradesAreLoading = false;
#if THREAD_SYNCH
            _semaphoreInitializeTrades.Release();
#endif
        }
    }
    
    private async UniTask UpdateTradesInternal(CancellationToken token)
    {
        string[] symbols =
        {
            $"{_model.CurrencyName2}{_model.CurrencyName1}",
            $"{_model.CurrencyName1}{_model.CurrencyName2}"
        };

        if (token.IsCancellationRequested)
            return;
        
        {
            var initTasks = symbols.Select(
                    s => _binanceClient.SpotApi.ExchangeData.GetRecentTradesAsync(s, _config.TradesCountLimit,
                        token))
                .ToArray();
            await Task.WhenAll(initTasks);
            
            if (token.IsCancellationRequested)
                return;

            var trades = new List<Trade>();
            {
                void AddResults(int index, bool isBuy)
                {
                    var r = initTasks[index].Result;
                    if (!r.Success)
                        throw new Exception(r.Error?.Message);
                    trades.AddRange(r.Data.Select(x => x.ToTrade(isBuy)));
                }

                AddResults(0, true);
                AddResults(1, false);
            }
            trades.Sort((x, y) => x.DateTime.CompareTo(y.DateTime));

            _model.Trades.Clear();
            foreach (var trade in trades)
                _model.Trades.Add(trade);
        }
        
        if (token.IsCancellationRequested)
            return;

        {
            var subscriptionTask = _binanceSocketClient.SpotStreams.SubscribeToTradeUpdatesAsync(
                symbols, e => OnNextTrade(e.Data), token);
            var subscriptionResult = await subscriptionTask;

            if (!subscriptionResult.Success)
                throw new Exception(subscriptionResult.Error?.Message);
        }
    }
    
    public void OnNextTrade(IBinanceTrade trade)
    {
        if (_model.TradesAreLoading)
            return;
        
        var symbol = trade.Symbol;
        bool isBuy = symbol.StartsWith(_model.CurrencyName1) && symbol.EndsWith(_model.CurrencyName2);
        var tradeModel = trade.ToTrade(isBuy);
        _model.Trades.Add(tradeModel);
    }
}

public partial class TradesModel : ObservableObject
{
    [ObservableProperty] private bool _tradesAreLoading;
    [ObservableProperty] private string _currencyName1;
    [ObservableProperty] private string _currencyName2;
    public ObservableCollection<Trade> Trades { get; }
}

public partial class TestViewModel : ViewModel
{
    private 
    [ObservableProperty] private string _currencyName1;
    [ObservableProperty] private string _currencyName2;

    private partial void OnCurrencyName1Changed() => OnCurrencyNameChanged();
    private partial void OnCurrencyName2Changed() => OnCurrencyNameChanged();

    private void OnCurrencyNameChanged()
    {
        
    }

    // For now do this, later either change this to a circular buffer or idk.
    // I'm not sure how INotifyCollectionChanged is supposed to handle inserts at indices.
    // Does is support them only in the form of a Move and then a Set at an index?
    // If so, then it would add no value.
    public ObservableCollection<RowViewModel> Rows { get; } = new();
}