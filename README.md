# xHTTP

A strongly typed HTTP clients source generator aiming for simplicity, zero extra runtime cost and ease of use.

## Current status

Working but with a few known limitations. See below.

Has the basic `Get`, `Post`, `Put`, `Delete`, `Patch`, `Head`, `Options` and generic `Request` attributes for picking the HTTP method and route template.

Has the `Headers` attribute for specifying request headers all at once.

Has the `AliasAs` (as `Query`), `Body`, `Header`, `Property` and `Authorize` attributes for parameters.

## Known Limitations

> These are tracked and will be addressed in future releases (primarily via diagnostics in v0.2).

- **Methods must return `Task`, `Task<T>`, `ValueTask`, or `ValueTask<T>`.** Returning a bare type (e.g., `User` instead of `Task<User>`) will produce invalid generated code. Always use an async return type.
- **Route template placeholders must exactly match parameter names.** A typo in a placeholder like `{usrId}` when the parameter is named `userId` will produce a compile error on the generated code without a clear indication of the source. Double-check your route templates.
- **Only one `[Body]` parameter per method.** If multiple parameters are marked with `[Body]`, only the first one is used. The others are silently ignored.
- **Do not combine conflicting parameter attributes.** Using mutually exclusive attributes on the same parameter (e.g., `[Authorize]` + `[Body]`, or `[Header]` + `[Body]`) produces inconsistent generated code. Use only one behavioral attribute per parameter.
- **Methods without a request attribute are silently skipped.** If a method in a `[Client]` interface is missing a request attribute (e.g., `[Get]`, `[Post]`), no code is generated for it, resulting in a compile error on the generated class. Every method must have exactly one request attribute.
- **Containing types must be `partial`.** If the `[Client]` interface is nested inside a class or struct, that outer type must be declared `partial`. There is currently no diagnostic for this.

## TODO

- [x] Fully working source generation.
- [ ] Feature detection by TFM and conditional code generation.
- [ ] Refit [attribute](https://github.com/reactiveui/refit/blob/main/Refit/Attributes.cs) parity (with adaptations for cases where attributes can be joined or renamed for better clarity).
- [ ] Class/struct support.
- [ ] Support for `{**part}` in routes.
- [ ] Diagnostics for common mistakes and limitations.
- [ ] Better support for source-generated JsonSerializerContexts.

## Non-goals

- Support for Newtonsoft.Json or other serializers.
- Customizable route parameters, query parameters and header serialization.
