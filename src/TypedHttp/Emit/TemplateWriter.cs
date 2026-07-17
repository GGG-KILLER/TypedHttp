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
        if (template.Parts.Length == 1)
        {
            if (template.Parts[0].Kind == TemplatePartKind.String)
            {
                writer.Write(
                    SymbolDisplay.FormatLiteral(
                        template.Parts[0].Value,
                        quote: true));
            }
            else
            {
                writer.Write("$\"{");
                writer.Write(interpolationTransform(template.Parts[0].Value)!);
                writer.Write("}\"");
            }
        }
        else
        {
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
}
