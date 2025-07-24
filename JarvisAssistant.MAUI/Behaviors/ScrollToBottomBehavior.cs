using System.Collections;
using System.Collections.Specialized;

namespace JarvisAssistant.MAUI.Behaviors
{
    public class ScrollToBottomBehavior : Behavior<CollectionView>
    {
        private CollectionView? _attachedCollectionView;

        protected override void OnAttachedTo(CollectionView bindable)
        {
            _attachedCollectionView = bindable;
            bindable.Loaded += OnCollectionViewLoaded;
            base.OnAttachedTo(bindable);
        }

        protected override void OnDetachingFrom(CollectionView bindable)
        {
            bindable.Loaded -= OnCollectionViewLoaded;
            if (bindable.ItemsSource is INotifyCollectionChanged collection)
            {
                collection.CollectionChanged -= OnCollectionChanged;
            }
            _attachedCollectionView = null;
            base.OnDetachingFrom(bindable);
        }

        private void OnCollectionViewLoaded(object? sender, EventArgs e)
        {
            if (sender is CollectionView collectionView)
            {
                if (collectionView.ItemsSource is INotifyCollectionChanged collection)
                {
                    collection.CollectionChanged += OnCollectionChanged;
                }
            }
        }

        private async void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && sender is IEnumerable items)
            {
                await ScrollToBottom(items);
            }
        }

        private async Task ScrollToBottom(IEnumerable items)
        {
            try
            {
                // Use the stored reference to the CollectionView
                var collectionView = _attachedCollectionView;
                if (collectionView == null) return;

                // Get the last item
                var itemsList = items.Cast<object>().ToList();
                if (!itemsList.Any()) return;

                var lastItem = itemsList.Last();

                // Wait a bit for the UI to update
                await Task.Delay(50);

                // Scroll to the last item with animation
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    try
                    {
                        collectionView.ScrollTo(lastItem, position: ScrollToPosition.End, animate: true);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"ScrollToBottom error: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ScrollToBottom error: {ex.Message}");
            }
        }
    }
}
