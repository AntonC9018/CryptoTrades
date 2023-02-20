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
        var root = GetComponent<UIRoot>();
        var serviceProvider = new ServiceProvider();
        
        // TODO: add normal DI
        serviceProvider.RegisterService(new TradesModel
        {
            CurrencyName1 = "BTC",
            CurrencyName2 = "TBC",
        });
        serviceProvider.RegisterService(new TradesConfiguration());
        serviceProvider.RegisterService(new TradesService(
            serviceProvider.GetRequiredService<TradesConfiguration>(),
            new BinanceSocketClient(new BinanceSocketClientOptions()),
            new BinanceClient(new BinanceClientOptions()),
            serviceProvider.GetRequiredService<TradesModel>(),
            default));

        var messenger = new StrongReferenceMessenger();
        messenger.RegisterAll(serviceProvider.GetRequiredService<TradesService>());

        await root.Initialize(messenger, serviceProvider);
        messenger.Send<OpenTradesTableMessage>();
    }
}
