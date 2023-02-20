// The thread synchronization gets really complicated and can be entirely avoided
// as long as one never calls the Update method when TradesAreLoading is true,
// and as long as the Update is started in a single thread with the UI
// (the task creation is done in the same thread).
// In theory it should be fine without this.
// #define THREAD_SYNCH

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Binance.Net.Clients;
using Binance.Net.Interfaces;
using CommunityToolkit.Mvvm.Messaging;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;

public sealed class TradesConfiguration
{
    public int TradesCountLimit { get; set; } = 1000;
}

public sealed class ReloadTradesMessage
{
    
}

public sealed class TradesService : IDisposable, IRecipient<ReloadTradesMessage>
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
    
    public void Receive(ReloadTradesMessage message)
    {
        UpdateTrades().Forget();
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
            $"{_model.CurrencyNames.Item1}{_model.CurrencyNames.Item2}",
            $"{_model.CurrencyNames.Item2}{_model.CurrencyNames.Item1}",
        };

        if (token.IsCancellationRequested)
            return;
        
        {
            var initTask = _binanceClient.SpotApi.ExchangeData.GetRecentTradesAsync(
                symbols[0], _config.TradesCountLimit, token);
            var result = await initTask;
            if (!result.Success)
                throw new Exception(result.Error?.Message);

            _model.Trades.Clear();
            foreach (var trade in result.Data)
                _model.Trades.Add(trade.ToTrade());
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
    
    private void OnNextTrade(IBinanceTrade trade)
    {
        if (_model.TradesAreLoading)
            return;
        
        var tradeModel = trade.ToTrade();
        var trades = _model.Trades;
        
        while (trades.Count >= _config.TradesCountLimit)
            trades.RemoveAt(0);
        trades.Add(tradeModel);
    }

    public void Dispose()
    {
        _cts?.Dispose();
    }
}