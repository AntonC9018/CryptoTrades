using System;
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
    private readonly ICurrencySymbolMapper _symbolMapper;
    private readonly SemaphoreSlim _semaphoreTakeOver = new(initialCount: 1, maxCount: 1); 
    
    [CanBeNull] private CancellationTokenSource _cts;

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
        var cts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken);
        var token = cts.Token;
        
        // Since only one process can be loading at the same time, we have to wait on another lock.
        // Just awaiting a task will not be enough, because resetting the task will also have
        // to be locked between multiple threads.
        await _semaphoreTakeOver.WaitAsync(token);
        
        try
        {
            
            // Set this variable ASAP.
            _model.TradesAreLoading = true;
            
            {
                // Cancel the current loading process or the websocket streams.
                // If we happen to be reloading the data at this time, that means the current data is fresher,
                // so the previous call has to be interrupted.
                _cts?.Cancel();
                _cts?.Dispose();
                _cts = cts;
            }
        }
        finally
        {
            _semaphoreTakeOver.Release();
        }
        
        // Here, we actually do the logic.
        // This has to be under a different critical section, because the previous critical section
        // should be able to short-circuit the things done in this one.
        // And this needs to be a critical section since two threads must not start loading
        // at the same time.
        await _model.TradesSemaphore.WaitAsync(token);

        try
        {
            // Some other thread took over.
            if (token.IsCancellationRequested)
            {
                return;
            }

            try
            {
                await UpdateTradesInternal(token);
            }
            catch
            {
                _model.Trades.Clear();
                throw;
            }
            // This is the main reason why we need the critical section.
            // Without the critical section, this bit could run after the new
            // thread has already loaded the initial batch of data.
            if (token.IsCancellationRequested)
                _model.Trades.Clear();
        }
        finally
        {
            _model.TradesSemaphore.Release();
            
            // If the token has been cancelled by another thread, that means that thread has taken over.
            // But in the case when the token has been cancelled from somewhere else, we have to unset the
            // TradesAreLoading flag. The only way to truly check if the loading has been taken over by
            // another thread is to check the cts.
            // We don't use the token here, because if it happens to have been cancelled from the outside,
            // the loading variable won't be unset.
            await _semaphoreTakeOver.WaitAsync();
            try
            {
                    
                // cts is only changed inside a section that relies on the same semaphore.
                // If another thread has started loading, or is going to start loading, that means it's past the
                // cts change, which means it's taken over already.
                if (_cts == cts)
                    _model.TradesAreLoading = false;
            }
            finally
            {
                _semaphoreTakeOver.Release();
            }
        }
    }
    
    private async UniTask UpdateTradesInternal(CancellationToken token)
    {
        string symbol = await _symbolMapper.GetSymbol(_model.CurrencyNames);

        if (token.IsCancellationRequested)
        {
            return;
        }
        
        {
            var initTask = _binanceClient.SpotApi.ExchangeData.GetRecentTradesAsync(
                symbol, _config.TradesCountLimit, token);
            var result = await initTask;
            if (!result.Success)
            {
                throw new BinanceApiCallResultException(result);
            }

            _model.Trades.Clear();
            _model.Trades.PushFrontN(result.Data.Select(t => t.ToTrade()).ToArray());
        }

        if (token.IsCancellationRequested)
        {
            return;
        }

        {
            // We have to either capture the cancellation token here, or take the cts lock in the callback.
            var subscriptionTask = _binanceSocketClient.SpotStreams.SubscribeToTradeUpdatesAsync(
                symbol, e => OnNextTrade(e.Data, token).Forget(), token);
            var subscriptionResult = await subscriptionTask;
            if (!subscriptionResult.Success)
            {
                throw new BinanceApiCallResultException(subscriptionResult);
            }
        }
    }
    
    private async UniTask OnNextTrade(IBinanceTrade trade, CancellationToken token)
    {
        // I think this is called on the same thread each time (I'm 99% sure),
        // so we don't have to have another critical section here.
        if (_model.TradesAreLoading)
        {
            return;
        }

        await _model.ModifyTradesAsync(trades =>
        {
            if (_model.TradesAreLoading)
            {
                return;
            }
            trades.PushFront(trade.ToTrade());
        }, token);
    }

    public void Dispose()
    {
        _cts?.Dispose();
    }
}