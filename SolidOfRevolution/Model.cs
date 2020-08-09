using System.Collections.Generic;
using Common;

namespace SolidOfRevolution
{
    internal class Model
    {
        private const int countOfVerticesPerAPoint = 4;
        private List<Point2D> basePoints = new List<Point2D>();
        private List<Point3D> curvePoints = new List<Point3D>();
        private Point3D rotationAxis =new Point3D();

        private float[] color = { 0.5f, 0.5f, 0.5f };

        private void PreparePoint(Point3D point, List<float> vertices)
        {
            double dx = -PointRadius, dy = -PointRadius;

            for (int j = 0; j < countOfVerticesPerAPoint; j++)
            {
                vertices.Add((float)(point.X + dx));
                vertices.Add((float)(point.Y + dy));
                vertices.Add((float)point.Z);

                if (j % 3 == 0 || j % 3 == 2)
                    dy *= -1;
                if (j % 3 == 1)
                    dx *= -1;
                vertices.AddRange(color);
            }
        }

        private void TraversePoint(uint pos, List<uint> indices)
        {
            indices.Add(countOfVerticesPerAPoint * pos);
            indices.Add(countOfVerticesPerAPoint * pos + 1);
            indices.Add(countOfVerticesPerAPoint * pos + 2);
            indices.Add(countOfVerticesPerAPoint * pos + 2);
            indices.Add(countOfVerticesPerAPoint * pos + 3);
            indices.Add(countOfVerticesPerAPoint * pos);
        }

        private void PreparePoints(List<Point3D> points, List<uint> indices, List<float> vertices, int startI)
        {
            for (int i = 0; i < points.Count; i++)
            {
                PreparePoint(points[i], vertices);
                TraversePoint((uint)(startI + i), indices);
            }
        }

        private void TriangulizeCurve(List<Point3D> curve, List<uint> curveIndices, int startI)
        {
            if (curvePoints.Count < 2)
                return;
            Point3D currentPoint = curvePoints[0];
            for (int i = 0; i < curvePoints.Count - 1; i++)
            {
                int nextI = startI + i + 1;
                Point3D nextPoint = curvePoints[i + 1];
                if (currentPoint.X >= nextPoint.X && currentPoint.Y <= nextPoint.Y ||
                    currentPoint.X <= nextPoint.X && currentPoint.Y >= nextPoint.Y)
                {
                    curveIndices.Add((uint)(countOfVerticesPerAPoint * (startI + i)));
                    curveIndices.Add((uint)(countOfVerticesPerAPoint * nextI));
                    curveIndices.Add((uint)(countOfVerticesPerAPoint * (startI + i) + 2));
                    curveIndices.Add((uint)(countOfVerticesPerAPoint * (startI + i) + 2));
                    curveIndices.Add((uint)(countOfVerticesPerAPoint * nextI + 2));
                    curveIndices.Add((uint)(countOfVerticesPerAPoint * nextI));
                }
                else
                {
                    curveIndices.Add((uint)(countOfVerticesPerAPoint * (startI + i) + 3));
                    curveIndices.Add((uint)(countOfVerticesPerAPoint * nextI + 3));
                    curveIndices.Add((uint)(countOfVerticesPerAPoint * (startI + i) + 1));
                    curveIndices.Add((uint)(countOfVerticesPerAPoint * nextI + 3));
                    curveIndices.Add((uint)(countOfVerticesPerAPoint * nextI + 1));
                    curveIndices.Add((uint)(countOfVerticesPerAPoint * (startI + i) + 1));
                }
                currentPoint = nextPoint;
            }
        }

        private void FindCurveEnds(out Point3D begin, out Point3D end)
        {
            begin = curvePoints[0];
            end = curvePoints[0];
            for (int i = 1; i < curvePoints.Count; i++)
            {
                if (curvePoints[i].X < begin.X)
                    begin = curvePoints[i];
                if (curvePoints[i].X > end.X)
                    end = curvePoints[i];
            }
        }

        public const int VerCoordCount = 3;
        public const int ColorCount = 3;
        public const int VertexSize = VerCoordCount + ColorCount;

        public uint Vao {get;set;}
        public uint Ibo {get;set;}
        public uint Vbo {get;set;}

        public List<uint> BaseIndices {get;} = new List<uint>();
        public List<uint> CurveIndices {get;} = new List<uint>();
        public List<uint> ModelIndices {get;} = new List<uint>();

        public List<float> BaseVertices {get;} = new List<float>();
        public List<float> CurveVertices {get;} = new List<float>();
        public List<float> ModelVertices {get;} = new List<float>();

        public MVPMatrix m {get;set;}

