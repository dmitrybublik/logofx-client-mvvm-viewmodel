using System;

namespace LogoFX.Client.Mvvm.ViewModel.Contracts
{
    /// <summary>
    /// Represents an object that has selection predicate.
    /// </summary>
    public interface IHaveSelectionPredicate
    {
        Predicate<object> SelectionPredicate { get; set; }
    }
}