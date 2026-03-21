using System.CodeDom.Compiler;

namespace Xhttp.IO;

internal readonly struct Indentation : IDisposable
{
    private readonly IndentedTextWriter _writer;

    public Indentation(IndentedTextWriter writer)
    {
        _writer = writer;

        writer.WriteLine('{');
        writer.Indent++;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _writer.Indent--;
        _writer.WriteLine('}');
    }
}

internal static class IndentedTextWriterExtensions
{
    public static Indentation Indent(this IndentedTextWriter writer) => new(writer);
}
