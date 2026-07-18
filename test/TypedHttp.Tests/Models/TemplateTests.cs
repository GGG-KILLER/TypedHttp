using TypedHttp.Model;
using Xunit;

namespace TypedHttp.Tests.Models;

public class TemplateTests
{
    [Fact]
    public void Parse_StringOnly_ReturnsStringPart()
    {
        var result = Template.Parse("hello world");

        Assert.True(result.IsOk);
        var template = result.Ok.Value;
        Assert.Single(template.Parts.Array);
        Assert.Equal(TemplatePartKind.String, template.Parts[0].Kind);
        Assert.Equal("hello world",           template.Parts[0].Value);
    }

    [Fact]
    public void Parse_ParameterOnly_ReturnsParameterPart()
    {
        var result = Template.Parse("{param}");

        Assert.True(result.IsOk);
        var template = result.Ok.Value;
        Assert.Single(template.Parts.Array);
        Assert.Equal(TemplatePartKind.Parameter, template.Parts[0].Kind);
        Assert.Equal("param",                    template.Parts[0].Value);
    }

    [Fact]
    public void Parse_MixedStringAndParameter_ReturnsMultipleParts()
    {
        var result = Template.Parse("api/{id}/users");

        Assert.True(result.IsOk);
        var template = result.Ok.Value;
        Assert.Equal(3,                          template.Parts.Count);
        Assert.Equal(TemplatePartKind.String,    template.Parts[0].Kind);
        Assert.Equal("api/",                     template.Parts[0].Value);
        Assert.Equal(TemplatePartKind.Parameter, template.Parts[1].Kind);
        Assert.Equal("id",                       template.Parts[1].Value);
        Assert.Equal(TemplatePartKind.String,    template.Parts[2].Kind);
        Assert.Equal("/users",                   template.Parts[2].Value);
    }

    [Fact]
    public void Parse_EscapedDoubleBrace_AppendsLiteralDoubleBrace()
    {
        // {{ is kept verbatim so it can be used in C# interpolated strings (where {{ → {)
        var result = Template.Parse("{{escaped}}");

        Assert.True(result.IsOk);
        var template = result.Ok.Value;
        var str      = Assert.Single(template.Parts.Array);
        Assert.Equal(TemplatePartKind.String, str.Kind);
        Assert.Equal("{{escaped}}",           str.Value);
    }

    [Fact]
    public void Parse_EscapedDoubleBrace_MixedWithParameter()
    {
        var result = Template.Parse("{{prefix}}{id}suffix");

        Assert.True(result.IsOk);
        var template = result.Ok.Value;
        Assert.Equal(3,                          template.Parts.Count);
        Assert.Equal(TemplatePartKind.String,    template.Parts[0].Kind);
        Assert.Equal("{{prefix}}",               template.Parts[0].Value);
        Assert.Equal(TemplatePartKind.Parameter, template.Parts[1].Kind);
        Assert.Equal("id",                       template.Parts[1].Value);
        Assert.Equal(TemplatePartKind.String,    template.Parts[2].Kind);
        Assert.Equal("suffix",                   template.Parts[2].Value);
    }

    [Fact]
    public void Parse_TrailingOpenBrace_ReturnsError()
    {
        var result = Template.Parse("hello{");

        Assert.True(result.IsErr);
        Assert.Equal(TemplateError.UnclosedBrace, result.Err.Value);
    }

    [Fact]
    public void Parse_UnclosedParameter_ReturnsError()
    {
        var result = Template.Parse("{unclosed");

        Assert.True(result.IsErr);
        Assert.Equal(TemplateError.UnclosedBrace, result.Err.Value);
    }

    [Fact]
    public void String_CreatesStringTemplate()
    {
        var template = Template.String("test");

        Assert.Single(template.Parts.Array);
        Assert.Equal(TemplatePartKind.String, template.Parts[0].Kind);
        Assert.Equal("test",                  template.Parts[0].Value);
    }

    [Fact]
    public void Parameter_CreatesParameterTemplate()
    {
        var template = Template.Parameter("name");

        Assert.Single(template.Parts.Array);
        Assert.Equal(TemplatePartKind.Parameter, template.Parts[0].Kind);
        Assert.Equal("name",                     template.Parts[0].Value);
    }
}
