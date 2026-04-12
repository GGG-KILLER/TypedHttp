using System;
using Xhttp.Model;
using Xunit;

namespace Xhttp.Tests.Models;

public class TemplateTests
{
    [Fact]
    public void Parse_StringOnly_ReturnsStringPart()
    {
        var input = "hello world";

        var template = Template.Parse(input);

        Assert.Single(template.Parts.Array);
        Assert.Equal(TemplatePartKind.String, template.Parts[0].Kind);
        Assert.Equal("hello world",           template.Parts[0].Value);
    }

    [Fact]
    public void Parse_ParameterOnly_ReturnsParameterPart()
    {
        var input = "{param}";

        var template = Template.Parse(input);

        Assert.Single(template.Parts.Array);
        Assert.Equal(TemplatePartKind.Parameter, template.Parts[0].Kind);
        Assert.Equal("param",                    template.Parts[0].Value);
    }

    [Fact]
    public void Parse_MixedStringAndParameter_ReturnsMultipleParts()
    {
        var input = "api/{id}/users";

        var template = Template.Parse(input);

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
        var input = "{{escaped}}";

        var template = Template.Parse(input);

        var str = Assert.Single(template.Parts.Array);
        Assert.Equal(TemplatePartKind.String, str.Kind);
        Assert.Equal("{{escaped}}",           str.Value);
    }

    [Fact]
    public void Parse_EscapedDoubleBrace_MixedWithParameter()
    {
        var input = "{{prefix}}{id}suffix";

        var template = Template.Parse(input);

        Assert.Equal(3,                          template.Parts.Count);
        Assert.Equal(TemplatePartKind.String,    template.Parts[0].Kind);
        Assert.Equal("{{prefix}}",               template.Parts[0].Value);
        Assert.Equal(TemplatePartKind.Parameter, template.Parts[1].Kind);
        Assert.Equal("id",                       template.Parts[1].Value);
        Assert.Equal(TemplatePartKind.String,    template.Parts[2].Kind);
        Assert.Equal("suffix",                   template.Parts[2].Value);
    }

    [Fact]
    public void Parse_TrailingOpenBrace_Throws()
    {
        Assert.Throws<FormatException>(() => Template.Parse("hello{"));
    }

    [Fact]
    public void Parse_UnclosedParameter_Throws()
    {
        Assert.Throws<FormatException>(() => Template.Parse("{unclosed"));
    }

    [Fact]
    public void Parse_UnclosedBrace_ThrowsWithCorrectMessage()
    {
        var ex = Assert.Throws<FormatException>(() => Template.Parse("{unclosed"));

        Assert.Equal("Unclosed { in template string.", ex.Message);
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
