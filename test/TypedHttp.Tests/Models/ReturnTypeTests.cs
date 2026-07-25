using TypedHttp.Model;
using Xunit;

namespace TypedHttp.Tests.Models;

public class ReturnTypeTests
{
    [Fact]
    public void StringReturnType_StoresCorrectly()
    {
        var returnType = new StringReturnType("System.String");

        Assert.Equal("System.String", returnType.FullType);
        Assert.False(returnType.NeedsUndisposedResponse);
    }

    [Fact]
    public void CustomReturnType_StoresCorrectly()
    {
        var returnType = new CustomReturnType("MyNamespace.MyModel", "MyNamespace.MyModel");

        Assert.Equal("MyNamespace.MyModel", returnType.FullType);
        Assert.Equal("MyNamespace.MyModel", returnType.DeserializeType);
        Assert.False(returnType.NeedsUndisposedResponse);
    }

    [Fact]
    public void VoidReturnType_StoresCorrectly()
    {
        var returnType = new VoidReturnType("System.Threading.Tasks.Task");

        Assert.Equal("System.Threading.Tasks.Task", returnType.FullType);
        Assert.False(returnType.NeedsUndisposedResponse);
    }

    [Fact]
    public void HttpResponseMessageReturnType_NeedsUndisposedResponse()
    {
        var returnType = new HttpResponseMessageReturnType("System.Net.Http.HttpResponseMessage");

        Assert.Equal("System.Net.Http.HttpResponseMessage", returnType.FullType);
        Assert.True(returnType.NeedsUndisposedResponse);
    }

    [Fact]
    public void StreamReturnType_NeedsUndisposedResponse()
    {
        var returnType = new StreamReturnType("System.IO.Stream");

        Assert.Equal("System.IO.Stream", returnType.FullType);
        Assert.True(returnType.NeedsUndisposedResponse);
    }

    [Fact]
    public void ResponseReturnType_DoesNotNeedUndisposedResponse()
    {
        var returnType = new ResponseReturnType("TypedHttp.Response");

        Assert.Equal("TypedHttp.Response", returnType.FullType);
        Assert.False(returnType.NeedsUndisposedResponse);
    }

    [Fact]
    public void ResponseOfTReturnType_StoresCorrectly()
    {
        var returnType = new ResponseOfTReturnType("TypedHttp.Response<System.String>", "System.String");

        Assert.Equal("TypedHttp.Response<System.String>", returnType.FullType);
        Assert.Equal("System.String",                     returnType.DeserializeType);
        Assert.False(returnType.NeedsUndisposedResponse);
    }
}
