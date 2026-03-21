using System.CodeDom.Compiler;
using Microsoft.CodeAnalysis.CSharp;
using Xhttp.Model;

namespace Xhttp.IO;

internal abstract class BaseWriter(IndentedTextWriter writer)
{
    protected IndentedTextWriter Writer { get; } = writer;

    protected void WriteTemplate(
        Template              template,
        Func<string, string>? interpolationTransform = null)
    {
        interpolationTransform ??= static x => x;
        if (template.Parts.Length == 1)
        {
            if (template.Parts[0].Kind == TemplatePartKind.String)
            {
                Writer.Write(SymbolDisplay.FormatLiteral(template.Parts[0].Value,
                                                         quote: true));
            }
            else
            {
                Writer.Write("$\"{");
                Writer.Write(interpolationTransform(template.Parts[0].Value)!);
                Writer.Write("}\"");
            }
        }
        else
        {
            Writer.Write("$\"");
            foreach (var part in template.Parts)
            {
                if (part.Kind == TemplatePartKind.String)
                {
                    Writer.Write(SymbolDisplay.FormatLiteral(part.Value,
                                                             quote: false));
                }
                else
                {
                    Writer.Write('{');
                    Writer.Write(interpolationTransform(part.Value)!);
                    Writer.Write('}');
                }
            }

            Writer.Write('"');
        }
    }
}
