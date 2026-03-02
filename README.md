# xHTTP

A strongly typed HTTP clients source generator aiming for simplicity, zero extra runtime cost and ease of use.

## Current status

POC quality and feature count.

Has the basic `Get`, `Post`, `Put`, `Delete`, `Patch`, `Head`, `Options` and generic `Request` attributes for picking the HTTP method and route template.

Has the `Headers` attribute for specifying request headers all at once.

Has the `AliasAs`, `Body`, `Header`, `Property` and `Authorize` attributes for parameters.

## TODO

- [ ] Fully working source generation.
- [ ] Refit [attribute](https://github.com/reactiveui/refit/blob/main/Refit/Attributes.cs) parity.
- [ ] Per-type generation.
- [ ] Interface support.
