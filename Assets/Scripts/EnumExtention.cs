using System;
using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;

public static class EnumExtensions
{
    public static string ToPrettyString(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        var attr = field?.GetCustomAttribute<DescriptionAttribute>();

        if (attr != null)
            return attr.Description;

        return Regex.Replace(value.ToString(), "(\\B[A-Z])", " $1");
    }
}