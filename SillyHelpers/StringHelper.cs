using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fonlow.Utilities
{
    public static class StringHelper
    {
        /// <summary>
        /// Reverse the words in a string, for example “cat and dog” becomes “tac dna god”.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown when text is null or empty.</exception>
        public static string ReverseWords(string text)
        {
            if (String.IsNullOrEmpty(text))
            {
                throw new ArgumentNullException("text", "Text cannot be null or empty.");
            }

            var builder = new StringBuilder(text.Length);
            var charArray = text.ToCharArray();
            int lowBoundInclusive = 0;
            int highBoundInclusive = 0;
            bool literalFound = false;
            for (int i = 0; i <= charArray.Length; i++)//inclusive for loop
            {
                bool endOfText = i == charArray.Length;
                if (endOfText || Char.IsWhiteSpace(charArray[i]))
                {
                    if (literalFound && (highBoundInclusive >= lowBoundInclusive))
                    {
                        if (endOfText)
                        {
                            for (int j = charArray.Length - 1; j >= lowBoundInclusive; j--)//inclusive for loop
                            {
                                builder.Append(charArray[j]);
                            }

                            break;
                        }
                        else
                        {
                            for (int j = highBoundInclusive; j >= lowBoundInclusive; j--)//inclusive for loop
                            {
                                builder.Append(charArray[j]);
                            }
                        }
                    }

                    if (endOfText)
                    {
                        break;
                    }

                    builder.Append(charArray[i]);

                    lowBoundInclusive = i + 1;
                    highBoundInclusive = i;
                    literalFound = false;
                }
                else
                {
                    highBoundInclusive = i;
                    literalFound = true;
                }
            }

            return builder.ToString();

        }
    }


}
