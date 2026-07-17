using Tsu;

namespace TypedHttp.Model;

internal readonly record struct Header(Template Name, Template Value)
{
    public static Result<Header, HeaderError> Parse(string input)
    {
        var colon = input.IndexOf(':');
        if (colon == -1) return Result.Err<Header, HeaderError>(HeaderError.HeaderHasNoColon);

        var left  = input.AsSpan(0, colon).Trim();
        var right = input.AsSpan(colon + 1).Trim();

        var parsedLeft  = Template.Parse(left);
        var parsedRight = Template.Parse(right);

        if (parsedLeft.IsErr) return Result.Err<Header, HeaderError>(HeaderError.LeftHasUnclosedBrace);
        if (parsedRight.IsErr) return Result.Err<Header, HeaderError>(HeaderError.RightHasUnclosedBrace);

        return Result.Ok<Header, HeaderError>(new Header(parsedLeft.Ok.Value, parsedRight.Ok.Value));
    }
}

internal enum HeaderError
{
    HeaderHasNoColon, LeftHasUnclosedBrace, RightHasUnclosedBrace,
}
