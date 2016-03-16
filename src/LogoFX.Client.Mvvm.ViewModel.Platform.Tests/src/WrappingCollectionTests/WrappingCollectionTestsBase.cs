using System.Windows.Threading;
using LogoFX.Client.Testing.Infra;
using NUnit.Framework;

namespace LogoFX.Client.Mvvm.ViewModel.Tests.WrappingCollectionTests
{
    abstract class WrappingCollectionTestsBase
    {
        [SetUp]
        protected void Setup()
        {
            Dispatch.Current = new SameThreadDispatch();
        }                
    }
}