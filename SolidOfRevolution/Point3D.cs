using Common;

namespace SolidOfRevolution
{
    internal struct Point3D
	{
		public double X {get;set;}
		public double Y {get;set;}
		public double Z {get;set;}

		public Point3D(double x, double y, double z)
		{
			X = x;
			Y = y;
			Z = z;
		}

		public Point3D(Point2D point, double z)
		{
			X = point.X;
			Y = point.Y;
			Z = z;
		}

		public static Point3D operator *(MVPMatrix m, Point3D p)
		{
			float[] content = m.Content;
			return new Point3D(
                content[0] * p.X + content[4] * p.Y + content[8] * p.Z,
                content[1] * p.X + content[5] * p.Y + content[9] * p.Z,
                content[2] * p.X + content[6] * p.Y + content[10] * p.Z
            );
		}

		public static Point3D operator - (Point3D p1, Point3D p2) =>
            new Point3D(p1.X - p2.X, p1.Y - p2.Y, p1.Z - p2.Z);
	};
}