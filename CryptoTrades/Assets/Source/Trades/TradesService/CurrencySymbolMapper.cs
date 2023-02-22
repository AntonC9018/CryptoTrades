using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Binance.Net.Clients;
using Binance.Net.Objects.Models.Spot;
using CryptoExchange.Net.Objects;
using Cysharp.Threading.Tasks;

public class CurrencySymbolMapper : ICurrencySymbolMapper
{
    private readonly Task<WebCallResult<BinanceExchangeInfo>> _initializationTask;
    private Dictionary<(string, string), string> _availableSymbols;
    
    public CurrencySymbolMapper(BinanceClient client, CancellationToken cancellationToken)
    {
        _initializationTask = client.SpotApi.ExchangeData.GetExchangeInfoAsync(cancellationToken);
    }
    
    public async UniTask<string> GetSymbol((string baseAsset, string quoteAsset) currencyPair)
    {
        if (_availableSymbols is null)
        {
            var symbolsInfo = await _initializationTask;
            if (!symbolsInfo.Success)
                throw new BinanceApiCallResultException(symbolsInfo);
            _availableSymbols = symbolsInfo.Data.Symbols
                .ToDictionary(s => (s.BaseAsset, s.QuoteAsset), s => s.Name);
        }

        string symbol;
        {
            var (a, b) = currencyPair;
            if (!_availableSymbols.TryGetValue((a, b), out symbol)
                && !_availableSymbols.TryGetValue((b, a), out symbol))
            {
                throw new InvalidOperationException($"Invalid currency pair: {a} and {b}");
            }
        }

        return symbol;
    }
}