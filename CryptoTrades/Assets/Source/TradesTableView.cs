using CommunityToolkit.Mvvm.Messaging;
using MVVMToolkit;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class TradesTableView : BaseView, IRecipient<OpenTradesTableMessage>
{
    private TradesTableViewModel ViewModel => (TradesTableViewModel) BindingContext;
    
    [SerializeField] private VisualTreeAsset _tableRowAsset;
    
#if UNITY_EDITOR
    [MenuItem("CONTEXT/TradesTableView/AddRow")]
    public static void AddRow(MenuCommand menuCommand)
    {
        if (!Application.isPlaying)
            return;
        
        var t = (TradesTableView) menuCommand.context;
        t.ViewModel.Rows.Add(new TradesTableRowViewModel
        {
            DateTime = "DateTime",
            TradeAmount = "TradeAmount",
            TradePrice = "TradePrice",
            IsBuy = true,
        });
    }
#endif

    protected override VisualElement Instantiate()
    {
        var root = base.Instantiate();
        
        // The MVVM library doesn't support data binding well for this use case.
        // We only need data binding on initialization for these.
        var rowsListView = root.Q<ListView>("RowsListView");
        rowsListView.itemsSource = ViewModel.Rows;
        rowsListView.makeItem = () => _tableRowAsset.Instantiate();
        rowsListView.bindItem = (e, i) =>
        {
            var row = ViewModel.Rows[i];
            var tradePrice = e.Q<Label>("TradePrice"); 
            tradePrice.text = row.TradePrice;
            tradePrice.AddToClassList(row.ColorClass);
            e.Q<Label>("TradeAmount").text = row.TradeAmount;
            e.Q<Label>("DateTime").text = row.DateTime;
        };
        rowsListView.selectionType = SelectionType.None;
        
        return root;
    }

    public void Receive(OpenTradesTableMessage message)
    {
        enabled = true;
    }
}