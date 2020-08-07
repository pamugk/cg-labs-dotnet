using System;
using System.Collections.Generic;

namespace SolidOfRevolution
{
    /**
    * tbezier.cpp is an implementation of finite discrete point set smooth interpolation algorithms
    * based on cubic Bezier curves with control points calculated according to the tangents to the
    * angles of polygonal line that is built by the linear interpolation of the input points set.
    *
    * Two functions are provided: tbezierSO1 that builds the curve with smoothness order 1 and
    * tbezierSO0 that builds curve with smoothness order 0 and uses special heuristics to
    * reduce lengths of tangents and therefore reduce the difference with linear interpolation
    * making result curve look nicer.
    *
    * tbezierSO1 is recommended for scientific visualization as it uses strict math to balance
    * between smoothness and interpolation accuracy.
    * tbezierSO0 is recommended for advertising purposes as it produces nicer looking curves while
    * the accuracy is in common case lower.
    *
    * Read this for algorithm details: http://sv-journal.org/2017-1/04.php?lang=en
    *
    * Written by Konstantin Ryabinin under terms of MIT license.
    *
    * The MIT License (MIT)
    * Copyright (c) 2016 Konstantin Ryabinin
    *
    * Permission is hereby granted, free of charge, to any person obtaining a copy of this software
    * and associated documentation files (the "Software"), to deal in the Software without restriction,
    * including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
    * and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
    * subject to the following conditions:
    *
    * The above copyright notice and this permission notice shall be included in all copies or substantial
    * portions of the Software.
    *
    * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
    * LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
    * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
    * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
    * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
    */
    internal static class TBezier
    {
        /**
        * Amount of lines representing each Bezier segment.
        */
        public const int RESOLUTION = 32;

        /**
        * Paramenet affecting curvature, should be in [2; +inf).
        */
        public const double C = 2.0;

        /**
        * Threshold for zero.
        */
        private const double EPSILON = 1.0e-5;

        /**
        * Test if real value is zero.
        */
        private static bool IS_ZERO(double v) => (Math.Abs(v) < EPSILON);

        /**
        * Signum function.
        */
        private static int SIGN(double v) => v > EPSILON ? 1 : v < -EPSILON ? -1 : 0;

        /**
        * Build an interpolation curve with smoothness order 0 based on cubic Bezier according to given point set.
        *
        * @param values - input array of points to interpolate.
        * @param curve - output array of curve segments.
        * @return true if interpolation successful, false if not.
        */
        public static bool TBezierSO0(List<Point2D> values, out List<Segment> curve)
        {
            int n = values.Count - 1;
            curve = null;
            if (n < 2)
                return false;
            curve = new List<Segment>(n);
            Point2D cur, next, tgL, tgR = default(Point2D), deltaL, deltaC, deltaR;
            double l1, l2;
            next = values[1] - values[0];
            next.Normalize();
            for (int i = 0; i < n; ++i)
            {
                tgL = tgR;
                cur = next;
                deltaC = values[i + 1] - values[i];
                if (i > 0)
                    deltaL = Point2D.AbsMin(deltaC, values[i] - values[i - 1]);
                else
                    deltaL = deltaC;
                if (i < n - 1)
                {
                    next = values[i + 2] - values[i + 1];
                    next.Normalize();
                    if (IS_ZERO(cur.X) || IS_ZERO(cur.Y))
                        tgR = cur;
                    else if (IS_ZERO(next.X) || IS_ZERO(next.Y))
                        tgR = next;
                    else
                        tgR = cur + next;
                    tgR.Normalize();
                    deltaR = Point2D.AbsMin(deltaC, values[i + 2] - values[i + 1]);
                }
                else
                {
                    tgR = new Point2D();
                    deltaR = deltaC;
                }
                l1 = IS_ZERO(tgL.X) ? 0.0 : deltaL.X / (C * tgL.X);
                l2 = IS_ZERO(tgR.X) ? 0.0 : deltaR.X / (C * tgR.X);
                if (Math.Abs(l1 * tgL.Y) > Math.Abs(deltaL.Y))
                    l1 = IS_ZERO(tgL.Y) ? 0.0 : deltaL.Y / tgL.Y;
                if (Math.Abs(l2 * tgR.Y) > Math.Abs(deltaR.Y))
                    l2 = IS_ZERO(tgR.Y) ? 0.0 : deltaR.Y / tgR.Y;
                curve[i].Points[0] = values[i];
                curve[i].Points[1] = curve[i].Points[0] + tgL * l1;
                curve[i].Points[3] = values[i + 1];
                curve[i].Points[2] = curve[i].Points[3] - tgR * l2;
            }
            return true;
        }

