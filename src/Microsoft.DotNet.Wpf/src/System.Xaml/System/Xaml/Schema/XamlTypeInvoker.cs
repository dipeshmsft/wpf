// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows.Markup;

namespace System.Xaml.Schema
{
    public class XamlTypeInvoker
    {
        private static XamlTypeInvoker s_Unknown;

        private Dictionary<XamlType, MethodInfo> _addMethods;
        internal MethodInfo EnumeratorMethod { get; set; }
        private XamlType _xamlType;

        private Action<object> _constructorDelegate;

        private ThreeValuedBool _isPublic;

        protected XamlTypeInvoker()
        {
        }

        public XamlTypeInvoker(XamlType type)
        {
            _xamlType = type ?? throw new ArgumentNullException(nameof(type));
        }

        public static XamlTypeInvoker UnknownInvoker
        {
            get
            {
                if (s_Unknown is null)
                {
                    s_Unknown = new XamlTypeInvoker();
                }

                return s_Unknown;
            }
        }

        public EventHandler<XamlSetMarkupExtensionEventArgs> SetMarkupExtensionHandler
        {
            get { return _xamlType?.SetMarkupExtensionHandler; }
        }

        public EventHandler<XamlSetTypeConverterEventArgs> SetTypeConverterHandler
        {
            get { return _xamlType?.SetTypeConverterHandler; }
        }

        public virtual void AddToCollection(object instance, object item)
        {
            ArgumentNullException.ThrowIfNull(instance);
            if (instance is IList list)
            {
                list.Add(item);
                return;
            }

            ThrowIfUnknown();
            if (!_xamlType.IsCollection)
            {
                throw new NotSupportedException(SR.OnlySupportedOnCollections);
            }

            XamlType itemType;
            if (item is not null)
            {
                itemType = _xamlType.SchemaContext.GetXamlType(item.GetType());
            }
            else
            {
                itemType = _xamlType.ItemType;
            }

            MethodInfo addMethod = GetAddMethod(itemType);
            if (addMethod is null)
            {
                throw new XamlSchemaException(SR.Format(SR.NoAddMethodFound, _xamlType, itemType));
            }

            addMethod.Invoke(instance, new object[] { item });
        }

        public virtual void AddToDictionary(object instance, object key, object item)
        {
            ArgumentNullException.ThrowIfNull(instance);
            if (instance is IDictionary dictionary)
            {
                dictionary.Add(key, item);
                return;
            }

            ThrowIfUnknown();
            if (!_xamlType.IsDictionary)
            {
                throw new NotSupportedException(SR.OnlySupportedOnDictionaries);
            }

            XamlType itemType;
            if (item is not null)
            {
                itemType = _xamlType.SchemaContext.GetXamlType(item.GetType());
            }
            else
            {
                itemType = _xamlType.ItemType;
            }

            MethodInfo addMethod = GetAddMethod(itemType);
            if (addMethod is null)
            {
                throw new XamlSchemaException(SR.Format(SR.NoAddMethodFound, _xamlType, itemType));
            }

            addMethod.Invoke(instance, new object[] { key, item });
        }

        public virtual object CreateInstance(object[] arguments)
        {
            ThrowIfUnknown();
            if (!_xamlType.UnderlyingType.IsValueType && (arguments is null || arguments.Length == 0))
            {
                object result = DefaultCtorXamlActivator.CreateInstance(this);
                if (result is not null)
                {
                    return result;
                }
            }

            return Activator.CreateInstance(_xamlType.UnderlyingType, arguments);
        }

        public virtual MethodInfo GetAddMethod(XamlType contentType)
        {
            ArgumentNullException.ThrowIfNull(contentType);
            if (IsUnknown || _xamlType.ItemType is null)
            {
                return null;
            }

            // Common case is that we match the item type. Short-circuit any additional lookup.
            if (contentType == _xamlType.ItemType ||
                (_xamlType.AllowedContentTypes.Count == 1 && contentType.CanAssignTo(_xamlType.ItemType)))
            {
                return _xamlType.AddMethod;
            }

            // Only collections can have additional content types
            if (!_xamlType.IsCollection)
            {
                return null;
            }

            // Populate the dictionary of all available Add methods
            MethodInfo addMethod;
            if (_addMethods is null)
            {
                Dictionary<XamlType, MethodInfo> addMethods = new Dictionary<XamlType, MethodInfo>();
                addMethods.Add(_xamlType.ItemType, _xamlType.AddMethod);
                foreach (XamlType type in _xamlType.AllowedContentTypes)
                {
                    addMethod = CollectionReflector.GetAddMethod(
                        _xamlType.UnderlyingType, type.UnderlyingType);
                    if (addMethod is not null)
                    {
                        // Use TryAdd as AllowedContentTypes can contain
                        // duplicate types.
                        addMethods.TryAdd(type, addMethod);
                    }
                }

                _addMethods = addMethods;
            }

            // First try the fast path.  Look for an exact match.
            if (_addMethods.TryGetValue(contentType, out addMethod))
            {
                return addMethod;
            }

            // Next the slow path.  Check each one for is assignable from.
            foreach (KeyValuePair<XamlType, MethodInfo> pair in _addMethods)
            {
                if (contentType.CanAssignTo(pair.Key))
                {
                    return pair.Value;
                }
            }

            return null;
        }

