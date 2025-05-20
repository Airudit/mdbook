
namespace Airudit.MdBook.Core.Internals
{
    using System.Text.RegularExpressions;

    public static class MyExtensions
    {
        private static readonly Regex invalidRelativePathRegex = new Regex(@"[:\0\r\n\t\p{M}\p{Cc}\p{Co}\p{Cf}]", RegexOptions.IgnoreCase);
        private static readonly string[] yesValues = new string[] { "yes", "ok", "1", "oui", "ja", "da", "confirm", "yep", "y", "on", };
        private static readonly string[] noValues = new string[] { "no", "non", "0", "nein", "niet", "never", "n", "off", };

        public static bool IsInvalidFileRelativePath(string path)
        {
            return invalidRelativePathRegex.IsMatch(path);
        }

        /// <summary>
        /// Parses many forms of boolean value.
        /// </summary>
        /// <param name="value">the value to parse</param>
        /// <param name="result">the resulting boolean</param>
        /// <returns>operation success</returns>
        public static bool TryParseBoolean(string value, out bool result)
        {
            // TODO: write unit tests for this method
            if (string.IsNullOrEmpty(value))
            {
                result = default(bool);
                return false;
            }
            else
            {
                value = value.Trim().ToLowerInvariant();
                if (bool.TryParse(value, out result))
                {
                    return true;
                }
                else if (yesValues.Contains(value))
                {
                    result = true;
                    return true;
                }
                else if (noValues.Contains(value))
                {
                    result = false;
                    return true;
                }
                else
                {
                    result = default(bool);
                    return false;
                }
            }
        }
    }
}
