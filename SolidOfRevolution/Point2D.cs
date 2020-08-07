using System;

namespace SolidOfRevolution
{
    internal struct Point2D
    {
        private static bool IsZero(double v) => (Math.Abs(v) < 1.0e-5);
        /**
        * Point coordinates.
        */
        public double X { get;set; }
        public double Y { get;set; }

        /**
        * Point2D constructor.
        *
        * @param x - x coordinate of the point.
        * @param y - y coordinate of the point.
        */
        public Point2D(double x, double y)
        {
            X = x; Y = y;
        }

        /**
        * Add other point to the current one.
        *
        * @param p - point to add.
        * @return summ of the current point and the given one.
        */
        public static Point2D operator +(Point2D p1, Point2D p2)
            => new Point2D(p1.X + p2.X, p1.Y + p2.Y);
        
        /**
        * Subtract other point from the current one.
        *
        * @param p - point to subtract.
        * @return difference of the current point and the given one.
        */
        public static Point2D operator -(Point2D p1, Point2D p2)
            => new Point2D(p1.X - p2.X, p1.Y - p2.Y);
        
        /**
        * Multiply current point by the real value.
        *
        * @param v - value to multiply by.
        * @return current point multiplied by the given value.
        */
        public static Point2D operator *(Point2D p, double v)
            => new Point2D(p.X * v, p.Y * v);

        /**
        * Safely normalize current point.
        */
        public void Normalize()
        {
            double l = Math.Sqrt(X * X + Y * Y);
            if (IsZero(l))
                X = Y = 0.0;
            else
            {
                X /= l;
                Y /= l;
            }
        }

        /**
        * Get the absolute minimum of two given points.
        *
        * @param p1 - first point.
        * @param p2 - second point.
        * @return absolute minimum of the given points' coordinates.
        */
        public static Point2D AbsMin(Point2D p1, Point2D p2) =>
            new Point2D(
                Math.Abs(p1.X) < Math.Abs(p2.X) ? p1.X : p2.X, 
                Math.Abs(p1.Y) < Math.Abs(p2.Y) ? p1.Y : p2.Y
            );
    };
}