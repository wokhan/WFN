using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Wokhan.WindowsFirewallNotifier.Common.Core
{
    public static class EnumExtensions
    {
        /*
         * Get an enum description.
         * 
         * Usage:
         *  public enum MyEnum
            {
                [Description("Description for Foo")]
                Foo,
                [Description("Description for Bar")]
                Bar
            }
            MyEnum x = MyEnum.Foo;
            string description = x.GetDescription();
         * 
         */
        public static string? GetDescription(this Enum value)
        {
            Type type = value.GetType();
            var name = Enum.GetName(type, value);
            if (name != null)
            {
                FieldInfo? field = type.GetField(name);
                if (field != null)
                {
                    var attr = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
                    return attr?.Description;
                }
            }
            return null;
        }

        //TODO: remove as it's taken from Wokhan.Core library
        public static T GetOrSetAsyncValue<T>(this INotifyPropertyChanged src, Func<T> resolve, Action<string>? propertyChanged = null, string? backingFieldName = null, [CallerMemberName] string propertyName = null)
        {
            backingFieldName = backingFieldName ?? $"<{propertyName}>k_BackingField"; // TODO: check how this is generated. Could be broken with some .NET future implementations.
            var field = src.GetType().GetField(backingFieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field is null)
            {
                throw new ArgumentOutOfRangeException(nameof(backingFieldName));
            }
            var currentValue = (T)field.GetValue(src);
            if (currentValue is null)
            {
                _ = Task.Run(() => resolve()).ContinueWith(task => { field.SetValue(src, task.Result); propertyChanged?.Invoke(propertyName); }, TaskScheduler.Default);
            }
            return currentValue;
        }

        //TODO: remove as it's taken from Wokhan.Core library
        public static T GetOrSetValueAsync<T>(this INotifyPropertyChanged src, Func<Task<T>> resolveAsync, Action<string>? propertyChanged = null, string? fieldName = null, [CallerMemberName] string propertyName = null)
        {
            fieldName = fieldName ?? $"<{propertyName}>k_BackingField"; // TODO: check how this is generated. Could be broken with some .NET future implementations.
            var field = src.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field is null)
            {
                throw new ArgumentOutOfRangeException(nameof(fieldName));
            }
            var currentValue = (T)field.GetValue(src);
            if (currentValue is null)
            {
                _ = resolveAsync().ContinueWith(task => { field.SetValue(src, task.Result); propertyChanged?.Invoke(propertyName); }, TaskScheduler.Current);
            }
            return currentValue;
        }
    }
}
