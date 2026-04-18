using System;
using TypedHttp.Model;
using Xunit;

namespace TypedHttp.Tests.Models;

public class HeaderTests
{
    [Fact]
    public void Parse_ValidHeader_ReturnsHeaderWithNameAndValue()
    {
        const string input = "Authorization: Bearer {token}";

        var header = Header.Parse(input);

        var name = Assert.Single(header.Name.Parts);
        Assert.Equal("Authorization",            name.Value);
        Assert.Equal(2,                          header.Value.Parts.Count);
        Assert.Equal(TemplatePartKind.String,    header.Value.Parts[0].Kind);
        Assert.Equal("Bearer ",                  header.Value.Parts[0].Value);
        Assert.Equal(TemplatePartKind.Parameter, header.Value.Parts[1].Kind);
        Assert.Equal("token",                    header.Value.Parts[1].Value);
    }

    [Fact]
    public void Parse_HeaderWithoutColon_ThrowsFormatException()
    {
        const string input = "InvalidHeader";

        Assert.Throws<FormatException>(() => Header.Parse(input));
    }

    [Fact]
    public void Parse_HeaderWithOnlyName_ReturnsEmptyValue()
    {
        const string input = "Content-Type:";

        var header = Header.Parse(input);

        var name = Assert.Single(header.Name.Parts.Array);
        Assert.Equal("Content-Type", name.Value);
        Assert.Empty(header.Value.Parts.Array);
    }
}
