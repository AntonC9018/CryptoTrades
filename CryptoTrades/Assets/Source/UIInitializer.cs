using Binance.Net.Clients;
using Binance.Net.Objects;
using CommunityToolkit.Mvvm.Messaging;
using Cysharp.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MVVMToolkit;
using MVVMToolkit.DependencyInjection;
using UnityEngine;
using Utils;

public class OpenTradesTableMessage
{
}

public class UIInitializer : MonoBehaviour
{
    [SerializeField] private UnityTimer _timer;
    [SerializeField] private bool _isTesting;
    [SerializeField] private TradesConfiguration _tradesConfiguration;
        
    void Start() => InitializeAsync().Forget();

    private async UniTask InitializeAsync()
    {
        // TBD: add normal DI
        var tradesConfig = _tradesConfiguration;
        var tradesModel = new TradesModel(new ObservableCircularBuffer<Trade>(tradesConfig.TradesCountLimit));

        object tradesService;

        if (_isTesting)
        {
            var fakeTradesService = new FakeTradesService(tradesConfig, tradesModel, _timer);
            tradesService = fakeTradesService;
        }
        else
        {
            var binanceClients = (
                socket: new BinanceSocketClient(BinanceSocketClientOptions.Default),
                regular: new BinanceClient(BinanceClientOptions.Default));
            var currencySymbolMapper = new CurrencySymbolMapper(binanceClients.regular, default);
            tradesService = new TradesService(
                tradesConfig,
                binanceClients.socket,
                binanceClients.regular,
                currencySymbolMapper,
                tradesModel,
                default);
        }
        
        
        var root = GetComponent<UIRoot>();
        var serviceProvider = new ServiceProvider();
        serviceProvider.RegisterService(tradesModel);

        var messenger = new StrongReferenceMessenger();
        messenger.RegisterAll(tradesService);

        await root.Initialize(messenger, serviceProvider);
        messenger.Send<OpenTradesTableMessage>();
    }
}
