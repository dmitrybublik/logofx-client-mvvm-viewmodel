using System.Linq;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace LogoFX.Client.Mvvm.ViewModel.Tests.WrappingCollectionTests
{    
    public class InitializationTests : WrappingCollectionTestsBase
    {        
        [Fact]
        public void AddingDataSource_DataSourceContainsModelsAndFactoryMethodIsSpecified_CollectionContainsConcreteTypeViewModelsWithDataSourceModels()
        {
            var dataSource = new[] {new TestModel(1), new TestModel(2), new TestModel(3)};

            var wrappingCollection = new WrappingCollection {FactoryMethod = o => new TestViewModel((TestModel)o)};
            wrappingCollection.AddSource(dataSource);

            var viewModels = wrappingCollection.OfType<TestViewModel>().ToArray();
            var actualModels = viewModels.Select(t => t.Model).ToArray();
            actualModels.Should().BeEquivalentTo(dataSource);
        }

        [Fact]
        public void AddingDataSource_DataSourceContainsModelsAndFactoryMethodIsNotSpecified_CollectionContainsViewModelsWithDataSourceModels()
        {
            var dataSource = new[] { new TestModel(1), new TestModel(2), new TestModel(3) };

            var wrappingCollection = new WrappingCollection();
            wrappingCollection.AddSource(dataSource);

            var viewModels = wrappingCollection.OfType<object>().ToArray();
            var viewModelType = viewModels.First().GetType();
            var modelPropertyInfo = viewModelType.GetTypeInfo().GetDeclaredProperty("Model");
            var actualModels = viewModels.Select(modelPropertyInfo.GetValue).ToArray();
            actualModels.Should().BeEquivalentTo(dataSource);
        }

        [Fact]
        public void AddingDataSource_DataSourceContainsModelsAndSelectionModeIsOne_FirstViewModelIsSelected()
        {
            var dataSource = new[] { new TestModel(1), new TestModel(2), new TestModel(3) };

            var wrappingCollection = new WrappingCollection.WithSelection(SelectionMode.One) { FactoryMethod = o => new TestViewModel((TestModel)o) };
            wrappingCollection.AddSource(dataSource);

            var viewModels = wrappingCollection.OfType<TestViewModel>().ToArray();
            var firstViewModel = viewModels.First();
            var selectedViewModel = wrappingCollection.SelectedItem;
            selectedViewModel.Should().BeSameAs(firstViewModel);            
        }

        [Fact]
        public void AddingDataSource_DataSourceContainsModelsAndSelectionModeIsZeroOrMore_NoViewModelIsSelected()
        {
            var dataSource = new[] { new TestModel(1), new TestModel(2), new TestModel(3) };

            var wrappingCollection = new WrappingCollection.WithSelection(SelectionMode.ZeroOrMore) { FactoryMethod = o => new TestViewModel((TestModel)o) };
            wrappingCollection.AddSource(dataSource);
            
            var selectedViewModel = wrappingCollection.SelectedItem;
            selectedViewModel.Should().BeNull();            
        }
    }
}
