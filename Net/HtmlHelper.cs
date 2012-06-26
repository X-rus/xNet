using System.Text;
using System.Text.RegularExpressions;

namespace xNet.Net
{
    /// <summary>
    /// Представляет статический класс, предназначенный для помощи в работе с HTML.
    /// </summary>
    public static class HtmlHelper
    {
        /// <summary>
        /// Заменяет в тексте HTML-сущности на представляющие их символы.
        /// </summary>
        /// <param name="text">Строка, в которой будет произведена замена.</param>
        /// <returns>Строка с заменёнными HTML-сущностями.</returns>
        /// <remarks>Заменяются только следующие мнемоники: quot, amp, lt и gt. И все виды кодов.</remarks>
        public static string ReplaceEntities(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var strBuilder = new StringBuilder(text);

            #region Замена мнемоник

            int offset = 0;
            var regex = new Regex(@"\&(?<text>\w{1,4})\;", RegexOptions.Compiled);

            foreach (Match match in regex.Matches(text))
            {
                char c;

                #region Получение символа, который представляет HTML-сущность

                switch (match.Groups["text"].Value)
                {
                    case "quot":
                        c = '"';
                        break;

                    case "amp":
                        c = '&';
                        break;

                    case "lt":
                        c = '<';
                        break;

                    case "gt":
                        c = '>';
                        break;

                    default:
                        continue;
                }

                #endregion

                ReplaceEntity(match, strBuilder, c, ref offset);
            }

            #endregion

            bool strHasChanged = false;

            // Сохраняем строку, на случай, если она не будет изменена. Чтобы не вызывать два раза ToString()
            string newStr = strBuilder.ToString();

            #region Замена кодов

            offset = 0;
            regex = new Regex(@"\&#(?<code>\w{1,4})\;", RegexOptions.Compiled);

            MatchCollection matches = regex.Matches(newStr);

            if (matches.Count != 0)
            {
                strHasChanged = true;

                foreach (Match match in matches)
                {
                    int code;

                    if (int.TryParse(match.Groups["code"].Value, out code))
                    {
                        ReplaceEntity(match, strBuilder, (char)code, ref offset);
                    }
                }
            }

            #endregion

            if (strHasChanged)
            {
                return strBuilder.ToString();
            }
            else
            {
                return newStr;
            }
        }


        private static void ReplaceEntity(Match match,
            StringBuilder strBuilder, char value, ref int offset)
        {
            // Стираем HTML-сущность.
            strBuilder.Remove(match.Index - offset, match.Length);

            // Вставляем символ, который представляет HTML-сущность.
            strBuilder.Insert(match.Index - offset, value);

            // Так как размер строки изменился, сохраняем смещение.
            offset += match.Length - 1;
        }
    }
}