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
        
    void Start() => InitializeAsync().Forget();

    private async UniTask InitializeAsync()
    {
        // TBD: add normal DI
        var tradesConfig = new TradesConfiguration();
        var tradesModel = new TradesModel(new ObservableCircularBuffer<Trade>(tradesConfig.TradesCountLimit));
        var binanceClients = (
            new BinanceSocketClient(BinanceSocketClientOptions.Default),
            new BinanceClient(BinanceClientOptions.Default));

        object tradesService;

        if (_isTesting)
        {
            var fakeTradesService = new FakeTradesService(tradesConfig, tradesModel, _timer);
            tradesService = fakeTradesService;
        }
        else
        {
            tradesService = new TradesService(
                tradesConfig,
                binanceClients.Item1,
                binanceClients.Item2,
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
