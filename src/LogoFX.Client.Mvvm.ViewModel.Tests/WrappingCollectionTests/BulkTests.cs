using System.Linq;
using FluentAssertions;
using LogoFX.Client.Mvvm.Model;
using LogoFX.Client.Mvvm.Model.Contracts;
using Xunit;

namespace LogoFX.Client.Mvvm.ViewModel.Tests.WrappingCollectionTests
{    
    public class BulkTests : WrappingCollectionTestsBase
    {
        [Fact]
        public void GivenCollectionIsBulkAndSourceWithTwoItemsIsAdded_WhenSecondItemIsRemoved_ThenCollectionContainsOneItem()
        {
            var source = new RangeModelsCollection<TestModel>();
            var modelOne = new TestModel(4);
            var modelTwo = new TestModel(5);            

            var wrappingCollection =
                new WrappingCollection.WithSelection(SelectionMode.ZeroOrOne, isBulk: true)
                {
                    FactoryMethod = o => o
                }.WithSource(((IReadModelsCollection<TestModel>) source).Items);
            source.AddRange(new[] { modelOne, modelTwo });
            source.Remove(modelTwo);

            var expectedModels = new[] {modelOne};
            wrappingCollection.OfType<TestModel>().ToArray().Should().BeEquivalentTo(expectedModels);            
        }

        [Fact]
        public void GivenCollectionIsBulkAndSourceWithTwoItemsIsAdded_WhenAllItemsAreRemoved_ThenCollectionContainsNoItem()
        {
            var source = new RangeModelsCollection<TestModel>();
            var modelOne = new TestModel(4);
            var modelTwo = new TestModel(5);            

            var wrappingCollection =
                new WrappingCollection.WithSelection(SelectionMode.ZeroOrOne, isBulk: true)
                {
                    FactoryMethod = o => o
                }.WithSource(((IReadModelsCollection<TestModel>) source).Items);
            source.AddRange(new[] { modelOne, modelTwo });
            source.RemoveRange(new[] { modelOne, modelTwo });

            wrappingCollection.OfType<TestModel>().ToArray().Should().BeEmpty();
        }
    }
}