        /**
        * Build an interpolation curve with smoothness order 1 based on cubic Bezier according to given point set.
        *
        * @param values - input array of points to interpolate.
        * @param curve - output array of curve segments.
        * @return true if interpolation successful, false if not.
        */
        public static bool TBezierSO1(List<Point2D> values, out List<Segment> curve)
        {
            int n = values.Count - 1;
            curve = null;
            if (n < 2)
                return false;
            curve = new List<Segment>(n);
            Point2D cur, next, tgL, tgR = default(Point2D), deltaC;
            double l1, l2, tmp, x;
            bool zL, zR;
            next = values[1] - values[0];
            next.Normalize();
            for (int i = 0; i < n; ++i)
            {
                tgL = tgR;
                cur = next;
                deltaC = values[i + 1] - values[i];
                if (i < n - 1)
                {
                    next = values[i + 2] - values[i + 1];
                    next.Normalize();
                    if (IS_ZERO(cur.X) || IS_ZERO(cur.Y))
                        tgR = cur;
                    else if (IS_ZERO(next.X) || IS_ZERO(next.Y))
                        tgR = next;
                    else
                        tgR = cur + next;
                    tgR.Normalize();
                }
                else
                    tgR = new Point2D();
                // There is actually a little mistake in the white paper (http://sv-journal.org/2017-1/04.php?lang=en):
                // algorithm described after figure 14 implicitly assumes that tangent vectors point inside the
                // A_i and B_i areas (see fig. 14). However in practice they can point outside as well. If so, tangents’
                // coordinates should be clamped to the border of A_i or B_i respectively to keep control points inside
                // the described area and thereby to avoid false extremes and loops on the curve.
                // The clamping is implemented by the next 4 if-statements.
                if (SIGN(tgL.X) != SIGN(deltaC.X))
                    tgL.X = 0.0;
                if (SIGN(tgL.Y) != SIGN(deltaC.Y))
                    tgL.Y = 0.0;
                if (SIGN(tgR.X) != SIGN(deltaC.X))
                    tgR.X = 0.0;
                if (SIGN(tgR.Y) != SIGN(deltaC.Y))
                    tgR.Y = 0.0;
                zL = IS_ZERO(tgL.X);
                zR = IS_ZERO(tgR.X);
                l1 = zL ? 0.0 : deltaC.X / (C * tgL.X);
                l2 = zR ? 0.0 : deltaC.X / (C * tgR.X);
                if (Math.Abs(l1 * tgL.Y) > Math.Abs(deltaC.Y))
                    l1 = IS_ZERO(tgL.Y) ? 0.0 : deltaC.Y / tgL.Y;
                if (Math.Abs(l2 * tgR.Y) > Math.Abs(deltaC.Y))
                    l2 = IS_ZERO(tgR.Y) ? 0.0 : deltaC.Y / tgR.Y;
                if (!zL && !zR)
                {
                    tmp = tgL.Y / tgL.X - tgR.Y / tgR.X;
                    if (!IS_ZERO(tmp))
                    {
                        x = (values[i + 1].Y - tgR.Y / tgR.X * values[i + 1].X - values[i].Y + tgL.Y / tgL.X * values[i].X) / tmp;
                        if (x > values[i].X && x < values[i + 1].X)
                        {
                            if (Math.Abs(l1) > Math.Abs(l2))
                                l1 = 0.0;
                            else
                                l2 = 0.0;
                        }
                    }
                }
                curve[i].Points[0] = values[i];
                curve[i].Points[1] = curve[i].Points[0] + tgL * l1;
                curve[i].Points[3] = values[i + 1];
                curve[i].Points[2] = curve[i].Points[3] - tgR * l2;
            }
            return true;
        }
    }
}