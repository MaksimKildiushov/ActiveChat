using System.Reflection;

namespace Shared.Extensions;

public static class PropertiesHlp
{
    public static List<string> AllAndValues(object obj)
    {
        var props = obj.GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);

        var vals = props.Select(e => e.Name + " = " + e.GetValue(obj, null)).ToList();
        return vals;
    }
}
