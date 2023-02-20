using Binance.Net.Clients;
using Binance.Net.Objects;
using CommunityToolkit.Mvvm.Messaging;
using Cysharp.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MVVMToolkit;
using MVVMToolkit.DependencyInjection;
using UnityEngine;

public class OpenTradesTableMessage
{
}

public class UIInitializer : MonoBehaviour
{
    void Start() => InitializeAsync().Forget();

    private async UniTask InitializeAsync()
    {
        // TBD: add normal DI
        var tradesModel = new TradesModel();
        var tradesConfig = new TradesConfiguration();
        var binanceClients = (
            new BinanceSocketClient(BinanceSocketClientOptions.Default),
            new BinanceClient(BinanceClientOptions.Default));

        var tradesService = new TradesService(
            tradesConfig,
            binanceClients.Item1,
            binanceClients.Item2,
            tradesModel,
            default);
        
        var root = GetComponent<UIRoot>();
        var serviceProvider = new ServiceProvider();
        serviceProvider.RegisterService(tradesModel);

        var messenger = new StrongReferenceMessenger();
        messenger.RegisterAll(tradesService);

        await root.Initialize(messenger, serviceProvider);
        messenger.Send<OpenTradesTableMessage>();
    }
}
