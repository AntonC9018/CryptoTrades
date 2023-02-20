using System.Threading;
using System.Threading.Tasks;
using Binance.Net.Clients;
using Binance.Net.Objects;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class Test : MonoBehaviour
{
    private CancellationTokenSource _cts;
    private UIDocument _uiDocument;
    
    void Start()
    {
        _uiDocument = GetComponent<UIDocument>();
    }
        
    async Task Stuff()
    {
        var binanceSocketClient = new BinanceSocketClient(new BinanceSocketClientOptions
        {
            // Set options here for this client
        });
        var binanceClient = new BinanceClient(new BinanceClientOptions
        {
            
        });
        _cts = new CancellationTokenSource();
        // binanceSocketClient.SpotStreams.SubscribeToTradeUpdatesAsync(
        //     new string[] {},
        //     e =>
        //     {
        //         Debug.Log(e);
        //     },
        //     _cts.Token);
        var spotSymbolData = await binanceClient.SpotApi.ExchangeData.GetExchangeInfoAsync(_cts.Token);
        var s = spotSymbolData.Data.Symbols;
    }
}