namespace Xhttp.Model;

/// <summary>
/// Represents a template string used for filling in headers and route parts.
/// </summary>
/// <param name="Kind">The kind of string it is</param>
/// <param name="Value">The value of the string</param>
internal readonly record struct Template(
    TemplateKind Kind,
    string       Value)
{
    public static Template String(string name)
        => new(TemplateKind.RawString, name);

    public static Template Parameter(string name)
        => new(TemplateKind.RawValue, name);

    public static Template Parse(string input)
    {
        // TODO: Implement an actual parser for interpolated strings
        if (input.Length == 0)
            return new Template(TemplateKind.RawValue, input);

        var openingBrace = input.IndexOf('{');
        var closingBrace = input.IndexOf('}');

        // If we start with { and end with }, without and }s in the middle (the,
        // index of the first } is the end of the string) we're a raw value.
        if (input.Length > 2
         && openingBrace == 0
         && closingBrace != -1
         && closingBrace == input.Length - 1)
        {
            // However, we can have format modifiers, so check for those
            if (input.IndexOf(':') != -1)
                return new Template(TemplateKind.Interpolation,
                                    input);

            return new Template(TemplateKind.RawValue,
                                input.Substring(1, input.Length - 2));
        }

        // If we have an opening brace and a closing brace that closes the
        // opening
        // one, then we're an interpolated string as we have string parts and
        // value parts
        if (openingBrace != -1
         && closingBrace != -1
         && openingBrace > closingBrace)
        {
            return new Template(TemplateKind.Interpolation,
                                input);
        }

        // If we don't have any interpolation holes, it's a raw string.
        return new Template(TemplateKind.RawString, input);
    }
}

/// <summary>
/// Defines what type of <see cref="Template"/> an instance is.
/// </summary>
internal enum TemplateKind
{
    /// <summary>
    /// Raw string with no values injected inside.
    /// </summary>
    RawString,

    /// <summary>
    /// Contains only a single value, no prefix or suffix.
    /// </summary>
    RawValue,

    /// <summary>
    /// Has either multiple values or a mix of string parts and value parts.
    /// </summary>
    Interpolation,
}
