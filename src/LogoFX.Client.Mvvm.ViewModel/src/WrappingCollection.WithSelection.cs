using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Threading;
using LogoFX.Client.Core;
using LogoFX.Client.Mvvm.ViewModel.Contracts;
using LogoFX.Core;

namespace LogoFX.Client.Mvvm.ViewModel
{
    public partial class WrappingCollection
    {
        /// <summary>
        /// Represents collection of view models which enables synchronization with its data source(s) and supports selection.
        /// </summary>
        public class WithSelection : WrappingCollection, ISelector, INotifyPropertyChanged
        {
            private readonly ObservableCollection<object> _selectedItems = new ObservableCollection<object>();
            private const uint SingleSelectionMask = (uint)(SelectionMode.One | SelectionMode.ZeroOrOne);
            private const uint MultipleSelectionMask = (uint)(SelectionMode.OneOrMore | SelectionMode.ZeroOrMore);
            private const uint RequiredSelectionMask = (uint)(SelectionMode.OneOrMore | SelectionMode.One);
            private const SelectionMode DefaultSelectionMode = SelectionMode.ZeroOrMore;
            private readonly ReentranceGuard _selectionManagement = new ReentranceGuard();
            private readonly SelectionMode _selectionMode;            
            private EventHandler<SelectionChangingEventArgs> _currentHandler;
            private Action<object, SelectionChangingEventArgs> _selectionHandler;
            private PropertyChangedEventHandler _internalSelectionHandler;

