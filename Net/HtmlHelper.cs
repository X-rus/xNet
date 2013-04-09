using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace xNet.Net
{
    /// <summary>
    /// Представляет статический класс, предназначенный для помощи в работе с HTML.
    /// </summary>
    public static class HtmlHelper
    {
        #region _htmlMnemonics

        private static readonly Dictionary<string, string> _htmlMnemonics = new Dictionary<string, string>()
        {
            { "apos", "'" },
            { "quot", "\"" },
            { "amp", "&" },
            { "lt", "<" },
            { "gt", ">" }
        };

        #endregion


        #region Статические методы (открытые)

        /// <summary>
        /// Заменяет в строке HTML-сущности на представляющие их символы.
        /// </summary>
        /// <param name="str">Строка, в которой будет произведена замена.</param>
        /// <returns>Строка с заменёнными HTML-сущностями.</returns>
        /// <remarks>Заменяются только следующие мнемоники: apos, quot, amp, lt и gt. И все виды кодов.</remarks>
        public static string ReplaceEntities(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            var regex = new Regex(@"(\&(?<text>\w{1,4})\;)|(\&#(?<code>\w{1,4})\;)", RegexOptions.Compiled);

            string result = regex.Replace(str, match =>
            {
                if (match.Groups["text"].Success)
                {
                    string value;

                    if (_htmlMnemonics.TryGetValue(match.Groups["text"].Value, out value))
                    {
                        return value;
                    }
                }
                else if (match.Groups["code"].Success)
                {
                    int code = int.Parse(match.Groups["code"].Value);
                    return ((char)code).ToString();
                }

                return match.Value;
            });

            return result;
        }

        /// <summary>
        /// Заменяет в строке Unicode-сущности на представляющие их символы.
        /// </summary>
        /// <param name="str">Строка, в которой будет произведена замена.</param>
        /// <returns>Строка с заменёнными Unicode-сущностями.</returns>
        /// <remarks>Unicode-сущности имеют вид: \u2320 или \U044F</remarks>
        public static string ReplaceUnicode(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            var regex = new Regex(@"\\[uU](?<text>[0-9A-F]{4})", RegexOptions.Compiled);

            string result = regex.Replace(str, match =>
            {
                int code = int.Parse(match.Groups["code"].Value, NumberStyles.HexNumber);

                return ((char)code).ToString();
            });

            return result;
        }

        #endregion
    }
}