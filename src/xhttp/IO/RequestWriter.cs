using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Web;
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
        Writer.Write($"public {(request.ReturnType.Async ? "async " : "")}{request.ReturnType.Type} {request.Name}(");
        var firstParameter = true;
        foreach (var parameter in request.Parameters.Values)
        {
            if (!firstParameter) Writer.Write(", ");
            firstParameter = false;

            Writer.Write($"{parameter.Type} {parameter.Name}");
        }
        Writer.WriteLine(')');

        Writer.WriteLine('{');
        Writer.Indent++;

        // Initialize request
        WriteRoute(request, cancellationToken);
        Writer.WriteLine(
            $"using var {Names.RequestVar} = new {Types.HttpRequestMessage}({GetHttpMethod(request.Method)}, {Names.RouteVar});");

        Writer.WriteLineNoTabs("");
        foreach (var parameter in request.Parameters.Values.Where(p => p.IsProperty))
        {
            cancellationToken.ThrowIfCancellationRequested();

            Writer.WriteLine(
                $"{Names.RequestVar}.Properties.Add({SymbolDisplay.FormatLiteral(parameter.PropertyName ?? parameter.Name, quote: true)}, {parameter.Name})");
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

        Writer.WriteLineNoTabs("");
        WriteRequestContent(request.Parameters.Values.FirstOrDefault(p => p.IsBody));
        Writer.WriteLine("throw new global::System.NotImplementedException();");

        Writer.Indent--;
        Writer.WriteLine('}');
    }

    private void WriteRequestContent(Parameter? body)
    {
        switch (body?.Kind)
        {
            case ParameterKind.HttpContentBody: Writer.WriteLine($"{Names.RequestVar}.Content = {body.Name};"); break;

            case ParameterKind.StreamBody:
                Writer.WriteLine($"{Names.RequestVar}.Content = new {Types.StreamContent}({body.Name});");
                break;

            case ParameterKind.StringBody:
                Writer.WriteLine(
                    $"{Names.RequestVar}.Content = new {Types.ByteArrayContent}({Types.Encoding}.UTF8.GetBytes({body.Name}));");
                break;

            case ParameterKind.JsonBody:
                Writer.WriteLine($"if (this.{Names.JsonContextField} is not null)");
                Writer.Indent++;
                Writer.WriteLine(
                    $"{Names.RequestVar}.Content = {Types.JsonContent}.Create<{body.Type}>({body.Name}, ({Types.JsonTypeInfo}<{body.Type}>) this.{Names.JsonContextField}.GetTypeInfo(typeof({body.Type})));");
                Writer.Indent--;
                Writer.WriteLine("else");
                Writer.Indent++;
                Writer.WriteLine(
                    $"{Names.RequestVar}.Content = {Types.JsonContent}.Create<{body.Type}>({body.Name}, options: this.{Names.JsonOptionsField});");
                Writer.Indent--;
                break;
        }
    }

    private void WriteRoute(Request request, CancellationToken cancellationToken)
    {
        if (!request.Parameters.Values.Any(p => p.IsQuery))
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
        foreach (var parameter in request.Parameters.Values.Where(p => p.IsQuery))
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
        Writer.WriteLineNoTabs("");
    }

    private static string GetHttpMethod(string method)
    {
        if (string.Equals(method, "GET",     StringComparison.OrdinalIgnoreCase)) return $"{Types.HttpMethod}.Get";
        if (string.Equals(method, "HEAD",    StringComparison.OrdinalIgnoreCase)) return $"{Types.HttpMethod}.Head";
        if (string.Equals(method, "POST",    StringComparison.OrdinalIgnoreCase)) return $"{Types.HttpMethod}.Post";
        if (string.Equals(method, "PUT",     StringComparison.OrdinalIgnoreCase)) return $"{Types.HttpMethod}.Put";
        if (string.Equals(method, "TRACE",   StringComparison.OrdinalIgnoreCase)) return $"{Types.HttpMethod}.Trace";
        if (string.Equals(method, "DELETE",  StringComparison.OrdinalIgnoreCase)) return $"{Types.HttpMethod}.Delete";
        if (string.Equals(method, "OPTIONS", StringComparison.OrdinalIgnoreCase)) return $"{Types.HttpMethod}.Options";
        return $"new {Types.HttpMethod}({SymbolDisplay.FormatLiteral(method, quote: true)})";
    }
}
