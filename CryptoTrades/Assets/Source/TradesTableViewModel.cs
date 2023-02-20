using System;
using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using MVVMToolkit;
using MVVMToolkit.DependencyInjection;


public partial class TradesTableViewModel : ViewModel
{
    public TradesModel Model { get; private set; }
    
    public string CurrencyName1
    {
        get => Model.CurrencyName1;
        set => SetProperty(Model.CurrencyName1, value, Model, (m, v) => m.CurrencyName1 = v);
    }
    
    public string CurrencyName2
    {
        get => Model.CurrencyName2;
        set => SetProperty(Model.CurrencyName2, value, Model, (m, v) => m.CurrencyName2 = v);
    }

    // For now do this, later either change this to a circular buffer or idk.
    // I'm not sure how INotifyCollectionChanged is supposed to handle inserts at indices.
    // Does is support them only in the form of a Move and then a Set at an index?
    // If so, then it would add no value.
    public ObservableCollection<TradesTableRowViewModel> Rows { get; } = new();
    
    protected override void OnInit()
    {
        Initialize(ServiceProvider.GetRequiredService<TradesModel>());
    }
    
    public void Initialize(TradesModel model)
    {
        Model = model;
        CurrencyName1 = model.CurrencyName1;
        CurrencyName2 = model.CurrencyName2;

        var culture = CultureInfo.CurrentUICulture;
        
        Rows.Clear();
        foreach (var t in model.Trades)
        {
            Rows.Add(new TradesTableRowViewModel
            {
                DateTime = t.ToString(),
                IsBuy = t.IsBuy,
                TradeAmount = t.Amount.ToString(culture),
                TradePrice = Math.Abs(t.Price).ToString(culture),
            });
        }
        
        // Make it respond to events too (borrow the code from my other project).
    }
}