using System.Collections.ObjectModel;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace LogoFX.Client.Mvvm.ViewModel.Tests.WrappingCollectionTests
{    
    public class DataSourceModificationsTests : WrappingCollectionTestsBase
    {
        [Fact]
        public void ModelIsAddedThenModelIsRemovedThenModelIsAdded()
        {
            var firstModel = new TestModel(1);
            var dataSource =
                new ObservableCollection<TestModel>(new[] { firstModel });

            var wrappingCollection = new WrappingCollection { FactoryMethod = o => new TestViewModel((TestModel)o) };
            wrappingCollection.AddSource(dataSource);
            dataSource.Remove(firstModel);
            dataSource.Add(firstModel);

            var viewModels = wrappingCollection.OfType<TestViewModel>();
            var actualViewModel = viewModels.SingleOrDefault(t => t.Model.Id == 1);
            actualViewModel.Should().NotBeNull();            
        }
    }
}
