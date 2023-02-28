using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino;
using Rhino.Geometry;
using Rhino.Collections;

namespace QuadHyp
{
    public class SurfaceQuadSubdivision
    {
        NurbsSurface srf;
        int UCount;
        int VCount;
        double width;
        double height;
        List<Interval> UIntervals;
        List<Interval> VIntervals;
        List<int> Xs, Ys;
        public List<NurbsCurve> allPolygons2d;

        public NurbsCurve TrimCurve3d;
        public NurbsCurve TrimCurve2d;
        public List<NurbsCurve> resultCurves2d = new List<NurbsCurve>();
        public List<NurbsCurve> resultCurves3d = new List<NurbsCurve>();

        public SurfaceQuadSubdivision(NurbsSurface _srf, int _UCount, int _VCount)
        {

            srf = _srf;
            UCount = _UCount;
            VCount = _VCount;
            srf.GetSurfaceSize(out width, out height);
            Interval UInterval = new Interval(0, width);
            Interval VInterval = new Interval(0, height);
            UIntervals = DivideInterval(UInterval, UCount);
            VIntervals = DivideInterval(VInterval, VCount);
            GetXYIndices(out Xs, out Ys, IterateMethod.XFixYIter);

        }

        List<Interval> DivideInterval(Interval interval, int count)
        {
            List<Interval> result = new List<Interval>();
            for (int i = 0; i < count; i++)
            {
                result.Add(new Interval(interval.T0 + i * interval.Length / count, interval.T0 + (i + 1) * interval.Length / count));
            }
            return result;
        }

        public void AddTrim(NurbsCurve crv3d)
        {

            TrimCurve2d = XYZ2UV(crv3d);
            TrimCurve3d = UV2XYZ(TrimCurve2d);
        }

        public Adjacency GetOppositeAdjacency(Adjacency adjacency)
        {
            if (adjacency == Adjacency.Left)
            {
                return Adjacency.Right;
            }
            else if (adjacency == Adjacency.Right)
            {
                return Adjacency.Left;
            }
            else if (adjacency == Adjacency.Top)
            {
                return Adjacency.Bottom;
            }
            else
            {
                return Adjacency.Top;
            }
        }

        public void GetInterval(int x, int y, out Interval UQuery, out Interval VQuery)
        {
            UQuery = UIntervals[x];
            VQuery = VIntervals[y];
        }

        public void GetXYIndices(out List<int> Xs, out List<int> Ys, IterateMethod iterateMethod) // A list that iterates over all subdivided intervals in a particular order
        {
            Xs = new List<int>();
            Ys = new List<int>();

            if (iterateMethod == IterateMethod.XFixYIter)
            {
                for (int i = 0; i < UCount; i++)
                {
                    for (int j = 0; j < VCount; j++)
                    {
                        Xs.Add(i);
                        Ys.Add(j);
                    }
                }

            }
            else
            {
                for (int i = 0; i < VCount; i++)
                {
                    for (int j = 0; j < UCount; j++)
                    {
                        Xs.Add(j);
                        Ys.Add(i);
                    }
                }
            }

        }

        public bool TryGetAdjacentInterval(int x, int y, Adjacency adjacency, out Interval UQuery, out Interval VQuery)
        {
            if (adjacency == Adjacency.Left)
            {
                if (x > 0)
                {
                    GetInterval(x - 1, y, out UQuery, out VQuery);
                    return true;
                }
                else
                {
                    UQuery = new Interval(0, 1);
                    VQuery = new Interval(0, 1);
                    return false;
                }
            }
            else if (adjacency == Adjacency.Right)
            {
                if (x < (UCount - 1))
                {
                    GetInterval(x + 1, y, out UQuery, out VQuery);
                    return true;
                }
                else
                {
                    UQuery = new Interval(0, 1);
                    VQuery = new Interval(0, 1);
                    return false;
                }
            }
            else if (adjacency == Adjacency.Bottom)
            {
                if (y < (VCount - 1))
                {
                    GetInterval(x, y + 1, out UQuery, out VQuery);
                    return true;
                }
                else
                {
                    UQuery = new Interval(0, 1);
                    VQuery = new Interval(0, 1);
                    return false;
                }
            }
            else
            {
                if (y > 0)
                {
                    GetInterval(x, y - 1, out UQuery, out VQuery);
                    return true;
                }
                else
                {
                    UQuery = new Interval(0, 1);
                    VQuery = new Interval(0, 1);
                    return false;
                }
            }


        }

