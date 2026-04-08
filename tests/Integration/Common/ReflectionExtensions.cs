using System.Reflection;

namespace Integration.Common;

public static class ReflectionExtensions
{
    public static void SetPrivateProperty<T>(this T instance, string propertyName, object value)
    {
        var prop = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (prop == null) throw new ArgumentException($"Property '{propertyName}' doesnt exist in {typeof(T).Name}");
        
        var setter = prop.GetSetMethod(nonPublic: true);
        if (setter == null) throw new ArgumentException($"Property '{propertyName}' doesnt have setter (no even private).");

        setter.Invoke(instance, new[] { value });
    }
}