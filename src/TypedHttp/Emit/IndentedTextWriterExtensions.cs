using System.CodeDom.Compiler;

namespace TypedHttp.Emit;

internal static class IndentedTextWriterExtensions
{
    /// <summary>
    /// Opens a braced block: emits <c>{</c> and indents. Disposing the result emits the
    /// matching <c>}</c> and dedents.
    /// </summary>
    public static Indentation Block(this IndentedTextWriter writer) => new(writer);

    /// <summary>
    /// Writes <paramref name="header"/> on its own line, then opens a braced block below it
    /// (e.g. a type/method/statement header followed by its body). Disposing the result emits
    /// the matching <c>}</c> and dedents.
    /// </summary>
    public static Indentation Block(this IndentedTextWriter writer, string header)
    {
        writer.WriteLine(header);
        return new Indentation(writer);
    }

    public static void SplitAndWriteLines(this IndentedTextWriter writer, string text)
    {
        // IndentedTextWriter only indents at WriteLine boundaries, so emit line-by-line;
        // each line then picks up the current indent, and \r\n is applied by the writer.
        foreach (var line in text.Split('\n'))
            writer.WriteLine(line.TrimEnd('\r'));
    }
}
