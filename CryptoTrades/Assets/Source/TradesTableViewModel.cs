using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MVVMToolkit;


public partial class TradesTableViewModel : ViewModel
{
    [ObservableProperty] private string _currencyName1;
    [ObservableProperty] private string _currencyName2;

    partial void OnCurrencyName1Changed(string value) => OnCurrencyNameChanged();
    partial void OnCurrencyName2Changed(string value) => OnCurrencyNameChanged();

    private void OnCurrencyNameChanged()
    {
        
    }

    // For now do this, later either change this to a circular buffer or idk.
    // I'm not sure how INotifyCollectionChanged is supposed to handle inserts at indices.
    // Does is support them only in the form of a Move and then a Set at an index?
    // If so, then it would add no value.
    public ObservableCollection<TradesTableRowViewModel> Rows { get; } = new();
}