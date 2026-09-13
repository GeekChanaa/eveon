using System.Text.RegularExpressions;

namespace VoltaXApi.Helpers;

/// <summary>
/// Moroccan phone numbers reach us in several shapes ("0610610614", "+212610610614",
/// "212 610 610 614"). They are stored and compared in the "+212XXXXXXXXX" form only.
/// </summary>
public static class PhoneHelper
{
  // Digits with optional separators and an optional leading "+". An email address can
  // never match, so this is enough to tell the two apart on the login form.
  private static readonly Regex PhoneShape =
    new(@"^\+?[0-9][0-9\s\-\.\(\)]{6,19}$", RegexOptions.Compiled);

  public static bool LooksLikePhoneNumber(string? value)
  {
    return !string.IsNullOrWhiteSpace(value) && PhoneShape.IsMatch(value.Trim());
  }

  /// <summary>
  /// Turns any accepted shape into "+212XXXXXXXXX". Returns null for an empty input.
  /// </summary>
  public static string? Normalize(string? phone)
  {
    if (string.IsNullOrWhiteSpace(phone)) return null;

    string digits = new string(phone.Where(char.IsDigit).ToArray());

    // International prefixes, then the local trunk zero.
    if (digits.StartsWith("00212"))
      digits = digits.Substring(5);
    else if (digits.StartsWith("212"))
      digits = digits.Substring(3);
    else if (digits.StartsWith("0"))
      digits = digits.Substring(1);

    return $"+212{digits}";
  }
}
