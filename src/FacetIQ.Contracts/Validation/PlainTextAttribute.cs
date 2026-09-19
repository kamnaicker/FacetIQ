using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;

namespace FacetIQ.Contracts.Validation;

/// <summary>
/// Single-line text without control or invisible formatting characters. Bidi controls can make a
/// value display as something else, and a zero-width space makes two equal-looking words differ.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class PlainTextAttribute : ValidationAttribute
{
    // Zero-width non-joiner and joiner. Persian, Indic scripts and emoji need them.
    private const int NonJoiner = 0x200C;
    private const int Joiner = 0x200D;

    public PlainTextAttribute()
        : base("Contains a hidden or control character.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is not string text)
        {
            return true;
        }

        foreach (var rune in text.EnumerateRunes())
        {
            if (IsHidden(rune))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsHidden(Rune rune)
    {
        if (rune.Value is NonJoiner or Joiner)
        {
            return false;
        }

        return Rune.GetUnicodeCategory(rune) is
            UnicodeCategory.Control or
            UnicodeCategory.Format or
            UnicodeCategory.LineSeparator or
            UnicodeCategory.ParagraphSeparator;
    }
}
