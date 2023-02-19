using CommunityToolkit.Mvvm.ComponentModel;
using MVVMToolkit;

public partial class TestViewModel : ViewModel
{
    // To bind a simple property, just create a backing field
    // and attach [ObservableProperty] attribute. TestInt property will be generated.
    [ObservableProperty] private int _testInt = 12;

    partial void OnTestIntChanging(int oldValue, int value)
    {
    }
}