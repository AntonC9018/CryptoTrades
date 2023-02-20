using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using UnityEngine;

public sealed partial class TradesModel : ObservableObject
{
    [ObservableProperty] private bool _tradesAreLoading;
    [ObservableProperty] private (string, string) _currencyNames = ("", "");
    public ObservableCollection<Trade> Trades { get; } = new();
}