using System.CodeDom.Compiler;
using Microsoft.CodeAnalysis.CSharp;
using TypedHttp.Model;

namespace TypedHttp.Emit;

internal static class TemplateWriter
{
    public static void WriteTemplate(
        IndentedTextWriter    writer,
        Template              template,
        Func<string, string>? interpolationTransform = null)
    {
        interpolationTransform ??= static x => x;

        // Fast path: a lone literal with no interpolation holes. Emit a plain string,
        // un-doubling the escaped braces the parser kept doubled for the $"..." path below
        // (otherwise "{{" would reach runtime literally instead of collapsing to "{").
        if (template.Parts is [{ Kind: TemplatePartKind.String, Value: var literal }])
        {
            writer.Write(
                SymbolDisplay.FormatLiteral(
                    literal.Replace("{{", "{").Replace("}}", "}"),
                    quote: true));
            return;
        }

        writer.Write("$\"");
        foreach (var part in template.Parts)
        {
            if (part.Kind == TemplatePartKind.String)
            {
                writer.Write(
                    SymbolDisplay.FormatLiteral(
                        part.Value,
                        quote: false));
            }
            else
            {
                writer.Write('{');
                writer.Write(interpolationTransform(part.Value)!);
                writer.Write('}');
            }
        }
        writer.Write('"');
    }
}
