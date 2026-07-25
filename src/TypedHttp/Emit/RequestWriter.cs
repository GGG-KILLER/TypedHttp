using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Web;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis.CSharp;
using TypedHttp.Model;

namespace TypedHttp.Emit;

internal sealed class RequestWriter(IndentedTextWriter writer)
{
    public void WriteRequest(
        ImmutableArray<Header> clientHeaders,
        Request                request,
        CancellationToken      cancellationToken = default)
    {
        writer.WriteLineNoTabs("");

        // Write function header
        writer.Write($"public async {request.ReturnType.FullType} {request.Name}(");
        var firstParameter = true;
        foreach (var parameter in request.Parameters)
        {
            if (!firstParameter) writer.Write(", ");
            firstParameter = false;

            writer.Write($"{parameter.Type} {parameter.Name}");
        }
        writer.Write(')');
        writer.WriteLine(request.ConstraintClauses);

        using (writer.Block( /* empty because we write the function header above */))
        {
            // Initialize request
            WriteRoute(request, cancellationToken);

            using (writer.Block(
                       $"using (var {Names.RequestVar} = new {Types.HttpRequestMessage}({GetHttpMethod(request.Method)}, {Names.RouteVar}))"))
            {
                foreach (var parameter in request.Properties)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    writer.WriteLine(
                        $"{Names.RequestVar}.Properties.Add({SymbolDisplay.FormatLiteral(parameter.Alias, quote: true)}, {parameter.Name});");
                }

                WriteHeaders(request.Headers.Array, cancellationToken);

                if (clientHeaders.Length > 0)
                {
                    // Request headers override client headers with the same (case-insensitive)
                    // name, so skip any client header the request already sets.
                    var requestHeaderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var header in request.Headers.Array)
                        if (TryGetLiteralName(header.Name) is { } name)
                            requestHeaderNames.Add(name);

                    WriteHeaders(
                        ((IEnumerable<Header>)clientHeaders).Where(
                            h => TryGetLiteralName(h.Name) is not { } n || !requestHeaderNames.Contains(n)),
                        cancellationToken);
                }

                using (WriteRequestBody(request.Body))
                {
                    var send = request.CancellationTokenParameter is { } ct
                                   ? $"await this.{Names.HttpClientField}.SendAsync({Names.RequestVar}, {ct}).ConfigureAwait(false)"
                                   : $"await this.{Names.HttpClientField}.SendAsync({Names.RequestVar}).ConfigureAwait(false)";

                    if (request.ReturnType.NeedsUndisposedResponse)
                    {
                        writer.WriteLine($"var {Names.ResponseVar} = {send};");
                        WriteResponseContent(request.ReturnType, request.CancellationTokenParameter);
                    }
                    else
                    {
                        using (writer.Block($"using (var {Names.ResponseVar} = {send})"))
                            WriteResponseContent(request.ReturnType, request.CancellationTokenParameter);
                    }
                }
            }
        }
    }

    private void WriteHeaders(IEnumerable<Header> headers, CancellationToken cancellationToken)
    {
        foreach (var header in headers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            writer.Write($"{Names.RequestVar}.Headers.TryAddWithoutValidation(");
            TemplateWriter.WriteTemplate(writer, header.Name);
            writer.Write(", ");
            TemplateWriter.WriteTemplate(writer, header.Value);
            writer.WriteLine(");");
        }
    }

    // Override resolution matches on the header name, which we can only compare when it is a
    // plain literal (the normal case). Templated names are treated as non-overriding.
    private static string? TryGetLiteralName(Template name) =>
        name.Parts.Length == 1 && name.Parts[0].Kind == TemplatePartKind.String
            ? name.Parts[0].Value
            : null;

    private void WriteRoute(Request request, CancellationToken cancellationToken)
    {
        if (request.QueryParameters.Length == 0)
        {
            // Fast path for no StringBuilder
            cancellationToken.ThrowIfCancellationRequested();

            writer.Write($"var {Names.RouteVar} = ");
            TemplateWriter.WriteTemplate(
                writer,
                request.Route,
                static str => $"({Types.HttpUtility}.UrlPathEncode({str}.ToString()))");
            writer.WriteLine(';');
            return;
        }

        // Write the path code
        writer.Write($"var {Names.RouteBuilderVar} = new {Types.StringBuilder}(");
        TemplateWriter.WriteTemplate(
            writer,
            request.Route,
            static str => $"({Types.HttpUtility}.UrlPathEncode({str}.ToString()))");
        writer.WriteLine(");");

        // Write the query string code
        writer.WriteLine($"{Names.RouteBuilderVar}.Append('?');");
        foreach (var query in request.QueryParameters)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (query.IsNullable) writer.Write($"if ({query.Name} is not null) ");

            writer.Write($"{Names.RouteBuilderVar}.Append(");
            TemplateWriter.WriteTemplate(
                writer,
                new Template(
                    [
                        new TemplatePart(TemplatePartKind.String, HttpUtility.UrlEncode(query.Alias)),
                        new TemplatePart(TemplatePartKind.String, "="), new TemplatePart(
                            TemplatePartKind.Parameter,
                            $"({Types.HttpUtility}.UrlEncode({query.Name}.ToString()))"),
                        new TemplatePart(TemplatePartKind.String, "&"),
                    ]));
            writer.WriteLine(");");
        }

        // Use the .ToString(0, len - 1) trick to remove any trailing ?s and &s
        writer.WriteLine(
            $"var {Names.RouteVar} = {Names.RouteBuilderVar}.ToString(0, {Names.RouteBuilderVar}.Length - 1);");
    }

    private static string GetHttpMethod(string method)
    {
        if (string.Equals(method, "GET",     StringComparison.OrdinalIgnoreCase)) return $"{Types.HttpMethod}.Get";
        if (string.Equals(method, "HEAD",    StringComparison.OrdinalIgnoreCase)) return $"{Types.HttpMethod}.Head";
        if (string.Equals(method, "POST",    StringComparison.OrdinalIgnoreCase)) return $"{Types.HttpMethod}.Post";
        if (string.Equals(method, "PUT",     StringComparison.OrdinalIgnoreCase)) return $"{Types.HttpMethod}.Put";
        if (string.Equals(method, "PATCH",   StringComparison.OrdinalIgnoreCase)) return $"{Types.HttpMethod}.Patch";
        if (string.Equals(method, "TRACE",   StringComparison.OrdinalIgnoreCase)) return $"{Types.HttpMethod}.Trace";
        if (string.Equals(method, "DELETE",  StringComparison.OrdinalIgnoreCase)) return $"{Types.HttpMethod}.Delete";
        if (string.Equals(method, "OPTIONS", StringComparison.OrdinalIgnoreCase)) return $"{Types.HttpMethod}.Options";
        return $"new {Types.HttpMethod}({SymbolDisplay.FormatLiteral(method, quote: true)})";
    }

    [MustDisposeResource]
    private Indentation? WriteRequestBody(RequestBody? body)
    {
        if (body == null) return null;

        Indentation? indent = null;
        writer.WriteLine($"{Types.HttpContent} {Names.HttpContentVar};");
        switch (body.Kind)
        {
            case BodyKind.HttpContent:
                // No using here since we assume the caller-provided resources should be disposed
                // of by the caller.
                writer.WriteLine($"{Names.HttpContentVar} = {body.Parameter};");
                break;

            case BodyKind.Stream:
                indent = writer.Block($"using ({Names.HttpContentVar} = new {Types.StreamContent}({body.Parameter}))");
                break;

            case BodyKind.String:
                indent = writer.Block(
                    $"using ({Names.HttpContentVar} = new {Types.ByteArrayContent}({Types.Encoding}.UTF8.GetBytes({body.Parameter})))");
                break;

            case BodyKind.Json:
                using (writer.Block($"if (this.{Names.JsonContextField} is not null)"))
                {
                    writer.WriteLine(
                        $"{Names.HttpContentVar} = {Types.JsonContent}.Create<{body.Type}>({body.Parameter}, ({Types.JsonTypeInfo}<{body.Type}>) this.{Names.JsonContextField}.GetTypeInfo(typeof({body.Type})));");
                }
                using (writer.Block("else"))
                {
                    writer.WriteLine(
                        $"{Names.HttpContentVar} = {Types.JsonContent}.Create<{body.Type}>({body.Parameter}, options: this.{Names.JsonOptionsField});");
                }
                indent = writer.Block($"using ({Names.HttpContentVar})");
                break;

            default: throw new InvalidOperationException($"Invalid body type: {body.Kind}");
        }
        writer.WriteLine($"{Names.RequestVar}.Content = {Names.HttpContentVar};");
        return indent;
    }

    private void WriteResponseContent(ReturnType ret, string? ctsParam)
    {
        var cts1Str = ctsParam ?? string.Empty;
        var cts2Str = ctsParam is not null ? $", {ctsParam}" : string.Empty;

        if (ret is not (ResponseReturnType or ResponseOfTReturnType or HttpResponseMessageReturnType))
            writer.WriteLine($"{Names.ResponseVar}.EnsureSuccessStatusCode();");

        switch (ret)
        {
            case HttpResponseMessageReturnType: writer.WriteLine($"return {Names.ResponseVar};"); break;
            case ResponseReturnType:
                writer.WriteLine($"return {Types.Response}.FromMessage({Names.ResponseVar});");
                break;
            case ResponseOfTReturnType(_, var innerType):
                writer.WriteLine($"{innerType} {Names.DeserializedJsonVar} = default!;");
                using (writer.Block($"if ({Names.ResponseVar}.IsSuccessStatusCode)"))
                    writeJsonDeserialization(innerType);
                writer.WriteLine(
                    $"return {Types.Response}<{innerType}>.FromMessage({Names.ResponseVar}, {Names.DeserializedJsonVar});");
                break;
            case StringReturnType:
                writer.SplitAndWriteLines(
                    $"""
                     #if NET5_0_OR_GREATER
                     return await {Names.ResponseVar}.Content.ReadAsStringAsync({cts1Str}).ConfigureAwait(false);
                     #else
                     return await {Names.ResponseVar}.Content.ReadAsStringAsync().ConfigureAwait(false);
                     #endif
                     """);
                break;
            case StreamReturnType:
                writer.SplitAndWriteLines(
                    $"""
                     #if NET5_0_OR_GREATER
                     return await {Names.ResponseVar}.Content.ReadAsStreamAsync({cts1Str}).ConfigureAwait(false);
                     #else
                     return await {Names.ResponseVar}.Content.ReadAsStreamAsync().ConfigureAwait(false);
                     #endif
                     """);
                break;
            case CustomReturnType(_, var innerType):
                writer.WriteLine($"{innerType} {Names.DeserializedJsonVar};");
                writeJsonDeserialization(innerType);
                writer.WriteLine($"return {Names.DeserializedJsonVar};");
                break;
            case VoidReturnType:
                // do nothing
                break;
            default: throw new InvalidOperationException($"Invalid return type: {ret.GetType()}");
        }
        return;

        void writeJsonDeserialization(string jsonType)
        {
            using (writer.Block($"if (this.{Names.JsonContextField} is not null)"))
            {
                var arg1 =
                    $"({Types.JsonTypeInfo}<{jsonType}>) this.{Names.JsonContextField}.GetTypeInfo(typeof({jsonType}))";
                writer.WriteLine(
                    $"{Names.DeserializedJsonVar} = await {Names.ResponseVar}.Content.ReadFromJsonAsync<{jsonType}>({arg1}{cts2Str}).ConfigureAwait(false);");
            }
            using (writer.Block($"else if (this.{Names.JsonOptionsField} is not null)"))
            {
                writer.WriteLine(
                    $"{Names.DeserializedJsonVar} = await {Names.ResponseVar}.Content.ReadFromJsonAsync<{jsonType}>(this.{Names.JsonOptionsField}{cts2Str}).ConfigureAwait(false);");
            }
            using (writer.Block("else"))
            {
                writer.WriteLine(
                    $"{Names.DeserializedJsonVar} = await {Names.ResponseVar}.Content.ReadFromJsonAsync<{jsonType}>({cts1Str}).ConfigureAwait(false);");
            }
        }
    }
}
