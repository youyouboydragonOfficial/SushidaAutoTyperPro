using System.Collections.Generic;
using System.Text;

namespace SushidaAutoTyper.Services
{
    public class RomajiConverter
    {
        private static readonly Dictionary<string, string> StandardRomajiMap = new Dictionary<string, string>
        {
            { "あ", "a" }, { "い", "i" }, { "う", "u" }, { "え", "e" }, { "お", "o" },
            { "か", "ka" }, { "き", "ki" }, { "く", "ku" }, { "け", "ke" }, { "こ", "ko" },
            { "さ", "sa" }, { "し", "shi" }, { "す", "su" }, { "せ", "se" }, { "そ", "so" },
            { "た", "ta" }, { "ち", "chi" }, { "つ", "tsu" }, { "て", "te" }, { "と", "to" },
            { "な", "na" }, { "に", "ni" }, { "ぬ", "nu" }, { "ね", "ne" }, { "の", "no" },
            { "は", "ha" }, { "ひ", "hi" }, { "ふ", "fu" }, { "へ", "he" }, { "ほ", "ho" },
            { "ま", "ma" }, { "み", "mi" }, { "む", "mu" }, { "め", "me" }, { "も", "mo" },
            { "や", "ya" }, { "ゆ", "yu" }, { "よ", "yo" },
            { "ら", "ra" }, { "り", "ri" }, { "る", "ru" }, { "れ", "re" }, { "ろ", "ro" },
            { "わ", "wa" }, { "を", "wo" }, { "ん", "nn" },
            { "が", "ga" }, { "ぎ", "gi" }, { "ぐ", "gu" }, { "げ", "ge" }, { "ご", "go" },
            { "ざ", "za" }, { "じ", "ji" }, { "ず", "zu" }, { "ぜ", "ze" }, { "ぞ", "zo" },
            { "だ", "da" }, { "ぢ", "di" }, { "づ", "du" }, { "で", "de" }, { "ど", "do" },
            { "ば", "ba" }, { "び", "bi" }, { "ぶ", "bu" }, { "べ", "be" }, { "ぼ", "bo" },
            { "ぱ", "pa" }, { "ぴ", "pi" }, { "ぷ", "pu" }, { "ぺ", "pe" }, { "ぽ", "po" },
            { "きゃ", "kya" }, { "きゅ", "kyu" }, { "きょ", "kyo" },
            { "しゃ", "sha" }, { "しゅ", "shu" }, { "しょ", "sho" },
            { "ちゃ", "cha" }, { "ちゅ", "chu" }, { "ちょ", "cho" },
            { "にゃ", "nya" }, { "にゅ", "nyu" }, { "にょ", "nyo" },
            { "ひゃ", "hya" }, { "ひゅ", "hyu" }, { "ひょ", "hyo" },
            { "みゃ", "mya" }, { "みゅ", "myu" }, { "みょ", "myo" },
            { "りゃ", "rya" }, { "りゅ", "ryu" }, { "りょ", "ryo" },
            { "ぎゃ", "gya" }, { "ぎゅ", "gyu" }, { "ぎょ", "gyo" },
            { "じゃ", "ja" }, { "じゅ", "ju" }, { "じょ", "jo" },
            { "びゃ", "bya" }, { "びゅ", "byu" }, { "びょ", "byo" },
            { "ぴゃ", "pya" }, { "ぴゅ", "pyu" }, { "ぴょ", "pyo" },
            { "ー", "-" }
        };

        /// <summary>
        /// Converts Japanese Hiragana string into standard Romaji typing sequence.
        /// </summary>
        public static string ConvertToRomaji(string kanaText)
        {
            if (string.IsNullOrEmpty(kanaText)) return string.Empty;

            StringBuilder sb = new StringBuilder();
            int i = 0;
            while (i < kanaText.Length)
            {
                // Try 2-char combinations first (e.g., きゃ, しゃ)
                if (i + 1 < kanaText.Length)
                {
                    string pair = kanaText.Substring(i, 2);
                    if (StandardRomajiMap.TryGetValue(pair, out string? romajiPair))
                    {
                        sb.Append(romajiPair);
                        i += 2;
                        continue;
                    }
                }

                // Sokuon っ
                if (kanaText[i] == 'っ' && i + 1 < kanaText.Length)
                {
                    string nextChar = kanaText.Substring(i + 1, 1);
                    if (StandardRomajiMap.TryGetValue(nextChar, out string? nextRomaji) && nextRomaji.Length > 0)
                    {
                        sb.Append(nextRomaji[0]);
                        i++;
                        continue;
                    }
                }

                // Single char
                string single = kanaText.Substring(i, 1);
                if (StandardRomajiMap.TryGetValue(single, out string? romajiSingle))
                {
                    sb.Append(romajiSingle);
                }
                else
                {
                    sb.Append(single); // Keep as is if ASCII or unknown
                }
                i++;
            }

            return sb.ToString();
        }
    }
}
