using System.CodeDom.Compiler;
using System.Collections.Immutable;
using Xhttp.Model;

namespace Xhttp;

internal sealed class RequestWriter(IndentedTextWriter writer)
{
    public void Write(Request request)
    {
        WriteStarts(request.ContainingStructures.Array);
        // TODO
        WriteEnds(request.ContainingStructures.Array);
    }

    private void WriteStarts(ImmutableArray<ContainingStructure> structures)
    {
        foreach (var structure in structures)
        {
            switch (structure.Kind)
            {
                case StructureKind.Namespace: writer.Write("namespace "); break;
                case StructureKind.Struct:
                    writer.Write("partial struct ");
                    break;
                case StructureKind.Class: writer.Write("partial class "); break;
                default: throw new ArgumentOutOfRangeException();
            }

            writer.WriteLine(structure.Name);
            writer.WriteLine("{");
            writer.Indent++;
        }
    }

    private void WriteEnds(ImmutableArray<ContainingStructure> structures)
    {
        for (var index = 0; index < structures.Length; index++)
        {
            writer.WriteLine("}");
            writer.Indent--;
        }
    }
}
