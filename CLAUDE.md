# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

TypedHttp is a C# Roslyn **incremental source generator** that turns `[Client]`-annotated
interfaces into strongly typed `HttpClient` implementations at compile time (a Refit
alternative with zero extra runtime cost and full AOT compatibility). See `README.md` for
the complete user-facing attribute/return-type reference — do not duplicate it here.

## Build & Test

```bash
dotnet restore                        # lock files are enabled; restores all projects
dotnet build --no-restore
dotnet test  --no-build               # runs both test projects across all TFMs

# Single test / filtered run (xUnit v3):
dotnet test --filter "FullyQualifiedName~GetTests"
dotnet test test/TypedHttp.Tests/TypedHttp.Tests.csproj --filter "Name=Generator_GeneratesGetRequestsCorrectly"
```

The generator project (`src/TypedHttp`) targets **netstandard2.0** (required for analyzers).
The test and sample projects multi-target **net8.0;net9.0;net10.0;net11.0**, so the CI matrix
needs the 8/9/10/11 SDKs installed. Central package management is used — all versions live in
`Directory.Packages.props`, never in individual `.csproj` files.

## Architecture

The generator is a pipeline; follow it in this order when tracing behavior:

1. **`HttpClientGenerator.cs`** — the `IIncrementalGenerator` entry point. In
   `RegisterPostInitializationOutput` it emits the attribute definitions (see below), then
   uses `ForAttributeWithMetadataName(MetadataNames.Client, ...)` to find `[Client]` interfaces.

2. **Parser** (`Parsing/*.cs`: `ClientParser` drives `RequestParser`, which drives
   `ParameterParser` and `ReturnTypeParser`) — turns each interface's `SemanticModel` into an
   immutable **Model**. `KnownSymbols` caches the `INamedTypeSymbol`s the parsers compare against
   (the emitted attributes, `Task`/`ValueTask`, `Stream`, `HttpContent`, and the optional
   `TypedHttp.Response`/`Response<T>`). Parsing never throws for user errors: `Header.Parse` /
   `Template.Parse` return Tsu `Result<T, TErr>`, and each failure is collected as a
   `DiagnosticInfo` on the model instead of aborting.

3. **Model** (`Model/*.cs`: `Client`, `Request`, `Parameter`/`AliasedParameter`, `Header`,
   `Template`, `RequestBody`, `ReturnType`, `DiagnosticInfo`) — value-equatable data carriers.
   Correct equality here is critical for incremental-generator caching: use `ImmutableByValArray`
   (never a raw `ImmutableArray<T>`) for collection fields so they compare structurally, and store
   diagnostics as `DiagnosticInfo` — a syntax-tree-free, value-equatable stand-in for `Diagnostic`
   so the cached model doesn't root a whole `SyntaxTree`; the real `Diagnostic` is rehydrated at
   the `RegisterSourceOutput` stage. `ReturnType` is an abstract record hierarchy
   (`VoidReturnType`, `StringReturnType`, `StreamReturnType`, `ResponseOfTReturnType`, …), matched
   with pattern switches in the writer, not an enum + fields.

4. **Emit writers** (`Emit/*.cs`) — `ClientWriter` drives `RequestWriter` (and `TemplateWriter`
   for route/header templates) over an `IndentedTextWriter`. Prefer the helpers in
   `IndentedTextWriterExtensions`: `Block(...)` opens a `{`-delimited, auto-indented scope (dispose
   to close it) and `SplitAndWriteLines` emits a multi-line raw string literal with each line
   correctly indented — `IndentedTextWriter` only applies indentation at `WriteLine` boundaries,
   not on newlines embedded inside a single `Write`. `Names`/`Types` hold the generated field/local
   names (e.g. `___httpClient`) and fully-qualified type strings. Generated code is
   `global::`-qualified throughout with CRLF line endings.

When any `DiagnosticInfo` is present on the parsed `Client`, `ProcessClient` reports the
diagnostics and emits **no** source for that client (see `HttpClientGenerator.ProcessClient`).

Output file name is the interface name **minus the leading `I`** + `.Generated.cs`
(e.g. `ICrudClient` → `CrudClient.Generated.cs`), matching the generated class name.

### Attribute definitions are embedded resources

The `[Client]`, request, and parameter attributes are NOT compiled into the generator
assembly. They live as source files in `src/TypedHttp/Resources/*.cs`, are `<Compile Remove>`d
and shipped as `<EmbeddedResource>`s, then re-emitted into the *consumer's* compilation at
post-init. When editing an attribute, edit the file under `Resources/` — the copy under
`src/TypedHttp.Sample/Generated/` is generator output, not a source of truth.

### The generator's runtime dependencies must be plumbed into every consumption path

The generator uses **Tsu** (`Result<T, TErr>`) and **Microsoft.Bcl.HashCode** *at generation
time*, both referenced `PrivateAssets=all`. They don't flow transitively, so every way the
generator is consumed has to forward those DLLs explicitly — otherwise the generator throws
`FileNotFoundException` and silently emits nothing:
- **NuGet consumers** — the DLLs are packed under `analyzers/dotnet/cs` next to the generator
  (`Content` items in `TypedHttp.csproj`).
- **`OutputItemType="Analyzer"` ProjectReferences** (the sample, `TypedHttp.RuntimeIntegrationTests`)
  — forwarded by the `GetGeneratorDependencyTargetPaths` target in `TypedHttp.csproj`.
- **`test/TypedHttp.Tests`** — the generator runs in-process, so the DLLs must be in the test
  project's `deps.json`; it therefore references Tsu / Microsoft.Bcl.HashCode directly.

Adding a new generator dependency means updating all three.

## Two very different test projects

- **`test/TypedHttp.Tests`** — generator *snapshot* tests. Each test feeds a source string to
  `CSharpSourceGeneratorTest<HttpClientGenerator, DefaultVerifier>` (see `TestBase.cs`) and
  string-compares the emitted file against a hard-coded expected output. **Expected strings must
  match the generator byte-for-byte**, including CRLF line endings, `global::` prefixes, and the
  `ThisVersion` version/date stamped into the header — a formatting change in a writer means
  every affected expected block must be updated in lockstep. Organized by feature under
  `Features/` (HttpMethods, Parameters, ReturnTypes, Headers, Diagnostics, ...). `TestBase`
  exposes `TestGenerator` (assert emitted source) and `TestDiagnostics` (assert reported
  diagnostics via `{|#N:span|}` markup bound to `DiagnosticResult`s — a client that produces a
  diagnostic emits no source, so these assert id, location and message args instead of output).

- **`test/TypedHttp.RuntimeIntegrationTests`** (net8.0) — *runtime* tests that actually execute
  generated clients against an in-process ASP.NET Core `MockApiServer`, referencing the
  generator as an `Analyzer` (`OutputItemType="Analyzer"`). Use these to verify real HTTP
  behavior, not just emitted text.

## Release / versioning gotcha

Versioning is automated by **release-please**. `src/TypedHttp/ThisVersion.cs` and README version
snippets are rewritten in place via `# x-release-please-*` marker comments. **Never remove,
reword, or move those comment markers off their line**, and don't hand-edit the version —
release-please owns it.

## Inspecting generator output

`src/TypedHttp.Sample` has `EmitCompilerGeneratedFiles=true` writing to `Generated/`, so building
the sample is the fastest way to eyeball what the generator produces for a given interface.
