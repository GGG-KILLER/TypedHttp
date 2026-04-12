# xHTTP

A strongly typed HTTP clients source generator aiming for simplicity, zero extra runtime cost and ease of use.

## Current status

Working POC.

Has the basic `Get`, `Post`, `Put`, `Delete`, `Patch`, `Head`, `Options` and generic `Request` attributes for picking the HTTP method and route template.

Has the `Headers` attribute for specifying request headers all at once.

Has the `AliasAs` (as `Query`), `Body`, `Header`, `Property` and `Authorize` attributes for parameters.

## TODO

- [x] Fully working source generation.
- [ ] Feature detection by TFM and conditional code generation.
- [ ] Refit [attribute](https://github.com/reactiveui/refit/blob/main/Refit/Attributes.cs) parity.
- [ ] Class/struct support.
- [ ] Support for `{**part}` in routes.

## Non-goals

- Support for Newtonsoft.Json or other serializers.
- Customizable route parameters, query parameters and header serialization.
