using System.Collections.Immutable;
using System.Text;

namespace Xhttp.Model;

/// <summary>
/// Represents a template string used for filling in headers and route parts.
/// </summary>
internal readonly record struct Template(ImmutableByValArray<TemplatePart> Parts)
{
    public static Template String(string value) => new([ new TemplatePart(TemplatePartKind.String, value) ]);

    public static Template Parameter(string name) => new([ new TemplatePart(TemplatePartKind.Parameter, name) ]);

    public static Template Parse(ReadOnlySpan<char> input)
    {
        var parts        = ImmutableArray.CreateBuilder<TemplatePart>();
        var currentKind  = TemplatePartKind.String;
        var currentValue = new StringBuilder();

        for (var index = 0; index < input.Length; index++)
        {
            var ch = input[index];
            switch (currentKind)
            {
                // If we're in a string part
                case TemplatePartKind.String
                    // and the next character opens an interpolation hole
                    when ch == '{'
                         // and it is not escaping the next character by being a
                         // repeated {
                      && index            < input.Length - 1
                      && input[index + 1] != '{':
                    // Then we end the current part and swap the interpolation kind

                    if (currentValue.Length > 0) parts.Add(new TemplatePart(currentKind, currentValue.ToString()));

                    currentKind = ~currentKind;
                    currentValue.Clear();
                    break;

                // Same logic as above but simpler since we don't accept any
                // escapes for parameter holes.
                case TemplatePartKind.Parameter when ch == '}':
                    parts.Add(new TemplatePart(currentKind, currentValue.ToString()));

                    currentKind = ~currentKind;
                    currentValue.Clear();
                    break;

                // Special case }} for strings, read as a single }
                case TemplatePartKind.String
                    // If we're in a string and the next character closes an interpolation hole
                    when ch == '}'
                         // but it is an escaped }
                      && index            < input.Length - 1
                      && input[index + 1] == '}':
                    index++;      // Skip the 2nd } by incrementing past it
                    goto default; // And append it to the current part

                // Otherwise just append to the current part contents.
                default: currentValue.Append(ch); break;
            }
        }

        // If we end on a parameter kind, it means there wasn't a closing }
        if (currentKind == TemplatePartKind.Parameter)
            throw new FormatException("Unclosed parameter hole in template string.");

        // Submit the last part at the end of the input.
        if (currentValue.Length > 0) parts.Add(new TemplatePart(currentKind, currentValue.ToString()));

        return new Template(new ImmutableByValArray<TemplatePart>(parts.DrainToImmutable()));
    }
}

/// <summary>
/// Represents a part of a template string.
/// </summary>
/// <param name="Kind">The kind of part this template part is.</param>
/// <param name="Value">The raw value of this template part.</param>
internal readonly record struct TemplatePart(
    TemplatePartKind Kind,
    string           Value);

/// <summary>
/// Defines what type of <see cref="Template"/> an instance is.
/// </summary>
internal enum TemplatePartKind
{
    /// <summary>
    /// Plain string, needs to be escaped before being inserted into a C# string
    /// literal.
    /// </summary>
    String = 0,

    /// <summary>
    /// A parameter that will be interpolated into the string.
    /// </summary>
    Parameter = ~String,
}
