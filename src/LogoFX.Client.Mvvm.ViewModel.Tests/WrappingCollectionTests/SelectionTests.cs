using System;
using System.Collections.ObjectModel;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace LogoFX.Client.Mvvm.ViewModel.Tests.WrappingCollectionTests
{        
    public class SelectionTests : WrappingCollectionTestsBase
    {
        [Fact]
        public void Selection_ItemIsSelectedAndDeselected_SelectionIsEmpty()
        {
            var originalDataSource =
                new ObservableCollection<TestModel>(new[] { new TestModel(1), new TestModel(2), new TestModel(3) });

            var wrappingCollection = new WrappingCollection.WithSelection { FactoryMethod = o => new TestViewModel((TestModel)o) };
            wrappingCollection.AddSource(originalDataSource);
            var firstItem = wrappingCollection.OfType<TestViewModel>().First();
            wrappingCollection.Select(firstItem);
            wrappingCollection.Unselect(firstItem);

            AssertEmptySelection(wrappingCollection);            
        }

        [Fact]
        public void Selection_SelectionModeIsMultipleItemIsSelectedAndAnotherItemIsSelected_BothItemsAreSelected()
        {
            var originalDataSource =
                new ObservableCollection<TestModel>(new[] { new TestModel(1), new TestModel(2), new TestModel(3) });

            var wrappingCollection = new WrappingCollection.WithSelection(SelectionMode.ZeroOrMore) { FactoryMethod = o => new TestViewModel((TestModel)o) };
            wrappingCollection.AddSource(originalDataSource);
            var firstItem = wrappingCollection.OfType<TestViewModel>().First();
            var secondItem = wrappingCollection.OfType<TestViewModel>().Skip(1).First();
            wrappingCollection.Select(firstItem);
            wrappingCollection.Select(secondItem);

            wrappingCollection.SelectedItem.Should().Be(firstItem);
            var expectedSelection = new[] {firstItem, secondItem};
            wrappingCollection.SelectedItems.Should().BeEquivalentTo(expectedSelection);
            wrappingCollection.SelectionCount.Should().Be(2);            
        }

        //TODO: causing stack overflow - check after model package update      
        void Selection_SelectionModeIsSingleItemIsSelectedAndAnotherItemIsSelected_OnlySecondItemIsSelected()
        {
            var originalDataSource =
                new ObservableCollection<TestModel>(new[] { new TestModel(1), new TestModel(2), new TestModel(3) });

            var wrappingCollection = new WrappingCollection.WithSelection(SelectionMode.One) { FactoryMethod = o => new TestViewModel((TestModel)o) };
            wrappingCollection.AddSource(originalDataSource);
            var firstItem = wrappingCollection.OfType<TestViewModel>().First();
            var secondItem = wrappingCollection.OfType<TestViewModel>().Skip(1).First();
            wrappingCollection.Select(firstItem);
            wrappingCollection.Select(secondItem);

            wrappingCollection.SelectedItem.Should().Be(secondItem);
            var expectedSelection = new[] { firstItem, secondItem };
            wrappingCollection.SelectedItems.Should().BeEquivalentTo(expectedSelection);
            wrappingCollection.SelectionCount.Should().Be(1);
        }

        [Fact]
        public void Selection_ItemIsSelectedThenItemIsRemoved_SelectionIsEmpty()
        {
            var originalDataSource =
                new ObservableCollection<TestModel>(new[] { new TestModel(1), new TestModel(2), new TestModel(3) });

            var wrappingCollection = new WrappingCollection.WithSelection(SelectionMode.ZeroOrMore) { FactoryMethod = o => new TestViewModel((TestModel)o) };
            wrappingCollection.AddSource(originalDataSource);
            var firstItem = wrappingCollection.OfType<TestViewModel>().First();            
            wrappingCollection.Select(firstItem);
            originalDataSource.RemoveAt(0);

            AssertEmptySelection(wrappingCollection);
        }        

        [Fact]
        public void ClearSelection_CollectionContainsTwoSelectedItems_SelectionIsEmpty()
        {
            var originalDataSource =
                            new ObservableCollection<TestModel>(new[] { new TestModel(1), new TestModel(2), new TestModel(3) });

            var wrappingCollection = new WrappingCollection.WithSelection(SelectionMode.ZeroOrMore) { FactoryMethod = o => new TestViewModel((TestModel)o) };
            wrappingCollection.AddSource(originalDataSource);
            var firstItem = wrappingCollection.OfType<TestViewModel>().First();
            wrappingCollection.Select(firstItem);
            var secondItem = wrappingCollection.OfType<TestViewModel>().Skip(1).First();
            wrappingCollection.Select(secondItem);
            wrappingCollection.ClearSelection();

            AssertEmptySelection(wrappingCollection);
        }

        [Fact]
        public void CollectionIsCreated_SelectionPredicateIsSetAndOriginalSourceContainsItemsThatMatchThePredicate_ItemsAreSelected()
        {
            var originalDataSource =
                new ObservableCollection<TestModel>(new[] { new TestModel(1), new TestModel(2), new TestModel(3)});

            var wrappingCollection = new WrappingCollection.WithSelection(wr => ((TestViewModel) wr).Model.Id >= 2) { FactoryMethod = o => new TestViewModel((TestModel)o) };
            wrappingCollection.AddSource(originalDataSource);
            var secondItem = wrappingCollection.OfType<TestViewModel>().ElementAt(1);
            var lastItem = wrappingCollection.OfType<TestViewModel>().Last();
            wrappingCollection.SelectedItem.Should().Be(secondItem);
            var expectedSelection = new[] { lastItem, secondItem };
            wrappingCollection.SelectedItems.Should().BeEquivalentTo(expectedSelection);
            wrappingCollection.SelectionCount.Should().Be(2);
        }

        [Fact]
        public void Select_SelectionPredicateIsSet_ExceptionIsThrown()
        {
            var originalDataSource =
                new ObservableCollection<TestModel>(new[] { new TestModel(1), new TestModel(2), new TestModel(3) });

            var wrappingCollection = new WrappingCollection.WithSelection(wr => ((TestViewModel)wr).Model.Id >= 2) { FactoryMethod = o => new TestViewModel((TestModel)o) };
            wrappingCollection.AddSource(originalDataSource);
            var exception = Record.Exception(() => wrappingCollection.Select(wrappingCollection.SelectedItem));

            exception.Should().BeOfType<InvalidOperationException>().Which.Message.Should()
                .Be("Explicit selection status change cannot be used together with selection predicate");
        }

        [Fact]
        public void Unselect_SelectionPredicateIsSet_ExceptionIsThrown()
        {
            var originalDataSource =
                new ObservableCollection<TestModel>(new[] { new TestModel(1), new TestModel(2), new TestModel(3) });

            var wrappingCollection = new WrappingCollection.WithSelection(wr => ((TestViewModel)wr).Model.Id >= 2) { FactoryMethod = o => new TestViewModel((TestModel)o) };
            wrappingCollection.AddSource(originalDataSource);
            var exception = Record.Exception(() => wrappingCollection.Unselect(wrappingCollection.SelectedItem));

            exception.Should().BeOfType<InvalidOperationException>().Which.Message.Should()
                .Be("Explicit selection status change cannot be used together with selection predicate");
        }

        [Fact]
        public void ModelPropertyIsChanged_SelectionPredicateIsSetAndItemDoesntMatchPredicateAfterModelChange_ItemIsUnselected()
        {
            var originalDataSource =
                new ObservableCollection<TestModel>(new[]
                {
                    new TestModel(1) {Name = "First"},
                    new TestModel(2) {Name = "Second"},
                    new TestModel(3) {Name = "Third"}
                });

            var wrappingCollection = new WrappingCollection.WithSelection(wr => ((TestViewModel)wr).Model.Name.Length <= 5) { FactoryMethod = o => new TestViewModel((TestModel)o) };
            wrappingCollection.AddSource(originalDataSource);
            var firstItem = wrappingCollection.OfType<TestViewModel>().ElementAt(0);
            firstItem.Model.Name = "FirstOne";

            var lastItem = wrappingCollection.OfType<TestViewModel>().Last();
            wrappingCollection.SelectedItem.Should().Be(lastItem);
            var expectedSelection = new[] { lastItem };
            wrappingCollection.SelectedItems.Should().BeEquivalentTo(expectedSelection);
            wrappingCollection.SelectionCount.Should().Be(1);
        }

        private static void AssertEmptySelection(WrappingCollection.WithSelection wrappingCollection)
        {
            wrappingCollection.SelectedItem.Should().BeNull();
            wrappingCollection.SelectedItems.Should().BeEmpty();
            wrappingCollection.SelectionCount.Should().Be(0);            
        }
    }
}
