using Microsoft.CodeAnalysis;

namespace TypedHttp;

internal static class SymbolDisplayFormats
{
    public static readonly SymbolDisplayFormat FullTypeFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers
                            | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
                            | SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName
                            | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);
}
