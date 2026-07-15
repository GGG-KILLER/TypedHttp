using TypedHttp.Model;
using Xunit;

namespace TypedHttp.Tests.Models;

public class ReturnTypeTests
{
    [Fact]
    public void StringReturnType_StoresCorrectly()
    {
        var returnType = new ReturnType(ReturnTypeKind.String, "System.String", null, null);

        Assert.Equal(ReturnTypeKind.String, returnType.Kind);
        Assert.Equal("System.String",       returnType.Type);
        Assert.Null(returnType.InnerType);
    }

    [Fact]
    public void TaskOfTReturnType_StoresCorrectly()
    {
        var returnType = new ReturnType(
            ReturnTypeKind.Custom,
            "System.Threading.Tasks.Task`1[System.String]",
            "System.String",
            null);

        Assert.Equal(ReturnTypeKind.Custom,                          returnType.Kind);
        Assert.Equal("System.Threading.Tasks.Task`1[System.String]", returnType.Type);
        Assert.Equal("System.String",                                returnType.InnerType);
    }

    [Fact]
    public void VoidReturnType_StoresCorrectly()
    {
        var returnType = new ReturnType(ReturnTypeKind.Void, "System.Threading.Tasks.Task", null, null);

        Assert.Equal(ReturnTypeKind.Void, returnType.Kind);
        Assert.Null(returnType.InnerType);
    }

    [Fact]
    public void HttpResponseMessageReturnType_StoresCorrectly()
    {
        var returnType =
            new ReturnType(ReturnTypeKind.HttpResponseMessage, "System.Net.Http.HttpResponseMessage", null, null);

        Assert.Equal(ReturnTypeKind.HttpResponseMessage, returnType.Kind);
        Assert.Null(returnType.InnerType);
    }

    [Fact]
    public void StreamReturnType_StoresCorrectly()
    {
        var returnType = new ReturnType(ReturnTypeKind.Stream, "System.IO.Stream", null, null);

        Assert.Equal(ReturnTypeKind.Stream, returnType.Kind);
        Assert.Null(returnType.InnerType);
    }

    [Theory]
    [InlineData((int) ReturnTypeKind.HttpResponseMessage, true)]
    [InlineData((int) ReturnTypeKind.Stream,              true)]
    [InlineData((int) ReturnTypeKind.String,              false)]
    [InlineData((int) ReturnTypeKind.Void,                false)]
    [InlineData((int) ReturnTypeKind.Custom,              false)]
    public void NeedsUndisposedResponse_MatchesExpected(int kindInt, bool expected)
    {
        var returnType = new ReturnType((ReturnTypeKind) kindInt, "T", null, null);

        Assert.Equal(expected, returnType.NeedsUndisposedResponse);
    }
}
