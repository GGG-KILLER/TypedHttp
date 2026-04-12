using Xhttp.Model;
using Xunit;

namespace Xhttp.Tests.Models;

public class ParameterTests
{
    [Fact]
    public void IsRoute_ReturnsTrueForRouteKinds()
    {
        var stringRoute = new Parameter(false, "string", "param", ParameterKind.StringRoute);
        var nonStringRoute = new Parameter(false, "int", "param", ParameterKind.NonStringRoute);

        Assert.True(stringRoute.IsRoute);
        Assert.True(nonStringRoute.IsRoute);
    }

    [Fact]
    public void IsRoute_ReturnsFalseForNonRouteKinds()
    {
        var query = new Parameter(false, "string", "param", ParameterKind.StringQuery);
        var body = new Parameter(false, "object", "param", ParameterKind.JsonBody);

        Assert.False(query.IsRoute);
        Assert.False(body.IsRoute);
    }

    [Fact]
    public void IsQuery_ReturnsTrueForQueryKinds()
    {
        var stringQuery = new Parameter(false, "string", "param", ParameterKind.StringQuery);
        var nonStringQuery = new Parameter(false, "int", "param", ParameterKind.NonStringQuery);

        Assert.True(stringQuery.IsQuery);
        Assert.True(nonStringQuery.IsQuery);
    }

    [Fact]
    public void IsProperty_ReturnsTrueForPropertyKinds()
    {
        var stringProperty = new Parameter(false, "string", "param", ParameterKind.StringProperty);
        var nonStringProperty = new Parameter(false, "int", "param", ParameterKind.NonStringProperty);

        Assert.True(stringProperty.IsProperty);
        Assert.True(nonStringProperty.IsProperty);
    }

    [Fact]
    public void IsBody_ReturnsTrueForBodyKinds()
    {
        var jsonBody = new Parameter(false, "object", "param", ParameterKind.JsonBody);
        var stringBody = new Parameter(false, "string", "param", ParameterKind.StringBody);
        var streamBody = new Parameter(false, "Stream", "param", ParameterKind.StreamBody);
        var httpContentBody = new Parameter(false, "HttpContent", "param", ParameterKind.HttpContentBody);

        Assert.True(jsonBody.IsBody);
        Assert.True(stringBody.IsBody);
        Assert.True(streamBody.IsBody);
        Assert.True(httpContentBody.IsBody);
    }
}
