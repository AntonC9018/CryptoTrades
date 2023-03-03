using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
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
    private bool _shouldRefresh;
    
    private ListView RowListView => RootVisualElement.Q<ListView>("RowsListView");

    public void Receive(OpenTradesTableMessage message)
    {
        var rowsListView = RowListView;
        if (rowsListView.itemsSource is null)
        {
            // The MVVM library doesn't support data binding well for this use case.
            // We only need data binding on initialization for these.
            rowsListView.makeItem = () => _tableRowAsset.Instantiate();
            rowsListView.bindItem = (e, i) => ViewModel.Rows[i].BindView(e);
            rowsListView.selectionType = SelectionType.None;
            rowsListView.itemsSource = new CircularBufferIListWrapper<TradesTableRowViewModel>(ViewModel.Rows);

            ViewModel.Rows.CircularBufferChanged += (_, e) =>
            {
                // Refreshing the list view while not on the main thread makes it glitch out.
                // In general, I think calling unity functions from a non-main thread is considered UB.
                // Since I'm always calling RefreshItems on any change, there's no need to queue the events.
                // A bool suffices here.
                // We don't really have to lock it.
                // The worst that can happen is that the view would get refreshed two frames in a row.
                // This can happen if this handler gets called, but not executed fully ->
                // the update happens, refreshing the items and setting it back to false ->
                // it get sets back to true, indicating that in the next frame it should refresh again.
                // That's ultimately innocent.
                _shouldRefresh = true;
            };
        }

        enabled = true;
    }

    void Update()
    {
        if (_shouldRefresh)
        {
            _shouldRefresh = false;
            RowListView.RefreshItems();
        }
    }
}