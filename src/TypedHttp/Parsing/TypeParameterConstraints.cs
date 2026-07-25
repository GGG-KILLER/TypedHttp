using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;

namespace TypedHttp.Parsing;

internal static class TypeParameterConstraints
{
    /// <summary>
    /// Builds the <c>where</c> constraint clauses for the given type parameters, rendered so they
    /// can be appended directly after a type or method declaration. Constraint types are fully
    /// <c>global::</c>-qualified (the generated file does not carry the source file's usings, so an
    /// unqualified name would not resolve). Returns an empty string when none of the type
    /// parameters are constrained; otherwise the result is prefixed with a leading space
    /// (e.g. <c>" where T : class, new()"</c>).
    /// </summary>
    public static string Build(ImmutableArray<ITypeParameterSymbol> typeParameters)
    {
        if (typeParameters.IsDefaultOrEmpty) return string.Empty;

        var builder = new StringBuilder();
        foreach (var typeParameter in typeParameters)
        {
            var constraints = GetConstraints(typeParameter);
            if (constraints.Count == 0) continue;

            builder.Append(" where ");
            builder.Append(typeParameter.Name);
            builder.Append(" : ");
            builder.Append(string.Join(", ", constraints));
        }

        return builder.ToString();
    }

    private static List<string> GetConstraints(ITypeParameterSymbol typeParameter)
    {
        var constraints = new List<string>();

        // Primary constraint, at most one and always first per the C# grammar. `unmanaged` also
        // sets HasValueTypeConstraint, so it must be checked before `struct`.
        if (typeParameter.HasReferenceTypeConstraint)
        {
            constraints.Add(
                typeParameter.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated
                    ? "class?"
                    : "class");
        }
        else if (typeParameter.HasUnmanagedTypeConstraint)
        {
            constraints.Add("unmanaged");
        }
        else if (typeParameter.HasValueTypeConstraint)
        {
            constraints.Add("struct");
        }
        else if (typeParameter.HasNotNullConstraint)
        {
            constraints.Add("notnull");
        }

        // Base class, interface and type-parameter constraints, in declaration order (Roslyn keeps
        // the base class first, which is where the grammar requires it).
        var constraintTypes           = typeParameter.ConstraintTypes;
        var constraintNullabilities   = typeParameter.ConstraintNullableAnnotations;
        for (var i = 0; i < constraintTypes.Length; i++)
        {
            constraints.Add(
                constraintTypes[i]
                   .WithNullableAnnotation(constraintNullabilities[i])
                   .ToDisplayString(SymbolDisplayFormats.FullTypeFormat));
        }

        // The constructor constraint must come last.
        if (typeParameter.HasConstructorConstraint) constraints.Add("new()");

        return constraints;
    }
}
