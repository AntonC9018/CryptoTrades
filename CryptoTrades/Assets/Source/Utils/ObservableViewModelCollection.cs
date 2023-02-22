// https://github.com/AntonC9018/uni_csharp/blob/6076582717404ecd01787e556d0f6906d1a927a1/sem2_lab1/ObservableViewModelCollection.cs

#pragma warning disable // nullability not enabled, but ? is used

namespace Utils
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Linq;
    
    public static class ObservableCollectionHelper
    {
        // The interface of NotifyCollectionChangedEventArgs isn't obvious at all,
        // this implementation is one that made sense.
        public static void ReflectCollectionChangedEvent<TModel, TViewModel>(
            this ObservableCollection<TViewModel> observer,
            Func<TModel?, TViewModel> viewModelFactory,
            NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                {
                    for (int i = 0; i < e.NewItems!.Count; i++)
                        observer.Insert(e.NewStartingIndex + i, viewModelFactory((TModel?) e.NewItems[i]));
                    break;
                }
    
                case NotifyCollectionChangedAction.Move:
                {
                    if (e.OldItems!.Count == 1)
                    {
                        observer.Move(e.OldStartingIndex, e.NewStartingIndex);
                    }
                    else
                    {
                        // This part is kinda terribly implemented though.
                        // I might reimplement this part.
                        List<TViewModel> items = observer.Skip(e.OldStartingIndex).Take(e.OldItems.Count).ToList();
                        for (int i = 0; i < e.OldItems.Count; i++)
                            observer.RemoveAt(e.OldStartingIndex);
    
                        for (int i = 0; i < items.Count; i++)
                            observer.Insert(e.NewStartingIndex + i, items[i]);
                    }
                    break;
                }
    
                case NotifyCollectionChangedAction.Remove:
                {
                    for (int i = 0; i < e.OldItems!.Count; i++)
                        observer.RemoveAt(e.OldStartingIndex);
                    break;
                }
    
                case NotifyCollectionChangedAction.Replace:
                {
                    // These things also could be done better, if it's possible to reuse the view models.
                    
                    // remove
                    for (int i = 0; i < e.OldItems!.Count; i++)
                        observer.RemoveAt(e.OldStartingIndex);
    
                    // add
                    goto case NotifyCollectionChangedAction.Add;
                }
    
                case NotifyCollectionChangedAction.Reset:
                {
                    observer.Clear();
                    if (e.NewItems is not null)
                    {
                        for (int i = 0; i < e.NewItems.Count; i++)
                            observer.Add(viewModelFactory((TModel?) e.NewItems[i]));
                    }
                    break;
                }
    
                default:
                    break;
            }
        }

        public static NotifyCollectionChangedEventHandler WrapCollectionChanged<TModel, TViewModel>(
            this ObservableCollection<TViewModel> observer,
            Func<TModel?, TViewModel> viewModelFactory)
        {
            return (_, e) => ReflectCollectionChangedEvent(observer, viewModelFactory, e);
        }
        
        public static void SubscribeReflectingCollectionChanged<TModel, TViewModel>(
            this ObservableCollection<TViewModel> observer,
            ObservableCollection<TModel?> observable,
            Func<TModel?, TViewModel> viewModelFactory)
        {
            observable.CollectionChanged += observer.WrapCollectionChanged(viewModelFactory);
        }
    }
    
    // https://stackoverflow.com/a/2177659/9731532
    /* 
        Click button to create
        -> add a new model to models
        -> trigger add event on the model observable
        -> add a new view model to view models
        -> trigger add view model event
        -> update UI
    
        UI value changed
        -> set property on view model via binding
        -> set field on model
        -> trigger update event
        -> update UI (validation for example)
    */
    public class ObservableViewModelCollection<TViewModel, TModel> : ObservableCollection<TViewModel>
    {
        public ObservableViewModelCollection(ObservableCollection<TModel?> observable, Func<TModel?, TViewModel> viewModelFactory)
            : base(observable.Select(viewModelFactory))
        {
            observable.CollectionChanged += this.WrapCollectionChanged(viewModelFactory);
        }
    }
}


#pragma warning restore CS8632 // nullability not enabled, but ? is used
