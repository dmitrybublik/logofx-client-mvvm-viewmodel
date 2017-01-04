using System.Collections.Specialized;
using FluentAssertions;
using LogoFX.Core;
using Xunit;

namespace LogoFX.Client.Mvvm.ViewModel.Tests.WrappingCollectionTests
{    
    public class NotificationTests : WrappingCollectionTestsBase
    {
        [Fact]
        public void
            WhenCollectionIsCreatedWithRangeAndSourceIsUpdatedWithAddRange_ThenSingleNotificationIsRaisedWithAllWrappers
            ()
        {
            var source = new RangeObservableCollection<object>();
            var items = new[] { new object(), new object() };
            var numberOfTimes = 0;

            var collection = new WrappingCollection(isBulk: true)
            {
                FactoryMethod = o => o
            };
            collection.AddSource(source);
            collection.CollectionChanged += (sender, args) =>
            {
                args.NewItems.Should().BeEquivalentTo(items);                
                numberOfTimes++;
                numberOfTimes.Should().Be(1);                
            };
            source.AddRange(items);
        }

        [Fact]
        public void
            WhenCollectionIsCreatedWithRangeAndSingleItemAndSourceIsUpdatedWithRemoveRange_ThenSingleNotificationIsRaisedWithAllWrappers
            ()
        {
            var source = new RangeObservableCollection<object>();
            var items = new[] { new object() };
            var numberOfTimes = 0;

            var collection = new WrappingCollection(isBulk: true)
            {
                FactoryMethod = o => o
            };
            collection.AddSource(source);            
            source.AddRange(items);
            collection.CollectionChanged += (sender, args) =>
            {
                args.OldItems.Should().BeEquivalentTo(items);
                numberOfTimes++;
                numberOfTimes.Should().Be(1);
            };
            source.RemoveRange(items);
        }

        [Fact]
        public void
            WhenCollectionIsCreatedWithRangeAndMultipleItemsAndSourceIsUpdatedWithRemoveRange_ThenSingleNotificationIsRaisedWithAllWrappersAndActionIsReset
            ()
        {
            var source = new RangeObservableCollection<object>();
            var items = new[] { new object(), new object(), new object() };
            var numberOfTimes = 0;

            var collection = new WrappingCollection(isBulk: true)
            {
                FactoryMethod = o => o
            };
            collection.AddSource(source);
            source.AddRange(items);
            collection.CollectionChanged += (sender, args) =>
            {
                args.Action.Should().Be(NotifyCollectionChangedAction.Reset);
                numberOfTimes++;
                numberOfTimes.Should().Be(1);
            };
            source.RemoveRange(items);
        }

        [Fact]
        public void
            WhenCollectionIsCreatedWithRangeAndSingleItemAndSourceIsCleared_ThenSingleNotificationIsRaisedWithAllWrappers
            ()
        {
            var source = new RangeObservableCollection<object>();
            var items = new[] { new object() };
            var numberOfTimes = 0;

            var collection = new WrappingCollection(isBulk: true)
            {
                FactoryMethod = o => o
            };
            collection.AddSource(source);            
            source.AddRange(items);            
            collection.CollectionChanged += (sender, args) =>
            {
                args.OldItems.Should().BeEquivalentTo(items);
                numberOfTimes++;
                numberOfTimes.Should().Be(1);
            };
            source.Clear();
        }

        [Fact]
        public void
            WhenCollectionIsCreatedWithRangeAndMultipleItemsAndSourceIsCleared_ThenSingleNotificationIsRaisedWithAllWrappersAndActionisReset
            ()
        {
            var source = new RangeObservableCollection<object>();
            var items = new[] { new object(), new object(), new object() };
            var numberOfTimes = 0;

            var collection = new WrappingCollection(isBulk: true)
            {
                FactoryMethod = o => o
            };
            collection.AddSource(source);
            source.AddRange(items);
            collection.CollectionChanged += (sender, args) =>
            {
                args.Action.Should().Be(NotifyCollectionChangedAction.Reset);
                numberOfTimes++;
                numberOfTimes.Should().Be(1);
            };
            source.Clear();
        }
    }
}
