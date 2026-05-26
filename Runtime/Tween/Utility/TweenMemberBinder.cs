using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace XO.Entityween
{
    internal static class TweenMemberBinder
    {
        private static readonly Dictionary<(Type TargetType, string MemberName, Type ValueType), object> _setters = new();
        private static readonly Dictionary<(Type TargetType, string MemberName, Type ValueType), object> _getters = new();
        private static readonly object _lock = new();

        public static bool TryCreateSetter<T>(
            object target,
            string memberName,
            out Action<object, T> setter,
            out string error)
            where T : unmanaged
        {
            setter = null;
            error = null;

            if (target == null)
            {
                error = "Target object is null.";
                return false;
            }

            if (string.IsNullOrEmpty(memberName))
            {
                error = "Member name is null or empty.";
                return false;
            }

            var targetType = target.GetType();
            var valueType = typeof(T);
            var key = (targetType, memberName, valueType);

            lock (_lock)
            {
                if (_setters.TryGetValue(key, out var cached))
                {
                    setter = (Action<object, T>)cached;
                    return true;
                }

                var prop = targetType.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop != null)
                {
                    if (prop.PropertyType != valueType)
                    {
                        error = $"Property '{memberName}' type mismatch. Expected {valueType.Name}, but found {prop.PropertyType.Name}.";
                        return false;
                    }

                    var setMethod = prop.GetSetMethod();
                    if (setMethod == null)
                    {
                        error = $"Property '{memberName}' does not have a public setter.";
                        return false;
                    }

                    try
                    {
                        var createSetterMethod = typeof(TweenMemberBinder)
                            .GetMethod(nameof(CreateSetterDelegate), BindingFlags.NonPublic | BindingFlags.Static)
                            .MakeGenericMethod(targetType, valueType);

                        setter = (Action<object, T>)createSetterMethod.Invoke(null, new object[] { setMethod });
                        _setters[key] = setter;
                        return true;
                    }
                    catch (Exception ex)
                    {
                        error = $"Failed to create property setter delegate: {ex.Message}";
                        return false;
                    }
                }

                var field = targetType.GetField(memberName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (field != null)
                {
                    if (field.FieldType != valueType)
                    {
                        error = $"Field '{memberName}' type mismatch. Expected {valueType.Name}, but found {field.FieldType.Name}.";
                        return false;
                    }

                    if (field.IsInitOnly || field.IsLiteral)
                    {
                        error = $"Field '{memberName}' is readonly or constant.";
                        return false;
                    }

                    setter = (tgt, val) => field.SetValue(tgt, val);
                    _setters[key] = setter;
                    return true;
                }

                error = $"Member '{memberName}' not found on type '{targetType.Name}' as a public instance property or field.";
                return false;
            }
        }

        public static bool TryCreateGetter<T>(
            object target,
            string memberName,
            out Func<object, T> getter,
            out string error)
            where T : unmanaged
        {
            getter = null;
            error = null;

            if (target == null)
            {
                error = "Target object is null.";
                return false;
            }

            if (string.IsNullOrEmpty(memberName))
            {
                error = "Member name is null or empty.";
                return false;
            }

            var targetType = target.GetType();
            var valueType = typeof(T);
            var key = (targetType, memberName, valueType);

            lock (_lock)
            {
                if (_getters.TryGetValue(key, out var cached))
                {
                    getter = (Func<object, T>)cached;
                    return true;
                }

                var prop = targetType.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop != null)
                {
                    if (prop.PropertyType != valueType)
                    {
                        error = $"Property '{memberName}' type mismatch. Expected {valueType.Name}, but found {prop.PropertyType.Name}.";
                        return false;
                    }

                    var getMethod = prop.GetGetMethod();
                    if (getMethod == null)
                    {
                        error = $"Property '{memberName}' does not have a public getter.";
                        return false;
                    }

                    try
                    {
                        var createGetterMethod = typeof(TweenMemberBinder)
                            .GetMethod(nameof(CreateGetterDelegate), BindingFlags.NonPublic | BindingFlags.Static)
                            .MakeGenericMethod(targetType, valueType);

                        getter = (Func<object, T>)createGetterMethod.Invoke(null, new object[] { getMethod });
                        _getters[key] = getter;
                        return true;
                    }
                    catch (Exception ex)
                    {
                        error = $"Failed to create property getter delegate: {ex.Message}";
                        return false;
                    }
                }

                var field = targetType.GetField(memberName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (field != null)
                {
                    if (field.FieldType != valueType)
                    {
                        error = $"Field '{memberName}' type mismatch. Expected {valueType.Name}, but found {field.FieldType.Name}.";
                        return false;
                    }

                    getter = (tgt) => (T)field.GetValue(tgt);
                    _getters[key] = getter;
                    return true;
                }

                error = $"Member '{memberName}' not found on type '{targetType.Name}' as a public instance property or field.";
                return false;
            }
        }

        private static Action<object, TValue> CreateSetterDelegate<TTarget, TValue>(MethodInfo setter)
            where TTarget : class
        {
            var typed = (Action<TTarget, TValue>)Delegate.CreateDelegate(
                typeof(Action<TTarget, TValue>),
                setter);

            return (target, value) => typed((TTarget)target, value);
        }

        private static Func<object, TValue> CreateGetterDelegate<TTarget, TValue>(MethodInfo getter)
            where TTarget : class
        {
            var typed = (Func<TTarget, TValue>)Delegate.CreateDelegate(
                typeof(Func<TTarget, TValue>),
                getter);

            return (target) => typed((TTarget)target);
        }

        public static void AotTouch<T>() where T : unmanaged
        {
            bool alwaysFalse = false;
            if (alwaysFalse)
            {
                var s = CreateSetterDelegate<DummyTarget<T>, T>(null);
                var g = CreateGetterDelegate<DummyTarget<T>, T>(null);
            }
        }

        private class DummyTarget<T> where T : unmanaged
        {
            public T Value { get; set; }
        }
    }
}
