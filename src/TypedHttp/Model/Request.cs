namespace TypedHttp.Model;

internal sealed record Request(
    string                                Name,
    string                                ConstraintClauses,
    string                                Method,
    ImmutableByValArray<Header>           Headers,
    Template                              Route,
    ImmutableByValArray<AliasedParameter> QueryParameters,
    ImmutableByValArray<AliasedParameter> Properties,
    RequestBody?                          Body,
    ImmutableByValArray<Parameter>        Parameters,
    string?                               CancellationTokenParameter,
    ReturnType                            ReturnType);
