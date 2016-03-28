using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fonlow.Utilities
{

    public enum TriangleType { Error, Scalene, Isosceles, Equilateral };

    public static class TriangleHelper
    {
        /// <summary>
        /// Receives three integer inputs for the lengths of the sides of a triangle and returns one of four values to determine the triangle type.
        /// </summary>
        public static TriangleType GetTriangleType(int side1, int side2, int side3)
        {
            if (side1 <= 0 || side2 <= 0 || side3 <= 0)
            {
                return TriangleType.Error;
            }

            long s1 = side1;
            long s2 = side2;
            long s3 = side3;

            if ((s1 + s2 <= s3) || (s1 + s3 <= s2) || (s2 + s3 <= s1))
            {
                return TriangleType.Error;
            }

            bool e1 = s1 == s2;
            bool e2 = s1 == s3;

            if (e1 && e2)
            {
                return TriangleType.Equilateral;
            }

            bool e3 = s2 == s3;

            if (e1 || e2 || e3)
            {
                return TriangleType.Isosceles;
            }

            return TriangleType.Scalene;
        }
    }

}
