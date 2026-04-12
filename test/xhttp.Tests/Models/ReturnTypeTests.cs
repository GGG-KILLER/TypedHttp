using Xhttp.Model;
using Xunit;

namespace Xhttp.Tests.Models;

public class ReturnTypeTests
{
    [Fact]
    public void StringReturnType_StoresCorrectly()
    {
        var returnType = new ReturnType(ReturnTypeKind.String, "System.String", null);

        Assert.Equal(ReturnTypeKind.String, returnType.Kind);
        Assert.Equal("System.String", returnType.Type);
        Assert.Null(returnType.InnerType);
    }

    [Fact]
    public void TaskOfTReturnType_StoresCorrectly()
    {
        var returnType = new ReturnType(ReturnTypeKind.Custom, "System.Threading.Tasks.Task`1[System.String]", "System.String");

        Assert.Equal(ReturnTypeKind.Custom, returnType.Kind);
        Assert.Equal("System.Threading.Tasks.Task`1[System.String]", returnType.Type);
        Assert.Equal("System.String", returnType.InnerType);
    }

    [Fact]
    public void VoidReturnType_StoresCorrectly()
    {
        var returnType = new ReturnType(ReturnTypeKind.Void, "System.Threading.Tasks.Task", null);

        Assert.Equal(ReturnTypeKind.Void, returnType.Kind);
        Assert.Null(returnType.InnerType);
    }

    [Fact]
    public void HttpResponseMessageReturnType_StoresCorrectly()
    {
        var returnType = new ReturnType(ReturnTypeKind.HttpResponseMessage, "System.Net.Http.HttpResponseMessage", null);

        Assert.Equal(ReturnTypeKind.HttpResponseMessage, returnType.Kind);
        Assert.Null(returnType.InnerType);
    }

    [Fact]
    public void StreamReturnType_StoresCorrectly()
    {
        var returnType = new ReturnType(ReturnTypeKind.Stream, "System.IO.Stream", null);

        Assert.Equal(ReturnTypeKind.Stream, returnType.Kind);
        Assert.Null(returnType.InnerType);
    }
}
