using System.CodeDom.Compiler;

namespace TypedHttp.Emit;

/// <summary>
/// An open brace scope. Emits <c>{</c> and increases indentation on creation, then emits the
/// matching <c>}</c> and decreases indentation on dispose. Pair it with a <c>using</c> so the
/// closing brace can never be forgotten and the block structure is visible in the source.
/// </summary>
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