        public virtual MethodInfo GetEnumeratorMethod()
        {
            if (IsUnknown)
            {
                return null;
            }

            return _xamlType.GetEnumeratorMethod;
        }

        public virtual IEnumerator GetItems(object instance)
        {
            ArgumentNullException.ThrowIfNull(instance);
            if (instance is IEnumerable enumerable)
            {
                return enumerable.GetEnumerator();
            }

            ThrowIfUnknown();
            if (!_xamlType.IsCollection && !_xamlType.IsDictionary)
            {
                throw new NotSupportedException(SR.OnlySupportedOnCollectionsAndDictionaries);
            }

            MethodInfo getEnumMethod = GetEnumeratorMethod();
            return (IEnumerator)getEnumMethod.Invoke(instance, Array.Empty<object>());
        }

        private bool IsPublic
        {
            get
            {
                if (_isPublic == ThreeValuedBool.NotSet)
                {
                    Type type = _xamlType.UnderlyingType.UnderlyingSystemType;
                    _isPublic = type.IsVisible ? ThreeValuedBool.True : ThreeValuedBool.False;
                }

                return _isPublic == ThreeValuedBool.True;
            }
        }

        private bool IsUnknown
        {
            get { return _xamlType is null || _xamlType.UnderlyingType is null; }
        }

        private void ThrowIfUnknown()
        {
            if (IsUnknown)
            {
                throw new NotSupportedException(SR.NotSupportedOnUnknownType);
            }
        }

        private static class DefaultCtorXamlActivator
        {
            private static ThreeValuedBool s_securityFailureWithCtorDelegate;
            private static ConstructorInfo s_actionCtor =
                typeof(Action<object>).GetConstructor(new Type[] { typeof(object), typeof(IntPtr) });

            public static object CreateInstance(XamlTypeInvoker type)
            {
                if (!EnsureConstructorDelegate(type))
                {
                    return null;
                }

                object inst = CallCtorDelegate(type);
                return inst;
            }
#pragma warning disable SYSLIB0050
            private static object CallCtorDelegate(XamlTypeInvoker type)
            {
                object inst = FormatterServices.GetUninitializedObject(type._xamlType.UnderlyingType);
                InvokeDelegate(type._constructorDelegate, inst);
                return inst;
            }
#pragma warning restore SYSLIB0050
            private static void InvokeDelegate(Action<object> action, object argument)
            {
                action.Invoke(argument);
            }

            // returns true if a delegate is available, false if not
            private static bool EnsureConstructorDelegate(XamlTypeInvoker type)
            {
                if (type._constructorDelegate is not null)
                {
                    return true;
                }

                if (!type.IsPublic)
                {
                    return false;
                }

                if (s_securityFailureWithCtorDelegate == ThreeValuedBool.NotSet)
                {
                    s_securityFailureWithCtorDelegate =
                        ThreeValuedBool.False;
                }

                if (s_securityFailureWithCtorDelegate == ThreeValuedBool.True)
                {
                    return false;
                }

                Type underlyingType = type._xamlType.UnderlyingType.UnderlyingSystemType;
                // Look up public ctors only, for equivalence with Activator.CreateInstance
                ConstructorInfo tConstInfo = underlyingType.GetConstructor(Type.EmptyTypes);
                if (tConstInfo is null)
                {
                    // Throwing MissingMethodException for equivalence with Activator.CreateInstance
                    throw new MissingMethodException(SR.Format(SR.NoDefaultConstructor, underlyingType.FullName));
                }

                if ((tConstInfo.IsSecurityCritical && !tConstInfo.IsSecuritySafeCritical) ||
                    (tConstInfo.Attributes & MethodAttributes.HasSecurity) == MethodAttributes.HasSecurity ||
                    (underlyingType.Attributes & TypeAttributes.HasSecurity) == TypeAttributes.HasSecurity)
                {
                    // We don't want to bypass security checks for a critical or demanding ctor,
                    // so just treat it as if it were non-public
                    type._isPublic = ThreeValuedBool.False;
                    return false;
                }

                IntPtr constPtr = tConstInfo.MethodHandle.GetFunctionPointer();
                // This requires Reflection Permission
                Action<object> ctorDelegate = ctorDelegate =
                    (Action<object>)s_actionCtor.Invoke(new object[] { null, constPtr });
                type._constructorDelegate = ctorDelegate;
                return true;
            }
        }
    }
}
