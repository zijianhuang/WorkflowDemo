using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fonlow.Utilities
{
    public static class Functions
    {
        public static int GetClosestTo1000(int a, int b)
        {
            const int target=1000;
            return Math.Abs(target - a) < Math.Abs(target - b) ? a : b;
        }

        public static string DoubleCharacters(string s)
        {
            if (s==null)
            {
                throw new ArgumentNullException("s");
            }
            
            StringBuilder builder = new StringBuilder();
            foreach (var item in s.ToCharArray())
            {
                builder.Append(item);
                builder.Append(item);
            }
            return builder.ToString();
        }

        public static bool EquallySplitable(int[] numbers)
        {
            if (numbers==null)
            {
                throw new ArgumentNullException("numbers");
            }

            if (numbers.Length < 2)
                return false;

            long sum = numbers.Sum(d=>(long)d);
            long half = sum / 2;

            if (half*2!=sum)
                return false;

            long left = 0;
            for (int i = 0; i < numbers.Length; i++)
            {
                left += numbers[i];
                if (left == half)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// To implement String.Join("*", s.ToCharArray()) with recursion
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string AddAsteriskBetweenCharactersRecursively(string s)
        {
            if (String.IsNullOrEmpty(s))
                return s;

            if (s.Length == 1)
                return s;
            return s.Substring(0, 1)+ "*" + AddAsteriskBetweenCharactersRecursively(s.Substring(1));
        }
    }
}