            /// <summary>
            /// Initializes a new instance of the <see cref="WrappingCollection.WithSelection"/> class.
            /// </summary>
            public WithSelection()
                :this(selectionMode: DefaultSelectionMode, isBulk: false)
            {
                
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="WrappingCollection.WithSelection"/> class.
            /// </summary>
            /// <param name="selectionMode">The selection mode. Cannot be used together with selection predicate.</param>
            /// <param name="isBulk">if set to <c>true</c> uses bulk operations mode.</param>
            public WithSelection(SelectionMode selectionMode = DefaultSelectionMode, bool isBulk = false)
                : base(isBulk)
            {
                _selectionMode = selectionMode;
                _selectedItems.CollectionChanged += (a, b) => OnSelectionChanged();
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="WrappingCollection.WithSelection"/> class.
            /// </summary>
            /// <param name="selectionPredicate">The selection predicate. Cannot be used together with selection mode.</param>
            /// <param name="isBulk">if set to <c>true</c> uses bulk operations mode.</param>
            public WithSelection(Predicate<object> selectionPredicate = null, bool isBulk = false)
                : base(isBulk)
            {
                _selectionPredicate = selectionPredicate;
                _selectedItems.CollectionChanged += (a, b) => OnSelectionChanged();
            }

            #region Private implementation
            private bool HandleItemSelectionChanged(object obj, bool isSelecting)
            {
                using (_selectionManagement.Raise())
                {
                    if (_selectionManagement.IsLocked)
                        return false;

                    if (_selectedItems.Contains(obj) && isSelecting)
                    {
                        //redundant call, skip
                        return true;
                    }

                    SelectionChangingEventArgs args = new SelectionChangingEventArgs(obj, isSelecting);
                    InvokeSelectionChanging(args);
                    if (args.Cancel)
                    {
                        //cancel selection change
                        if (obj is ISelectable selectable)
                            selectable.IsSelected = !isSelecting;
                        return false;
                    }

                    if (isSelecting)
                    {
                        if (_selectionPredicate == null && ((uint)_selectionMode & SingleSelectionMask) != 0)
                        {
                            _selectedItems.ForEach(a =>
                            {
                                if (a is ISelectable)
                                    ((ISelectable)obj).IsSelected = false;
                            });
                            _selectedItems.Clear();
                        }
                        _selectedItems.Add(obj);
                        if (obj is ISelectable selectable)
                            selectable.IsSelected = true;
                    }
                    else
                    {
                        _selectedItems.Remove(obj);
                        if (obj is ISelectable selectable)
                            selectable.IsSelected = false;

                        if (_selectionPredicate == null && IsSelectionRequired && _selectedItems.Count == 0 && _collectionManager.ItemsCount > 0)
                        {
                            _selectedItems.Add(_collectionManager.First());
                            if (obj is ISelectable selectable1)
                                selectable1.IsSelected = true;
                        }
                    }

                    InvokeSelectionChanged(new EventArgs());
                    OnPropertyChanged("SelectedItem");
                    OnPropertyChanged("SelectedItems");
                    OnPropertyChanged("SelectionCount");
                    return true;
                }
            }

            private void InternalIsSelectedChanged(object o, PropertyChangedEventArgs args)
            {
                if (o != null && !_collectionManager.Contains(o))
                    ((INotifyPropertyChanged)o).PropertyChanged -= _internalSelectionHandler;
                else if (args.PropertyName == "IsSelected" && o is ISelectable)
                    Dispatch.Current.OnUiThread(() => HandleItemSelectionChanged(o, ((ISelectable)o).IsSelected));
            }

            private bool IsSelectionRequired => ((uint)_selectionMode & RequiredSelectionMask) != 0;

            #endregion

            #region overrides

            /// <summary>
            /// Override this method to inject custom logic on collection change.
            /// </summary>
            /// <param name="e"></param>
            protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
            {
                if (_internalSelectionHandler == null)
                    _internalSelectionHandler = WeakDelegate.From(InternalIsSelectedChanged);

                void RemoveHandler(object a)
                {
                    if (a is INotifyPropertyChanged changed)
                        changed.PropertyChanged -= _internalSelectionHandler;
                    UnselectImpl(a);
                }

                void AddHandler(object a)
                {                    
                    if (a is INotifyPropertyChanged changed)
                        changed.PropertyChanged += _internalSelectionHandler;

                    if (_selectionPredicate != null)
                    {
                        if (a is IModelWrapper modelWrapper &&
                            modelWrapper.Model is INotifyPropertyChanged modelChanged)
                        {
                            PropertyChangedEventHandler strongHandler = ModelChangedOnPropertyChanged;
                            modelChanged.PropertyChanged += WeakDelegate.From(strongHandler);
                        }
                        if (_selectionPredicate(a))
                        {
                            SelectImpl(a);
                        }
                    }
                    else if (_selectedItems.Count == 0 && IsSelectionRequired)
                    {
                        SelectImpl(a);
                    }

                    void ModelChangedOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
                    {
                        if (_selectionPredicate(a))
                        {
                            SelectImpl(a);
                        }
                        else
                        {
                            UnselectImpl(a);
                        }
                    }
                }

                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        e.NewItems.Cast<object>().ForEach(AddHandler);
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        e.OldItems.Cast<object>().ForEach(RemoveHandler);
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        e.OldItems.Cast<object>().ForEach(RemoveHandler);
                        e.NewItems.Cast<object>().ForEach(AddHandler);
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        Debug.Assert(false, "We should never be here. Check base class impl");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();

                }
                base.OnCollectionChanged(e);
            }            

            #endregion

            #region Select

            /// <summary>
            /// Selection operation
            /// </summary>
            /// <param name="itemsToSelect">item to select </param>
            /// <param name="notify"></param>
            /// <returns>old selected item if available</returns>
            public bool Select(object itemsToSelect, bool notify = true)
            {
                if (_selectionPredicate != null)
                {
                    throw new InvalidOperationException("Explicit selection status change cannot be used together with selection predicate");
                }
                return SelectImpl(itemsToSelect);
            }

            private bool SelectImpl(object itemsToSelect)
            {
                object item = _collectionManager.Find(itemsToSelect);
                if (item != null)
                {
                    return HandleItemSelectionChanged(item, true);
                }
                return false;
            }

            #endregion

            #region Unselect

            /// <summary>
            /// Un-selects object
            /// </summary>
            /// <param name="item"></param>
            /// <param name="notify"></param>
            /// <returns><see langword="true"/> if succeeded, otherwise <see langword="false"/></returns>
            public bool Unselect(object item, bool notify = true)
            {
                if (_selectionPredicate != null)
                {
                    throw new InvalidOperationException("Explicit selection status change cannot be used together with selection predicate");
                }
                return UnselectImpl(item);
            }

            private bool UnselectImpl(object item)
            {
                if (item != null)
                    return HandleItemSelectionChanged(item, false);
                return false;
            }

            /// <summary>
            /// Clears the selection.
            /// </summary>
            public void ClearSelection()
            {
                if (_selectionPredicate != null)
                {
                    throw new InvalidOperationException("Explicit selection status change cannot be used together with selection predicate");
                }
                ClearSelectionImpl();
            }

            private void ClearSelectionImpl()
            {
                //TODO: refactor into more efficient approach
                foreach (var selectedItem in SelectedItems.OfType<object>().ToArray())
                {
                    UnselectImpl(selectedItem);
                }
            }

            #endregion

            /// <summary>
            /// Override this method to inject custom logic after the selection is changed.
            /// </summary>
            protected virtual void OnSelectionChanged()
            {
            }

            /// <summary>
            /// Gets or sets the selection handler.
            /// </summary>
            /// <value>
            /// The selection handler.
            /// </value>
            public Action<object, SelectionChangingEventArgs> SelectionHandler
            {
                get => _selectionHandler;
                set
                {
                    if (_currentHandler != null)
                        SelectionChanging -= _currentHandler;
                    _selectionHandler = value;
                    SelectionChanging += (_currentHandler = WeakDelegate.From<SelectionChangingEventArgs>((a, b) => _selectionHandler(this, b)));
                }
            }

            /// <summary>
            /// Selected item
            /// </summary>
            public object SelectedItem => _selectedItems.Count > 0 ? _selectedItems[0] : null;

            /// <summary>
            /// Gets the selection count.
            /// </summary>
            /// <value>
            /// The selection count.
            /// </value>
            public int SelectionCount => _selectedItems?.Count ?? 0;

            /// <summary>
            /// Selected items
            /// </summary>
            public IEnumerable SelectedItems => _selectedItems;

            private Predicate<object> _selectionPredicate;
            public Predicate<object> SelectionPredicate
            {
                get => _selectionPredicate;
                set
                {
                    _selectionPredicate = value;
                    ClearSelectionImpl();
                    if (_selectionPredicate != null)
                    {                        
                        foreach (var item in _collectionManager)
                        {                            
                            if (_selectionPredicate(item))
                            {
                                SelectImpl(item);
                            }
                        }                        
                    }                    
                }
            }

            /// <summary>
            /// Occurs when selection is changed.
            /// </summary>
            public event EventHandler SelectionChanged;

            /// <summary>
            /// Invokes the selection changed event.
            /// </summary>
            /// <param name="e"></param>
            protected void InvokeSelectionChanged(EventArgs e)
            {
                EventHandler handler = SelectionChanged;
                handler?.Invoke(this, e);
            }

            /// <summary>
            /// Occurs when selection is changing.
            /// </summary>
            public event EventHandler<SelectionChangingEventArgs> SelectionChanging;

            /// <summary>
            /// Invokes the selection changing event.
            /// </summary>
            /// <param name="e"></param>
            protected void InvokeSelectionChanging(SelectionChangingEventArgs e)
            {
                EventHandler<SelectionChangingEventArgs> handler = SelectionChanging;
                handler?.Invoke(this, e);
            }

            #region Implementation of INotifyPropertyChanged

            /// <summary>
            /// Occurs when a property value changes.
            /// </summary>
            public event PropertyChangedEventHandler PropertyChanged;

            /// <summary>
            /// Called when property is changed.
            /// </summary>
            /// <param name="p">The p.</param>
            protected void OnPropertyChanged(string p)
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                handler?.Invoke(this, new PropertyChangedEventArgs(p));
            }

            #endregion
        }
    }
}
