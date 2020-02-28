using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Wokhan.WindowsFirewallNotifier.Common.Extensions
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
        public static string GetDescription(this Enum value)
        {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null)
            {
                FieldInfo field = type.GetField(name);
                if (field != null)
                {
                    DescriptionAttribute attr =
                           Attribute.GetCustomAttribute(field,
                             typeof(DescriptionAttribute)) as DescriptionAttribute;
                    if (attr != null)
                    {
                        return attr.Description;
                    }
                }
            }
            return null;
        }
    }
}
