using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace TypedHttp.Tests.Features.Diagnostics;

public class DiagnosticsTests : TestBase
{
    [Fact]
    public async Task TYPEDHTTP001_ReportsMethodWithoutRequestMarker()
    {
        await TestDiagnostics(
            CSharp(
                """
                using TypedHttp;
                using System.Threading.Tasks;

                namespace X;

                [Client]
                public interface ICustomClient
                {
                    Task<string> {|#0:GetData|}();
                }
                """),
            DiagnosticResult.CompilerError("TYPEDHTTP001")
                            .WithLocation(0)
                            .WithArguments("ICustomClient.GetData()"));
    }

    [Fact]
    public async Task TYPEDHTTP002_ReportsMethodWithMultipleRequestMarkers()
    {
        await TestDiagnostics(
            CSharp(
                """
                using TypedHttp;
                using System.Threading.Tasks;

                namespace X;

                [Client]
                public interface ICustomClient
                {
                    [Get("a")]
                    [Post("b")]
                    Task<string> {|#0:GetData|}();
                }
                """),
            DiagnosticResult.CompilerError("TYPEDHTTP002")
                            .WithLocation(0)
                            .WithArguments("ICustomClient.GetData()"));
    }

    [Fact]
    public async Task TYPEDHTTP003_ReportsMalformedHeader()
    {
        await TestDiagnostics(
            CSharp(
                """
                using TypedHttp;
                using System.Threading.Tasks;

                namespace X;

                [Client]
                public interface ICustomClient
                {
                    [Get("data")]
                    [{|#0:Headers("no-colon-here")|}]
                    Task<string> GetData();
                }
                """),
            DiagnosticResult.CompilerError("TYPEDHTTP003")
                            .WithLocation(0)
                            .WithArguments("ICustomClient.GetData()", "header has no colon"));
    }

    [Fact]
    public async Task TYPEDHTTP004_ReportsMalformedRoute()
    {
        await TestDiagnostics(
            CSharp(
                """
                using TypedHttp;
                using System.Threading.Tasks;

                namespace X;

                [Client]
                public interface ICustomClient
                {
                    [{|#0:Get("data/{unclosed")|}]
                    Task<string> GetData();
                }
                """),
            DiagnosticResult.CompilerError("TYPEDHTTP004")
                            .WithLocation(0)
                            .WithArguments("ICustomClient.GetData()", "unclosed brace in route"));
    }

    [Fact]
    public async Task TYPEDHTTP005_ReportsRouteWithUnknownParameter()
    {
        await TestDiagnostics(
            CSharp(
                """
                using TypedHttp;
                using System.Threading.Tasks;

                namespace X;

                [Client]
                public interface ICustomClient
                {
                    [{|#0:Get("users/{id}")|}]
                    Task<string> GetData();
                }
                """),
            DiagnosticResult.CompilerError("TYPEDHTTP005")
                            .WithLocation(0)
                            .WithArguments("ICustomClient.GetData()", "id"));
    }

    [Fact]
    public async Task TYPEDHTTP007_ReportsMultipleBodyParameters()
    {
        await TestDiagnostics(
            CSharp(
                """
                using TypedHttp;
                using System.Threading.Tasks;

                namespace X;

                [Client]
                public interface ICustomClient
                {
                    [Post("items")]
                    Task {|#0:CreateItem|}([Body] string a, [Body] string b);
                }
                """),
            DiagnosticResult.CompilerError("TYPEDHTTP007")
                            .WithLocation(0)
                            .WithArguments("ICustomClient.CreateItem(string, string)"));
    }

    [Fact]
    public async Task TYPEDHTTP008_ReportsNonAsyncReturnType()
    {
        await TestDiagnostics(
            CSharp(
                """
                using TypedHttp;
                using System.Threading.Tasks;

                namespace X;

                [Client]
                public interface ICustomClient
                {
                    [Get("data")]
                    string {|#0:GetData|}();
                }
                """),
            DiagnosticResult.CompilerError("TYPEDHTTP008")
                            .WithLocation(0)
                            .WithArguments("ICustomClient.GetData()"));
    }

    [Fact]
    public async Task TYPEDHTTP010_ReportsNonPartialContainingType()
    {
        await TestDiagnostics(
            CSharp(
                """
                using TypedHttp;
                using System.Threading.Tasks;

                namespace X;

                public class {|#0:Outer|}
                {
                    [Client]
                    public interface ICustomClient
                    {
                        [Get("data")]
                        Task<string> GetData();
                    }
                }
                """),
            DiagnosticResult.CompilerError("TYPEDHTTP010")
                            .WithLocation(0)
                            .WithArguments("Outer"));
    }
}