        public double PointRadius {get;set;}

        public Model()
        {
            m = MVPMatrix.GetIdentityMatrix();
	        PointRadius = 0.0015;
        }

        public void RotateAboutAxis(float degree) =>
            m = m.Rotate((float)rotationAxis.X, (float)rotationAxis.Y, (float)rotationAxis.Z, degree);

        public void Rotate(float x, float y, float z, float degree) =>
            m = m.Rotate(x, y, z, degree);

        public void RotateAboutX(float degree) =>
            m = m.RotateAboutX(degree);

        public void RotateAboutY(float degree) =>
            m = m.RotateAboutY(degree);

        public void RotateAboutZ(float degree) =>
            m = m.RotateAboutZ(degree);

        public void AddBasePoint(Point2D point)
        {
            basePoints.Add(point);
            PreparePoint(new Point3D(point, 0.0), BaseVertices);
            TraversePoint((uint)(basePoints.Count - 1), BaseIndices);
        }

        public void ClearBasePoints()
        {
            basePoints.Clear();
            BaseIndices.Clear();
            BaseVertices.Clear();
        }

        public void FormCurve()
        {
            List<Segment> curve;
            TBezier.TBezierSO0(basePoints, out curve);

            ClearCurve();

            if (curve.Count == 0)
                return;

            foreach (var s in curve)
                for (int i = 0; i < TBezier.RESOLUTION; ++i)
                {
                    Point2D p = s.Calc((double)i / (double)TBezier.RESOLUTION);
                    curvePoints.Add(new Point3D(p.X, p.Y, 1.0));
                }

            PreparePoints(curvePoints, CurveIndices, CurveVertices, 0);
            TriangulizeCurve(curvePoints, CurveIndices, 0);
        }

        public void ClearCurve()
        {
            curvePoints.Clear();
            CurveIndices.Clear();
            CurveVertices.Clear();
        }

        public void FormSolidOfRevolution(float rotationDegree)
        {
            ClearSolidOfRevolution();

            Point3D begin, end;
            FindCurveEnds(out begin, out end);
            rotationAxis = end - begin;
            Point2D normalizedAxis = new Point2D(rotationAxis.X, rotationAxis.Y);
            normalizedAxis.Normalize();
            rotationAxis = new Point3D(normalizedAxis, 0.0);

            List<Point3D> currentCurve = new List<Point3D>(curvePoints);
            MVPMatrix rotationMatrix =
                MVPMatrix.GetIdentityMatrix()
                //.rotate(rotationAxis.x, rotationAxis.y, rotationAxis.z, rotationDegree);
                .RotateAboutX(rotationDegree);
            int rotatesCount = (int)(360 / rotationDegree);
            int sizeOfCurve = curvePoints.Count;
            int shift = 0;
            for (int i = 0; i < rotatesCount; i++)
            {
                List<Point3D> nextCurve = new List<Point3D>();
                for (int j = 0; j < sizeOfCurve - 1; j++)
                    nextCurve.Add(rotationMatrix * currentCurve[j]);

                PreparePoints(currentCurve, ModelIndices, ModelVertices, shift);
                TriangulizeCurve(currentCurve, ModelIndices, shift);

                //0: 0, 1, 2, ..., sizeOfCurve - 1
                //1: sizeOfCurve, sizeOfCurve+1, ... 2 * sizeOfCurve - 1
                int I = shift + i;
                int nextI = sizeOfCurve + i + 1;
                for (int j = 0; j < sizeOfCurve; j++)
                {
                    ModelIndices.Add((uint)(countOfVerticesPerAPoint * (j + shift) + 1));
                    ModelIndices.Add((uint)(countOfVerticesPerAPoint * (j + shift + sizeOfCurve) + 1));
                    ModelIndices.Add((uint)(countOfVerticesPerAPoint * (j + shift + sizeOfCurve) + 3));
                    ModelIndices.Add((uint)(countOfVerticesPerAPoint * (j + shift + sizeOfCurve) + 3));
                    ModelIndices.Add((uint)(countOfVerticesPerAPoint * (j + shift) + 2));
                    ModelIndices.Add((uint)(countOfVerticesPerAPoint * (j + shift) + 1));
                }

                shift += sizeOfCurve;
                currentCurve = nextCurve;
            }
        }

        public void ClearSolidOfRevolution()
        {
            ModelIndices.Clear();
            ModelVertices.Clear();
            m = MVPMatrix.GetIdentityMatrix();
        }

        public void SetColor(float r, float g, float b)
        {
            color[0] = r;
            color[1] = g;
            color[2] = b;
        }
    }
}