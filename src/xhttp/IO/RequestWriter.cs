using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Web;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis.CSharp;
using Xhttp.Model;

namespace Xhttp.IO;

internal sealed class RequestWriter(IndentedTextWriter writer) : BaseWriter(writer)
{
    public void WriteRequest(
        ImmutableArray<Header> clientHeaders,
        Request                request,
        CancellationToken      cancellationToken = default)
    {
        Writer.WriteLineNoTabs("");

        // Write function header
        Writer.Write($"public async {request.ReturnType.Type} {request.Name}(");
        var firstParameter = true;
        foreach (var parameter in request.Parameters)
        {
            if (!firstParameter) Writer.Write(", ");
            firstParameter = false;

            Writer.Write($"{parameter.Type} {parameter.Name}");
        }
        Writer.WriteLine(')');
        using var _1 = Writer.Indent(); // method curlies

        // Initialize request
        WriteRoute(request, cancellationToken);
        Writer.WriteLine(
            $"using (var {Names.RequestVar} = new {Types.HttpRequestMessage}({GetHttpMethod(request.Method)}, {Names.RouteVar}))");
        using var _2 = Writer.Indent();

        foreach (var parameter in request.Parameters.Where(p => p.IsProperty))
        {
            cancellationToken.ThrowIfCancellationRequested();

            Writer.WriteLine(
                $"{Names.RequestVar}.Properties.Add({SymbolDisplay.FormatLiteral(parameter.PropertyName ?? parameter.Name, quote: true)}, {parameter.Name});");
        }
        foreach (var header in clientHeaders)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Writer.Write($"{Names.RequestVar}.Headers.Add(");
            WriteTemplate(header.Name);
            Writer.Write(", ");
            WriteTemplate(header.Value);
            Writer.WriteLine(");");
        }
        foreach (var header in request.Headers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Writer.Write($"{Names.RequestVar}.Headers.Add(");
            WriteTemplate(header.Name);
            Writer.Write(", ");
            WriteTemplate(header.Value);
            Writer.WriteLine(");");
        }

        using var _3 = WriteRequestContent(request.Parameters.FirstOrDefault(p => p.IsBody));

        if (request.ReturnType.Kind != ReturnTypeKind.HttpResponseMessage) Writer.Write("using (");
        Writer.Write($"var {Names.ResponseVar} = ");
        var ctsParam = request.Parameters.FirstOrDefault(p => p.Kind == ParameterKind.CancellationToken);
        Writer.Write(ctsParam is not null
                         ? $"await this.{Names.HttpClientField}.SendAsync({Names.RequestVar}, {ctsParam.Name}).ConfigureAwait(false)"
                         : $"await this.{Names.HttpClientField}.SendAsync({Names.RequestVar}).ConfigureAwait(false)");

        Indentation? responseIndent = null;
        if (request.ReturnType.Kind != ReturnTypeKind.HttpResponseMessage)
        {
            Writer.WriteLine(')');
            responseIndent = Writer.Indent();
        }
        else
            Writer.WriteLine(';');
        using var _4 = responseIndent;

