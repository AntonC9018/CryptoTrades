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
    [SerializeField] private VisualTreeAsset _tableRowAsset;
    private TradesTableViewModel ViewModel => (TradesTableViewModel) BindingContext;

    public void Receive(OpenTradesTableMessage message)
    {
        var rowsListView = RootVisualElement.Q<ListView>("RowsListView");
        if (rowsListView.itemsSource is null)
        {
            // The MVVM library doesn't support data binding well for this use case.
            // We only need data binding on initialization for these.
            rowsListView.makeItem = () => _tableRowAsset.Instantiate();
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
        }

        enabled = true;
    }
}