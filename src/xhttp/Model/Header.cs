namespace Xhttp.Model;

internal readonly struct Header(Template Name, Template Value)
{
    public static Header Parse(string input)
    {
        var colon = input.IndexOf(':');
        if (colon == -1)
            throw new FormatException(
                "Header has no colon."); // TODO: Diagnostic

        var left  = input.AsSpan(0, colon).Trim();
        var right = input.AsSpan(colon + 1).Trim();

        return new Header(Template.Parse(left), Template.Parse(right));
    }
}
