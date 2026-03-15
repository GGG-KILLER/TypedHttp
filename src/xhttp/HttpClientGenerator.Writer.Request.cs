using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Text;
using System.Web;
using Microsoft.CodeAnalysis.CSharp;
using Xhttp.Model;

namespace Xhttp;

public partial class HttpClientGenerator
{
    private static partial class Writer
    {
        private const string RequestVar = "___request";
        private const string RouteVar   = "___route";

        private static void WriteRequest(
            IndentedTextWriter     writer,
            ImmutableArray<Header> clientHeaders,
            Request                request,
            CancellationToken      cancellationToken = default)
        {
            writer.WriteLineNoTabs("");

            // Write function header
            writer.Write("public ");
            if (request.ReturnType.Async) writer.Write("async ");
            writer.Write(request.ReturnType.Type);
            writer.Write(' ');
            writer.Write(request.Name);
            writer.Write('(');
            var firstParameter = true;
            foreach (var parameter in request.Parameters.Values)
            {
                if (!firstParameter) writer.Write(", ");
                firstParameter = false;

                writer.Write(parameter.Type);
                writer.Write(' ');
                writer.Write(parameter.Name);
            }
            writer.WriteLine(')');

            writer.WriteLine('{');
            writer.Indent++;

            // Initialize request
            WriteRoute(writer, request, cancellationToken);
            writer.WriteLine(
                $"using var {RequestVar} = new {Types.HttpRequestMessage}({GetHttpMethod(request.Method)}, {RouteVar});");

            writer.WriteLineNoTabs("");
            foreach (var parameter in request.Parameters.Values.Where(p => p.Kind is ParameterKind.NonStringProperty
                                                                            or ParameterKind.StringProperty))
            {
                cancellationToken.ThrowIfCancellationRequested();

                writer.WriteLine(
                    $"{RequestVar}.Properties.Add({SymbolDisplay.FormatLiteral(parameter.PropertyName ?? parameter.Name, quote: true)}, {parameter.Name})");
            }

            writer.WriteLineNoTabs("");
            foreach (var header in clientHeaders)
            {
                cancellationToken.ThrowIfCancellationRequested();

                writer.Write($"{RequestVar}.Headers.Add(");
                WriteTemplate(writer, header.Name);
                writer.Write(", ");
                WriteTemplate(writer, header.Value);
                writer.WriteLine(");");
            }
            foreach (var header in request.Headers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                writer.Write($"{RequestVar}.Headers.Add(");
                WriteTemplate(writer, header.Name);
                writer.Write(", ");
                WriteTemplate(writer, header.Value);
                writer.WriteLine(");");
            }

            writer.WriteLineNoTabs("");
            writer.WriteLine("throw new global::System.NotImplementedException();");

            writer.Indent--;
            writer.WriteLine('}');
        }

        private static void WriteRoute(IndentedTextWriter writer, Request request, CancellationToken cancellationToken)
        {
            if (!request.Parameters.Values.Any(p => p.Kind is ParameterKind.StringQuery
                                                           or ParameterKind.NonStringQuery))
            {
                // Fast path for no StringBuilder
                cancellationToken.ThrowIfCancellationRequested();

                writer.Write($"var {RouteVar} = ");
                WriteTemplate(writer,
                              request.Route,
                              static str => $"({Types.HttpUtility}.UrlPathEncode({str}.ToString()))");
                writer.WriteLine(';');
                return;
            }

            // Write the path code
            writer.Write($"var {RouteVar}Builder = new {Types.StringBuilder}(");
            WriteTemplate(writer,
                          request.Route,
                          static str => $"({Types.HttpUtility}.UrlPathEncode({str}.ToString()))");
            writer.WriteLine(");");

            // Write the query string code
            writer.WriteLine($"{RouteVar}Builder.Append('?');");
            foreach (var parameter in request.Parameters.Values.Where(p => p.Kind is ParameterKind.StringQuery
                                                                            or ParameterKind.NonStringQuery))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (parameter.IsNullable) writer.Write($"if ({parameter.Name} is not null) ");

                writer.Write($"{RouteVar}Builder.Append($\"");
                writer.Write(SymbolDisplay.FormatLiteral(HttpUtility.UrlEncode(parameter.Alias ?? parameter.Name),
                                                         quote: false));
                writer.Write('=');
                writer.WriteLine($"{{({Types.HttpUtility}.UrlEncode({parameter.Name}.ToString()))}}&\");");
            }

            // Use the .ToString(^1) trick to remove any trailing ?s and &s
            writer.WriteLine($"var {RouteVar} = {RouteVar}Builder.ToString(0, {RouteVar}Builder.Length - 1);");
            writer.WriteLineNoTabs("");
        }

        private static string GetHttpMethod(string method)
        {
            if (string.Equals(method, "GET",   StringComparison.OrdinalIgnoreCase)) return $"{Types.HttpMethod}.Get";
            if (string.Equals(method, "HEAD",  StringComparison.OrdinalIgnoreCase)) return $"{Types.HttpMethod}.Head";
            if (string.Equals(method, "POST",  StringComparison.OrdinalIgnoreCase)) return $"{Types.HttpMethod}.Post";
            if (string.Equals(method, "PUT",   StringComparison.OrdinalIgnoreCase)) return $"{Types.HttpMethod}.Put";
            if (string.Equals(method, "TRACE", StringComparison.OrdinalIgnoreCase)) return $"{Types.HttpMethod}.Trace";
            if (string.Equals(method, "DELETE", StringComparison.OrdinalIgnoreCase))
                return $"{Types.HttpMethod}.Delete";
            if (string.Equals(method, "OPTIONS", StringComparison.OrdinalIgnoreCase))
                return $"{Types.HttpMethod}.Options";
            return
                $"new {Types.HttpMethod}({SymbolDisplay.FormatLiteral(method, quote: true)})";
        }
    }
}
