using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TypedHttp.Model;

namespace TypedHttp.Parsing;

internal sealed class ReturnTypeParser
{
    private readonly KnownSymbols              _knownSymbols;
    private readonly ImmutableArray<DiagnosticInfo>.Builder _diagnostics;

    public ReturnTypeParser(KnownSymbols knownSymbols, ImmutableArray<DiagnosticInfo>.Builder diagnostics)
    {
        _knownSymbols = knownSymbols;
        _diagnostics  = diagnostics;
    }

    public ReturnType Parse(IMethodSymbol method)
    {
        var returnType  = method.ReturnType;
        var fullTypeStr = returnType.ToDisplayString(SymbolDisplayFormats.FullTypeFormat);

        // Check for void return type
        if (method.ReturnsVoid)
        {
            _diagnostics.Add(
                DiagnosticInfo.Create(
                    Diagnostics.NonAsyncReturn,
                    method.Locations.FirstOrDefault(),
                    method.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)));

            return new VoidReturnType(fullTypeStr);
        }

        // Check for non-generic Task/ValueTask return type
        if (SymbolEqualityComparer.Default.Equals(returnType, _knownSymbols.Task)
         || SymbolEqualityComparer.Default.Equals(returnType, _knownSymbols.ValueTask))
        {
            return new VoidReturnType(fullTypeStr);
        }

        var isAsync = false;
        // Check for Task<T>/ValueTask<T> return type
        if (returnType is INamedTypeSymbol { TypeArguments.Length: 1 } namedType
         && (SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, _knownSymbols.TaskOfT)
          || SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, _knownSymbols.ValueTaskOfT)))
        {
            isAsync = true;
            // The rest needs the inner type
            returnType = namedType.TypeArguments[0];
        }

        // Diagnostic for non-async return.
        if (!isAsync)
        {
            _diagnostics.Add(
                DiagnosticInfo.Create(
                    Diagnostics.NonAsyncReturn,
                    method.Locations.FirstOrDefault(),
                    method.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)));
        }

        // Check for Response
        if (SymbolEqualityComparer.Default.Equals(returnType, _knownSymbols.Response))
        {
            return new ResponseReturnType(fullTypeStr);
        }

        // Check for Response<T>
        if (returnType is INamedTypeSymbol { TypeArguments.Length: 1 } responseNamedType
         && SymbolEqualityComparer.Default.Equals(responseNamedType.OriginalDefinition, _knownSymbols.ResponseOfT))
        {
            var innerTypeStr = responseNamedType.TypeArguments[0].ToDisplayString(SymbolDisplayFormats.FullTypeFormat);
            return new ResponseOfTReturnType(fullTypeStr, innerTypeStr);
        }

        // Check for HttpResponseMessage
        if (SymbolEqualityComparer.Default.Equals(returnType, _knownSymbols.HttpResponseMessage))
        {
            return new HttpResponseMessageReturnType(fullTypeStr);
        }

        // Check for Stream
        if (SymbolEqualityComparer.Default.Equals(returnType, _knownSymbols.Stream))
        {
            return new StreamReturnType(fullTypeStr);
        }

        // Check for String
        if (returnType.SpecialType == SpecialType.System_String)
        {
            return new StringReturnType(fullTypeStr);
        }

        // If everything else fails, it's a type that needs deserializing
        return new CustomReturnType(fullTypeStr, returnType.ToDisplayString(SymbolDisplayFormats.FullTypeFormat));
    }
}
