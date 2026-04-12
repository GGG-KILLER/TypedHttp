using System;
using Xhttp.Model;
using Xunit;

namespace Xhttp.Tests.Models;

public class HeaderTests
{
    [Fact]
    public void Parse_ValidHeader_ReturnsHeaderWithNameAndValue()
    {
        var input = "Authorization: Bearer {token}";

        var header = Header.Parse(input);

        Assert.Equal(1,                          header.Name.Parts.Count);
        Assert.Equal("Authorization",            header.Name.Parts[0].Value);
        Assert.Equal(2,                          header.Value.Parts.Count);
        Assert.Equal(TemplatePartKind.String,    header.Value.Parts[0].Kind);
        Assert.Equal("Bearer ",                  header.Value.Parts[0].Value);
        Assert.Equal(TemplatePartKind.Parameter, header.Value.Parts[1].Kind);
        Assert.Equal("token",                    header.Value.Parts[1].Value);
    }

    [Fact]
    public void Parse_HeaderWithoutColon_ThrowsFormatException()
    {
        var input = "InvalidHeader";

        Assert.Throws<FormatException>(() => Header.Parse(input));
    }

    [Fact]
    public void Parse_HeaderWithOnlyName_ReturnsEmptyValue()
    {
        var input = "Content-Type:";

        var header = Header.Parse(input);

        Assert.Equal(1,              header.Name.Parts.Count);
        Assert.Equal("Content-Type", header.Name.Parts[0].Value);
        Assert.Empty(header.Value.Parts.Array);
    }
}
