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

2. **Parser** (`HttpClientGenerator.Parser.*.cs`, split by concern:
   `.Client`, `.Request`, `.Parameter`) — turns each interface's `SemanticModel` into an
   immutable **Model**.

3. **Model** (`Model/*.cs`: `Client`, `Request`, `Parameter`, `Header`, `Template`,
   `ReturnType`, `KnownSymbols`) — value-equatable data carriers. Correct equality here is
   critical for incremental-generator caching; `ImmutableByValArray`/`ImmutableByValDictionary`
   exist specifically to give collections structural equality (never use raw
   `ImmutableArray<T>` in a model field, or caching breaks).

4. **IO writers** (`IO/*.cs`) — `ClientWriter` drives `RequestWriter` over an
   `IndentedTextWriter` to emit the source text. `Names.cs`/`Types.cs` hold the fully-qualified
   type strings and generated field/local names (e.g. `___httpClient`). Generated code uses
   `global::`-qualified names throughout and CRLF line endings.

Output file name is the interface name **minus the leading `I`** + `.Generated.cs`
(e.g. `ICrudClient` → `CrudClient.Generated.cs`), matching the generated class name.

### Attribute definitions are embedded resources

The `[Client]`, request, and parameter attributes are NOT compiled into the generator
assembly. They live as source files in `src/TypedHttp/Resources/*.cs`, are `<Compile Remove>`d
and shipped as `<EmbeddedResource>`s, then re-emitted into the *consumer's* compilation at
post-init. When editing an attribute, edit the file under `Resources/` — the copy under
`src/TypedHttp.Sample/Generated/` is generator output, not a source of truth.

## Two very different test projects

- **`test/TypedHttp.Tests`** — generator *snapshot* tests. Each test feeds a source string to
  `CSharpSourceGeneratorTest<HttpClientGenerator, DefaultVerifier>` (see `TestBase.cs`) and
  string-compares the emitted file against a hard-coded expected output. **Expected strings must
  match the generator byte-for-byte**, including CRLF line endings, `global::` prefixes, and the
  `ThisVersion` version/date stamped into the header — a formatting change in a writer means
  every affected expected block must be updated in lockstep. Organized by feature under
  `Features/` (HttpMethods, Parameters, ReturnTypes, Headers, ...).

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
