using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using CommunityToolkit.Mvvm.Messaging;
using Cysharp.Threading.Tasks;
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
            rowsListView.makeItem = () =>
            {
                 Debug.Log("Making item " + DateTime.Now.Second);
                 return _tableRowAsset.Instantiate();
            };
            rowsListView.bindItem = (e, i) => ViewModel.Rows[i].BindView(e);
            rowsListView.selectionType = SelectionType.None;
            rowsListView.itemsSource = new CircularBufferIListWrapper<TradesTableRowViewModel>(ViewModel.Rows);

            ViewModel.Rows.CircularBufferChanged += async (_, e) =>
            {
                // Refreshing the list view while not on the main thread makes it glitch out.
                // In general, I think calling unity functions from a non-main thread is considered UB.
                await UniTask.SwitchToMainThread();

                switch (e.Action)
                {
                    case CircularBufferAction.SetAtIndex:
                        rowsListView.RefreshItem(e.Index);
                        break;
                    default:
                        // Refresh all items.
                        rowsListView.RefreshItems();
                        break;
                }
            };
            
            // The table header has to account for the scrollbar width
            // in order to align the header columns properly.
            var scrollBarWidth = rowsListView.style.width;
            var offset = RootVisualElement.Q<VisualElement>("ScrollbarOffset");
            offset.style.width = scrollBarWidth;
        }

        enabled = true;
    }
}