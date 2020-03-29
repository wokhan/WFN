using System;
using System.ComponentModel;
using System.Reflection;

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
    }
}
