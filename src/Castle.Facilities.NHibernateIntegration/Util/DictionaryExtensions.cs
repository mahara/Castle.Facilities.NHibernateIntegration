namespace Castle.Facilities.NHibernateIntegration.Util;

using System.Diagnostics.CodeAnalysis;

public static class DictionaryExtensions
{
    public static bool TryGetValueAs<TKey, TValue, TValueAs>(
        this IDictionary<TKey, TValue> dictionary,
        TKey key,
        [MaybeNullWhen(false)] out TValueAs? valueAs)
        where TValueAs : TValue
    {
        if (dictionary.TryGetValue(key, out var value))
        {
            if (value is TValueAs validValueAs)
            {
                valueAs = validValueAs;

                return true;
            }
        }

        valueAs = default;

        return false;
    }
}
