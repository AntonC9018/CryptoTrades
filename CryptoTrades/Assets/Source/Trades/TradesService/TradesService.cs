// The thread synchronization gets really complicated and can be entirely avoided
// as long as one never calls the Update method when TradesAreLoading is true,
// and as long as the Update is started in a single thread with the UI
// (the task creation is done in the same thread).
// In theory it should be fine without this.
// #define THREAD_SYNCH

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Binance.Net.Clients;
using Binance.Net.Interfaces;
using CommunityToolkit.Mvvm.Messaging;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;

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
    
    private readonly ICurrencySymbolMapper _symbolMapper;

    public TradesService(
        TradesConfiguration tradesConfig,
        BinanceSocketClient binanceSocketClient,
        BinanceClient binanceClient,
        ICurrencySymbolMapper symbolMapper,
        TradesModel model,
        CancellationToken cancellationToken)
    {
        _config = tradesConfig;
        _binanceClient = binanceClient;
        _binanceSocketClient = binanceSocketClient;
        _cancellationToken = cancellationToken;
        _model = model;
        _symbolMapper = symbolMapper;
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
        string symbol = await _symbolMapper.GetSymbol(_model.CurrencyNames);

        if (token.IsCancellationRequested)
            return;
        
        {
            var initTask = _binanceClient.SpotApi.ExchangeData.GetRecentTradesAsync(
                symbol, _config.TradesCountLimit, token);
            var result = await initTask;
            if (!result.Success)
                throw new BinanceApiCallResultException(result);

            _model.Trades.Clear();
            _model.Trades.PushFrontN(result.Data.Select(t => t.ToTrade()).ToArray());
        }
        
        if (token.IsCancellationRequested)
            return;

        {
            var subscriptionTask = _binanceSocketClient.SpotStreams.SubscribeToTradeUpdatesAsync(
                symbol, e => OnNextTrade(e.Data), token);
            var subscriptionResult = await subscriptionTask;

            if (!subscriptionResult.Success)
                throw new BinanceApiCallResultException(subscriptionResult);
        }
    }
    
    private void OnNextTrade(IBinanceTrade trade)
    {
        if (_model.TradesAreLoading)
            return;
        
        var tradeModel = trade.ToTrade();
        var trades = _model.Trades;
        
        // We're pushing from only one thread so it's fine not to lock.
        trades.PushFront(tradeModel);
    }

    public void Dispose()
    {
        _cts?.Dispose();
    }
}