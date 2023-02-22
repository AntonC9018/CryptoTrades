using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using CommunityToolkit.Mvvm.Messaging;
using MVVMToolkit;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Utils;

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
        t.ViewModel.Rows.PushFront(new TradesTableRowViewModel
        {
            DateTime = "DateTime",
            TradeAmount = "TradeAmount",
            TradePrice = "TradePrice",
            IsBuy = true,
        });
    }
#endif

    public void Receive(OpenTradesTableMessage message)
    {
        var rowsListView = RootVisualElement.Q<ListView>("RowsListView");
        if (rowsListView.itemsSource is null)
        {
            // The MVVM library doesn't support data binding well for this use case.
            // We only need data binding on initialization for these.
            rowsListView.makeItem = () => _tableRowAsset.Instantiate();
            rowsListView.bindItem = (e, i) => TradesTableRowViewModel.Bind(e, ViewModel.Rows[i]);
            rowsListView.selectionType = SelectionType.None;
            rowsListView.itemsSource = new CircularBufferIListWrapper<TradesTableRowViewModel>(ViewModel.Rows);

            ViewModel.Rows.CircularBufferChanged += (_, e) =>
            {
                switch (e.Action)
                {
                    case CircularBufferAction.SetAtIndex:
                        rowsListView.RefreshItem(e.Index);
                        break;
                    default:
                        // I would say this is a hack.
                        // This does so much extra work, especially when the collection is being initialized.
                        rowsListView.RefreshItems();
                        break;
                }
            };
        }

        enabled = true;
    }
}