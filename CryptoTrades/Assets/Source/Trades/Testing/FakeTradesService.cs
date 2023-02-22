using System;
using System.Linq;
using CommunityToolkit.Mvvm.Messaging;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using Timer = System.Timers.Timer;

// Used for testing.
// Since this is a demo project, I didn't bother setting up better testing.
public class FakeTradesService : IDisposable, IRecipient<ReloadTradesMessage>
{
    private readonly TradesConfiguration _config;
    private readonly TradesModel _model;
    private readonly UnityTimer _timer;
    
    public FakeTradesService(TradesConfiguration config, TradesModel model, UnityTimer unityTimer)
    {
        _config = config;
        _timer = unityTimer;
        _model = model;
        
        _timer.StopTimer();
        _timer.Elapsed += () =>
        {
            _model.Trades.PushFront(GetTrade());
        };
    }

    private Trade GetTrade()
    {
        return new Trade
        {
            Amount = Random.Range(0, 10),
            Price = Random.Range(0, 10),
            DateTime = DateTime.Now,
            IsBuy = Random.value > 0.5,
        };
    }
    
    public void Receive(ReloadTradesMessage message)
    {
        _timer.StopTimer();
        
        _model.Trades.Clear();
        _model.Trades.PushFrontN(Enumerable.Range(0, _config.TradesCountLimit).Select(i => GetTrade()).ToArray());

        _timer.StartTimer();
    }

    public void Dispose()
    {
        Object.Destroy(_timer);
    }
}
