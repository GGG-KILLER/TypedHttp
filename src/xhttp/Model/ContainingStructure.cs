namespace Xhttp.Model;

public readonly record struct ContainingStructure(
    StructureKind Kind,
    string        Name);

public enum StructureKind { Namespace, Class, Struct }