        public void GetCorners(Interval UInterval, Interval VInterval, out Point3d A, out Point3d B, out Point3d C, out Point3d D)
        {
            A = srf.PointAt(UInterval.T0, VInterval.T0);
            B = srf.PointAt(UInterval.T1, VInterval.T0);
            C = srf.PointAt(UInterval.T1, VInterval.T1);
            D = srf.PointAt(UInterval.T0, VInterval.T1);
        }

        public List<Curve> GetTriangulatedInteriorPolygon(Interval UInterval, Interval VInterval, Adjacency adjacency)
        {
            List<Curve> crvs = new List<Curve>();
            Point3d centroid = new Point3d(UInterval.Mid, VInterval.Mid, 0);

            Point3d LT, RT, RB, LB;
            LT = new Point3d(UInterval.T0, VInterval.T0, 0);
            RT = new Point3d(UInterval.T1, VInterval.T0, 0);
            RB = new Point3d(UInterval.T1, VInterval.T1, 0);
            LB = new Point3d(UInterval.T0, VInterval.T1, 0);

            if (adjacency == Adjacency.Left)
            {
                Line line1 = new Line(LB, centroid);
                crvs.Add(line1.ToNurbsCurve());
                Line line2 = new Line(centroid, LT);
                crvs.Add(line2.ToNurbsCurve());
            }
            else if (adjacency == Adjacency.Right)
            {
                Line line1 = new Line(RT, centroid);
                crvs.Add(line1.ToNurbsCurve());
                Line line2 = new Line(centroid, RB);
                crvs.Add(line2.ToNurbsCurve());
            }
            else if (adjacency == Adjacency.Top)
            {
                Line line1 = new Line(LT, centroid);
                crvs.Add(line1.ToNurbsCurve());
                Line line2 = new Line(centroid, RT);
                crvs.Add(line2.ToNurbsCurve());
            }
            else
            {
                Line line1 = new Line(RB, centroid);
                crvs.Add(line1.ToNurbsCurve());
                Line line2 = new Line(centroid, LB);
                crvs.Add(line2.ToNurbsCurve());
            }
            return crvs;
        }

        public NurbsCurve GetEdge(Interval UInterval, Interval VInterval, Adjacency adjacency)
        {
            Point3d LT, RT, RB, LB;
            LT = new Point3d(UInterval.T0, VInterval.T0, 0);
            RT = new Point3d(UInterval.T1, VInterval.T0, 0);
            RB = new Point3d(UInterval.T1, VInterval.T1, 0);
            LB = new Point3d(UInterval.T0, VInterval.T1, 0);

            Line line;
            if (adjacency == Adjacency.Left)
            {
                line = new Line(LB, LT);
            }
            else if (adjacency == Adjacency.Right)
            {
                line = new Line(RT, RB);
            }
            else if (adjacency == Adjacency.Top)
            {
                line = new Line(LT, RT);
            }
            else
            {
                line = new Line(RB, LB);
            }
            return line.ToNurbsCurve();
        }

        public bool GetTriangulatedPolygon(int x, int y, Adjacency adjacency, out NurbsCurve result)
        {
            Interval UInterval, VInterval;
            GetInterval(x, y, out UInterval, out VInterval);
            bool isDefinitive;

            List<Curve> crvs = GetTriangulatedInteriorPolygon(UInterval, VInterval, adjacency);
            Interval UQuery, VQuery;
            if (TryGetAdjacentInterval(x, y, adjacency, out UQuery, out VQuery))
            {
                crvs.AddRange(GetTriangulatedInteriorPolygon(UQuery, VQuery, GetOppositeAdjacency(adjacency)));
                isDefinitive = false;
            }
            else
            {
                crvs.Add(GetEdge(UInterval, VInterval, adjacency));
                isDefinitive = true;
            }
            result = Curve.JoinCurves(crvs)[0].ToNurbsCurve();
            return isDefinitive;
        }