        WriteResponseContent(request.ReturnType, ctsParam);
    }

    private void WriteRoute(Request request, CancellationToken cancellationToken)
    {
        if (!request.Parameters.Any(p => p.IsQuery))
        {
            // Fast path for no StringBuilder
            cancellationToken.ThrowIfCancellationRequested();

            Writer.Write($"var {Names.RouteVar} = ");
            WriteTemplate(request.Route,
                          static str => $"({Types.HttpUtility}.UrlPathEncode({str}.ToString()))");
            Writer.WriteLine(';');
            return;
        }

        // Write the path code
        Writer.Write($"var {Names.RouteBuilderVar} = new {Types.StringBuilder}(");
        WriteTemplate(request.Route,
                      static str => $"({Types.HttpUtility}.UrlPathEncode({str}.ToString()))");
        Writer.WriteLine(");");

        // Write the query string code
        Writer.WriteLine($"{Names.RouteBuilderVar}.Append('?');");
        foreach (var parameter in request.Parameters.Where(p => p.IsQuery))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (parameter.IsNullable) Writer.Write($"if ({parameter.Name} is not null) ");

            var encodedName =
                SymbolDisplay.FormatLiteral(HttpUtility.UrlEncode(parameter.Alias ?? parameter.Name), quote: false);
            Writer.WriteLine(
                $"{Names.RouteBuilderVar}.Append($\"{encodedName}={{({Types.HttpUtility}.UrlEncode({parameter.Name}.ToString()))}}&\");");
        }

        // Use the .ToString(^1) trick to remove any trailing ?s and &s
        Writer.WriteLine(
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
    private Indentation? WriteRequestContent(Parameter? body)
    {
        if (body == null) return null;

        Indentation? indent = null;
        Writer.WriteLine($"{Types.HttpContent} {Names.HttpContentVar};");
        switch (body.Kind)
        {
            case ParameterKind.HttpContentBody: Writer.WriteLine($"{Names.HttpContentVar} = {body.Name};"); break;

            case ParameterKind.StreamBody:
                Writer.WriteLine($"using ({Names.HttpContentVar} = new {Types.StreamContent}({body.Name}))");
                indent = Writer.Indent();
                break;

            case ParameterKind.StringBody:
                Writer.WriteLine(
                    $"using ({Names.HttpContentVar} = new {Types.ByteArrayContent}({Types.Encoding}.UTF8.GetBytes({body.Name})))");
                indent = Writer.Indent();
                break;

            case ParameterKind.JsonBody:
                Writer.WriteLine($"if (this.{Names.JsonContextField} is not null)");
                using (Writer.Indent())
                    Writer.WriteLine(
                        $"{Names.HttpContentVar} = {Types.JsonContent}.Create<{body.Type}>({body.Name}, ({Types.JsonTypeInfo}<{body.Type}>) this.{Names.JsonContextField}.GetTypeInfo(typeof({body.Type})));");
                Writer.WriteLine("else");
                using (Writer.Indent())
                    Writer.WriteLine(
                        $"{Names.HttpContentVar} = {Types.JsonContent}.Create<{body.Type}>({body.Name}, options: this.{Names.JsonOptionsField});");
                Writer.WriteLine($"using ({Names.HttpContentVar})");
                indent = Writer.Indent();
                break;

            default: throw new InvalidOperationException($"Invalid body type: {body.Kind}");
        }
        Writer.WriteLine($"{Names.RequestVar}.Content = {Names.HttpContentVar};");
        return indent;
    }

    private void WriteResponseContent(ReturnType ret, Parameter? ctsParam)
    {
        var cts1Str = ctsParam is not null ? ctsParam.Name : string.Empty;
        var cts2Str = ctsParam is not null ? $", {ctsParam.Name}" : string.Empty;
        switch (ret.Kind)
        {
            case ReturnTypeKind.HttpResponseMessage: Writer.WriteLine($"return {Names.ResponseVar};"); break;
            case ReturnTypeKind.String:
                // TODO: Check for CancellationToken compatibility
                Writer.WriteLine(
                    $"return await {Names.ResponseVar}.Content.ReadAsStringAsync({cts1Str}).ConfigureAwait(false);");
                break;
            case ReturnTypeKind.Stream:
                // TODO: Check for CancellationToken compatibility
                Writer.WriteLine(
                    $"return await {Names.ResponseVar}.Content.ReadAsStreamAsync({cts1Str}).ConfigureAwait(false);");
                break;
            case ReturnTypeKind.Custom:
                Writer.WriteLine($"if (this.{Names.JsonContextField} is not null)");
                using (Writer.Indent())
                {
                    var arg1 =
                        $"({Types.JsonTypeInfo}<{ret.InnerType}>) this.{Names.JsonContextField}.GetTypeInfo(typeof({ret.InnerType}))";
                    Writer.WriteLine(
                        $"return await {Names.ResponseVar}.Content.ReadFromJsonAsync<{ret.InnerType}>({arg1}{cts2Str}).ConfigureAwait(false);");
                }
                Writer.WriteLine($"else if (this.{Names.JsonOptionsField} is not null)");
                using (Writer.Indent())
                    Writer.WriteLine(
                        $"return await {Names.ResponseVar}.Content.ReadFromJsonAsync<{ret.InnerType}>(this.{Names.JsonOptionsField}{cts2Str}).ConfigureAwait(false);");
                Writer.WriteLine("else");
                using (Writer.Indent())
                    Writer.WriteLine(
                        $"return await {Names.ResponseVar}.Content.ReadFromJsonAsync<{ret.InnerType}>({cts1Str}).ConfigureAwait(false);");
                break;
            case ReturnTypeKind.Void:
                // do nothing
                break;
            default: throw new InvalidOperationException($"Invalid return type: {ret.Kind}");
        }
    }
}
