using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace TypedHttp.Model;

/// <summary>
/// A value-equatable stand-in for <see cref="Diagnostic"/> that is safe to store in the
/// incremental pipeline model. A real <see cref="Diagnostic"/> holds a <see cref="Location"/>
/// which references its <see cref="SyntaxTree"/>; caching that would root the whole tree in
/// memory and make cache hits depend on unrelated edits. We keep only a serializable location
/// plus the descriptor and message arguments, and rehydrate a <see cref="Diagnostic"/> at the
/// <c>RegisterSourceOutput</c> stage via <see cref="CreateDiagnostic"/>.
/// </summary>
internal sealed record DiagnosticInfo(
    DiagnosticDescriptor        Descriptor,
    LocationInfo?               Location,
    ImmutableByValArray<string> MessageArgs)
{
    /// <summary>
    /// Mirrors <see cref="Diagnostic.Create(DiagnosticDescriptor, Location, object?[])"/> so call
    /// sites can swap <c>Diagnostic.Create</c> for <c>DiagnosticInfo.Create</c> unchanged.
    /// </summary>
    public static DiagnosticInfo Create(
        DiagnosticDescriptor descriptor,
        Location?            location,
        params object?[]?    messageArgs)
    {
        var args = ImmutableArray.CreateBuilder<string>(messageArgs?.Length ?? 0);
        if (messageArgs is not null)
        {
            foreach (var arg in messageArgs) args.Add(arg?.ToString() ?? string.Empty);
        }

        return new DiagnosticInfo(descriptor, LocationInfo.CreateFrom(location), args.DrainToImmutable().ByVal());
    }

    public Diagnostic CreateDiagnostic()
    {
        var args                                             = new object?[MessageArgs.Length];
        for (var i = 0; i < MessageArgs.Length; i++) args[i] = MessageArgs[i];

        return Diagnostic.Create(Descriptor, Location?.ToLocation(), args);
    }
}

/// <summary>
/// A value-equatable, syntax-tree-free snapshot of a <see cref="Microsoft.CodeAnalysis.Location"/>.
/// <see cref="TextSpan"/> and <see cref="LinePositionSpan"/> are value types, so equality is
/// structural and nothing keeps the originating <see cref="SyntaxTree"/> alive.
/// </summary>
internal sealed record LocationInfo(string FilePath, TextSpan TextSpan, LinePositionSpan LineSpan)
{
    public Location ToLocation() => Location.Create(FilePath, TextSpan, LineSpan);

    public static LocationInfo? CreateFrom(Location? location)
    {
        if (location?.SourceTree is null) return null;
        return new LocationInfo(location.SourceTree.FilePath, location.SourceSpan, location.GetLineSpan().Span);
    }
}
