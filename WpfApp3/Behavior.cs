using Microsoft.Xaml.Behaviors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace WpfApp3
{
    public class TabControlBehavior : Behavior<TabControl>
    {
        private static readonly Dictionary<TabControl, TabItemsSourceHandler> ItemSourceHandlers = new Dictionary<TabControl, TabItemsSourceHandler>();
        // Using a DependencyProperty as the backing store for ItemSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(TabControlBehavior), new PropertyMetadata(null, OnItemsSourcePropertyChanged));
        private static readonly Dictionary<TabControl, TabControlSelectedItemHandler> SelectedItemHandlers = new Dictionary<TabControl, TabControlSelectedItemHandler>();

        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }
        public object SelectedItem
        {
            get { return (object)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedItem.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(
                nameof(SelectedItem), 
                typeof(object), 
                typeof(TabControlBehavior), 
                new PropertyMetadata(null, OnSelectedItemPropertyChanged));

        private static void OnSelectedItemPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tabControl = d as TabControl;

            if (tabControl == null)
                return;

            if (tabControl.ItemsSource != null)
                return;

            TabControlSelectedItemHandler handler;

            if (!SelectedItemHandlers.ContainsKey(tabControl))
            {
                handler = new TabControlSelectedItemHandler(tabControl);
                SelectedItemHandlers.Add(tabControl, handler);
                tabControl.Unloaded += SelectedItemTabControlUnloaded;
            }
            else
            {
                handler = SelectedItemHandlers[tabControl];
            }

            handler.ChangeSelectionFromProperty();
        }

        private static void SelectedItemTabControlUnloaded(object sender, RoutedEventArgs e)
        {
            var tabControl = sender as TabControl;

            if (tabControl == null)
                return;

            RemoveFromSelectedItemHandlers(tabControl);

            tabControl.Unloaded -= SelectedItemTabControlUnloaded;
        }

        private static void RemoveFromSelectedItemHandlers(TabControl tabControl)
        {
            if (!SelectedItemHandlers.ContainsKey(tabControl))
                return;

            SelectedItemHandlers[tabControl].Dispose();
            SelectedItemHandlers.Remove(tabControl);
        }

        private static void OnItemsSourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TabControl tabControl)
            {
                if (tabControl.ItemsSource != null)
                {
                    return;
                }

                TabItemsSourceHandler handler;

                if (!ItemSourceHandlers.ContainsKey(tabControl))
                {
                    handler = new TabItemsSourceHandler(tabControl);
                    ItemSourceHandlers.Add(tabControl, handler);
                    tabControl.Unloaded += ItemsSourceTabControlUnloaded;
                }
                else
                {
                    handler = ItemSourceHandlers[tabControl];
                }

                handler.Load();
            }
        }

        private static void ItemsSourceTabControlUnloaded(object sender, RoutedEventArgs e)
        {
            var tabControl = sender as TabControl;

            if (tabControl == null)
                return;

            RemoveFromItemSourceHandlers(tabControl);
            tabControl.Unloaded -= ItemsSourceTabControlUnloaded;
        }

        private static void RemoveFromItemSourceHandlers(TabControl tabControl)
        {
            if (!ItemSourceHandlers.ContainsKey(tabControl))
                return;

            ItemSourceHandlers[tabControl].Dispose();
            ItemSourceHandlers.Remove(tabControl);
        }
    }

    public class TabItemsSourceHandler
    {
        public TabControl Tab { get; private set; }

        public TabItemsSourceHandler(TabControl tab)
        {
            Tab = tab;
            Tab.Loaded += TabLoaded;
        }

        private void TabLoaded(object sender, RoutedEventArgs e)
        {
            AttachCollectionChangedEvent();

            LoadItemsSource();
        }

        private void AttachCollectionChangedEvent()
        {
            var source = Tab.GetValue(TabControlBehavior.ItemsSourceProperty) as INotifyCollectionChanged;

            // This property is not necessary to implement INotifyCollectionChanged.
            // Everything else will still work.  We just can't add or remove tab.
            if (source == null)
                return;

            source.CollectionChanged += SourceCollectionChanged;
        }

        private void SourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var view in e.NewItems)
                        AddTabItem(view);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var view in e.OldItems)
                        RemoveTabItem(view);
                    break;
            }
        }

        private void AddTabItem(object view)
        {
            var contentControl = new ContentControl();
            contentControl.SetBinding(ContentControl.ContentProperty, new Binding());
            var item = new TabItem { DataContext = view, Content = contentControl };

            Tab.Items.Add(item);

            // When there is only 1 Item, the tab can't be rendered without have it selected
            // Don't do Refresh().  This may clear the Selected item, causing issue in the ViewModel
            if (Tab.SelectedItem == null)
                item.IsSelected = true;
        }

        private void RemoveTabItem(object view)
        {
            var foundItem = Tab.Items.Cast<TabItem>().FirstOrDefault(t => t.DataContext == view);

            if (foundItem != null)
                Tab.Items.Remove(foundItem);
        }

        private void LoadItemsSource()
        {
            var sourceItems = Tab.GetValue(TabControlBehavior.ItemsSourceProperty) as IEnumerable;

            if (sourceItems == null)
                return;

            Load(sourceItems);
        }

        public void Load()
        {
            var source = Tab.GetValue(TabControlBehavior.ItemsSourceProperty) as IEnumerable;

            if (source == null)
                return;

            Load(source);
        }

        private void Load(IEnumerable sourceItems)
        {
            Tab.Items.Clear();

            foreach (var page in sourceItems)
                AddTabItem(page);

            // If there is selected item, select it after setting the initial tabitem collection
            SelectItem();
        }

        private void SelectItem()
        {
            var selectedObject = Tab.GetValue(TabControlBehavior.SelectedItemProperty);

            if (selectedObject == null)
                return;

            foreach (TabItem item in Tab.Items)
            {
                if (item.DataContext != selectedObject)
                    continue;

                item.IsSelected = true;
                return;
            }
        }

        public void Dispose()
        {
            var source = Tab.GetValue(TabControlBehavior.ItemsSourceProperty) as INotifyCollectionChanged;

            if (source != null)
                source.CollectionChanged -= SourceCollectionChanged;

            Tab = null;
        }
    }

    public class TabControlSelectedItemHandler 
    {
        public TabControl Tab { get; private set; }

        public TabControlSelectedItemHandler(TabControl tab)
        {
            Tab = tab;
            Tab.SelectionChanged += ChangeSelectionFromUi;
        }

        public void ChangeSelectionFromProperty()
        {
            var selectedObject = Tab.GetValue(TabControlBehavior.SelectedItemProperty);

            if (selectedObject == null)
            {
                Tab.SelectedItem = null;
                return;
            }

            foreach (TabItem tabItem in Tab.Items)
            {
                if (tabItem.DataContext == selectedObject)
                {
                    if (!tabItem.IsSelected)
                        tabItem.IsSelected = true;

                    break;
                }
            }
        }

        private void ChangeSelectionFromUi(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count >= 1)
            {
                var selectedObject = e.AddedItems[0];

                var selectedItem = selectedObject as TabItem;

                if (selectedItem != null)
                    SelectedItemProperty(selectedItem);
            }
        }

        private void SelectedItemProperty(TabItem selectedTabItem)
        {
            var tabObjects = Tab.GetValue(TabControlBehavior.ItemsSourceProperty) as IEnumerable;

            if (tabObjects == null)
                return;

            foreach (var tabObject in tabObjects)
            {
                if (tabObject == selectedTabItem.DataContext)
                {
                    //TabControlBehavior.SelectedItem = tabObject;
                    return;
                }
            }
        }

        public void Dispose()
        {
            Tab.SelectionChanged -= ChangeSelectionFromUi;
            Tab = null;
        }
    }
}
