namespace SolidOfRevolution
{
    internal class Segment
    {
        /**
        * Bezier control points.
        */
        public Point2D[] Points { get; } = new Point2D[4];

        /**
        * Calculate the intermediate curve points.
        *
        * @param t - parameter of the curve, should be in [0; 1].
        * @return intermediate Bezier curve point that corresponds the given parameter.
        */
        public Point2D Calc(double t)
        {
            double t2 = t * t;
            double t3 = t2 * t;
            double nt = 1.0 - t;
            double nt2 = nt * nt;
            double nt3 = nt2 * nt;
            return new Point2D(nt3 * Points[0].X + 3.0 * t * nt2 * Points[1].X + 3.0 * t2 * nt * Points[2].X + t3 * Points[3].X,
                nt3 * Points[0].Y + 3.0 * t * nt2 * Points[1].Y + 3.0 * t2 * nt * Points[2].Y + t3 * Points[3].Y);
        }
    };
}