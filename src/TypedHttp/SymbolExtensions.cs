using Microsoft.CodeAnalysis;

namespace TypedHttp;

internal static class SymbolExtensions
{
    public static bool InheritsFrom(
        this ITypeSymbol self,
        ITypeSymbol      possibleParent)
    {
        for (var currentType = self; currentType != null;
             currentType = currentType.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(currentType,
                                                      possibleParent))
                return true;
        }

        return false;
    }
}
