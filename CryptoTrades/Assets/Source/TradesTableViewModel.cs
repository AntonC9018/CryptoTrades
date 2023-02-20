﻿using System;
using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using MVVMToolkit;
using Utils;

public partial class TradesTableViewModel : ViewModel
{
    public TradesModel Model { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SomeCurrencyNameChanged))]
    [NotifyPropertyChangedFor(nameof(CanUpdateTrades))]
    [NotifyCanExecuteChangedFor(nameof(UpdateTradesCommand))]
    private string _currencyName1;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SomeCurrencyNameChanged))]
    [NotifyPropertyChangedFor(nameof(CanUpdateTrades))]
    [NotifyCanExecuteChangedFor(nameof(UpdateTradesCommand))]
    private string _currencyName2;

    // For now do this, later either change this to a circular buffer or idk.
    // I'm not sure how INotifyCollectionChanged is supposed to handle inserts at indices.
    // Does is support them only in the form of a Move and then a Set at an index?
    // If so, then it would add no value.
    public ObservableCollection<TradesTableRowViewModel> Rows { get; } = new();
    
    protected override void OnInit()
    {
        Initialize(ServiceProvider.GetRequiredService<TradesModel>());
    }
    
    private void Initialize(TradesModel model)
    {
        Model = model;
        (CurrencyName1, CurrencyName2) = model.CurrencyNames;

        static TradesTableRowViewModel GetRow(Trade t)
        {
            var culture = CultureInfo.CurrentUICulture;
            return new TradesTableRowViewModel
            {
                DateTime = t.ToString(),
                IsBuy = t.IsBuy,
                TradeAmount = t.Amount.ToString(culture),
                TradePrice = Math.Abs(t.Price).ToString(culture),
            };
        }
        Rows.SubscribeReflectingCollectionChanged(Model.Trades, GetRow);

        Model.PropertyChanged += (_, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(Model.TradesAreLoading):
                    OnPropertyChanged(nameof(CanUpdateTrades));
                    OnPropertyChanged(nameof(UpdateTradesCommand));
                    break;
                case nameof(Model.CurrencyNames):
                    (CurrencyName1, CurrencyName2) = Model.CurrencyNames;
                    break;
            }
        };
    }
    
    public bool SomeCurrencyNameChanged => (CurrencyName1, CurrencyName2) != Model.CurrencyNames;
    
    public bool CanUpdateTrades => !Model.TradesAreLoading
        && CurrencyName1 != ""
        && CurrencyName2 != ""
        && CurrencyName1 != CurrencyName2
        && SomeCurrencyNameChanged;
    
    [RelayCommand(CanExecute = nameof(CanUpdateTrades))]
    public void UpdateTrades()
    {
        Model.CurrencyNames = (CurrencyName1, CurrencyName2);
        Messenger.Send<ReloadTradesMessage>();
    }
}