        public List<NurbsCurve> GetAllTriangulatedPolygons()
        {
            List<NurbsCurve> crvs = new List<NurbsCurve>();
            for (int i = 0; i < Xs.Count; i++)
            {
                NurbsCurve crv;
                GetTriangulatedPolygon(Xs[i], Ys[i], Adjacency.Left, out crv);
                crvs.Add(crv);
                GetTriangulatedPolygon(Xs[i], Ys[i], Adjacency.Top, out crv);
                crvs.Add(crv);
                if (GetTriangulatedPolygon(Xs[i], Ys[i], Adjacency.Right, out crv))
                {
                    crvs.Add(crv);
                }
                if (GetTriangulatedPolygon(Xs[i], Ys[i], Adjacency.Bottom, out crv))
                {
                    crvs.Add(crv);
                }
            }
            allPolygons2d = crvs;
            return crvs;
        }

        public Point3d XYZ2UV(Point3d pt)
        {
            double u, v;
            srf.ClosestPoint(pt, out u, out v);
            return new Point3d(u, v, 0);
        }

        public NurbsCurve XYZ2UV(NurbsCurve crv3d)
        {
            List<NurbsCurve> crvs = new List<NurbsCurve>();
            double t0, t1;
            for (int i = 0; i < crv3d.SpanCount; i++)
            {
                var domain = crv3d.SpanDomain(i);
                t0 = domain.T0;
                t1 = domain.T1;
                Line line = new Line(XYZ2UV(crv3d.PointAt(t0)),
                  XYZ2UV(crv3d.PointAt(t1)));

                crvs.Add(line.ToNurbsCurve());
            }
            return Curve.JoinCurves(crvs)[0].ToNurbsCurve();
        }

        public List<NurbsCurve> XYZ2UV(List<NurbsCurve> crvs3d)
        {
            List<NurbsCurve> crvs = new List<NurbsCurve>();
            foreach (NurbsCurve crv3d in crvs3d)
            {
                crvs.Add(XYZ2UV(crv3d));
            }
            return crvs;
        }

        public NurbsCurve UV2XYZ(NurbsCurve crv2d)
        {
            List<NurbsCurve> crvs = new List<NurbsCurve>();
            double t0, t1;
            for (int i = 0; i < crv2d.SpanCount; i++)
            {
                var domain = crv2d.SpanDomain(i);
                t0 = domain.T0;
                t1 = domain.T1;
                Line line = new Line(srf.PointAt(crv2d.PointAt(t0).X, crv2d.PointAt(t0).Y),
                  srf.PointAt(crv2d.PointAt(t1).X, crv2d.PointAt(t1).Y));

                crvs.Add(line.ToNurbsCurve());
            }
            return Curve.JoinCurves(crvs)[0].ToNurbsCurve();
        }

        public List<NurbsCurve> UV2XYZ(List<NurbsCurve> crvs2d)
        {
            List<NurbsCurve> crvs = new List<NurbsCurve>();
            try
            {
                foreach (NurbsCurve crv2d in crvs2d)
                {

                    crvs.Add(UV2XYZ(crv2d));

                }
            }
            catch (Exception e) { }
            return crvs;
        }

        public NurbsSurface GetFacetedSurface3D(Interval UInterval, Interval VInterval)
        {
            Point3d A, B, C, D;
            GetCorners(UInterval, VInterval, out A, out B, out C, out D);
            NurbsSurface facetedSurface = NurbsSurface.CreateFromCorners(A, B, C, D);
            return facetedSurface;
        }

        public List<NurbsSurface> GetAllFacetedSurfaces3D()
        {
            List<NurbsSurface> result = new List<NurbsSurface>();

            for (int i = 0; i < Xs.Count; i++)
            {
                Interval uu, vv;
                GetInterval(Xs[i], Ys[i], out uu, out vv);
                result.Add(GetFacetedSurface3D(uu, vv));
            }
            return result;
        }


        public void CalculateTrims(bool coarse)
        {
            PolygonPolygonAnalyticalIntersection2d PPI2;

            foreach (NurbsCurve curve in allPolygons2d)
            {

                PPI2 = new PolygonPolygonAnalyticalIntersection2d(curve, TrimCurve2d, coarse);

                if (PPI2.hasResult)
                {


                        resultCurves2d.Add(PPI2.resultCurve);


                }
                
            }
            resultCurves3d = UV2XYZ(resultCurves2d);

        }

    }

}
