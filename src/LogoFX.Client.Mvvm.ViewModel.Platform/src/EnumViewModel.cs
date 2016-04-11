using System;
using System.Collections.Generic;
using System.Linq;
#if NETFX_CORE
using System.Reflection;
#endif
using LogoFX.Client.Mvvm.ViewModel.Contracts;
using LogoFX.Core;

namespace LogoFX.Client.Mvvm.ViewModel
{
    /// <summary>
    /// Represents a view model which wraps around an enum value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EnumEntryViewModel<T>:ObjectViewModel<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnumEntryViewModel{T}"/> class.
        /// </summary>
        /// <param name="obj">The object.</param>
        public EnumEntryViewModel(T obj):base(obj)
        {
            
        }        
    }

    /// <summary>
    /// Represents a view model which wraps around a collection of enum values.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EnumViewModel<T> : IHierarchicalViewModel
    {
        private ObservableViewModelsCollection<IObjectViewModel> _children;

        /// <summary>
        /// Returns an enum model wrapper for specified enum value.
        /// </summary>
        /// <param name="item">The specified enum value.</param>
        public EnumEntryViewModel<T> this[T item]
        {
            get { return InternalChildren.Cast<EnumEntryViewModel<T>>().First(a => a.Model.Equals(item)); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumViewModel{T}"/> class.
        /// </summary>
        public EnumViewModel()
        {
            EnumHelper.GetValues<T>().ForEach(a => InternalChildren.Add(new EnumEntryViewModel<T>(a)));
        }

        #region Implementation of IHierarhicalViewModel

        /// <summary>
        /// Gets the children.
        /// </summary>
        public IViewModelsCollection<IObjectViewModel> Children
        {
            get { return InternalChildren; }
        }

        /// <summary>
        /// Gets the items.(GLUE:compatibility to caliburn micro)
        /// </summary>
        public IViewModelsCollection<IObjectViewModel> Items
        {
            get { return InternalChildren; }
        }

        private ObservableViewModelsCollection<IObjectViewModel> InternalChildren
        {
            get { return _children?? (_children = new ObservableViewModelsCollection<IObjectViewModel>()); }
        }

        
        #endregion
    }

    /// <summary>
    /// Helper class for enum operations.
    /// </summary>
    public static class EnumHelper
    {
        private static readonly IDictionary<Type, object[]> _enumCache = new Dictionary<Type, object[]>();

        /// <summary>
        /// Gets the boxed enum value.
        /// </summary>
        /// <param name="s">The unboxed enum value.</param>
        /// <returns></returns>
        public static object GetBoxed(Enum s)
        {
            Type enumType = s.GetType();
            object ret = GetValues(enumType).Where(ss => ss.ToString() == s.ToString()).FirstOrDefault();
            return ret;
        }

        /// <summary>
        /// Gets all enum values from the specified enum type.
        /// </summary>
        /// <typeparam name="T">The specified enum type.</typeparam>
        /// <returns></returns>
        public static T[] GetValues<T>()
        {
            return GetValues(typeof (T)).Cast<T>().ToArray();
        }

        /// <summary>
        /// Gets the unboxed enum values from the specified enum type.
        /// </summary>
        /// <param name="enumType">Type of the enum.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Type ' + enumType.Name + ' is not an enum</exception>
        public static object[] GetValues(Type enumType)
        {
#if WINDOWS_UWP || NETFX_CORE
            try
            {
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed -- System.Type does not have support for enum testing in UWP.
                Enum.GetNames(enumType);
            }
            catch (Exception)
            {                
                throw new ArgumentException("Type '" + enumType.Name + "' is not an enum");
            }
#else
            if (enumType.IsEnum == false)
            {
                throw new ArgumentException("Type '" + enumType.Name + "' is not an enum");
            }
#endif                        
            object[] values;
            if (!_enumCache.TryGetValue(enumType, out values))
            {
#if WINDOWS_UWP || NETFX_CORE
                values = (from field in enumType.GetTypeInfo().DeclaredFields
                          where field.IsLiteral
                          select field.GetValue(enumType)).ToArray();
#else
                values = (from field in enumType.GetFields()
                          where field.IsLiteral
                          select field.GetValue(enumType)).ToArray();
#endif
                _enumCache[enumType] = values;
            }
            return values;
        }
    }
}
