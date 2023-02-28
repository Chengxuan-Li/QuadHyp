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
    public class Span
    {
        public NurbsCurve curve;
        public Line line;
        public Interval domain;
        public Point3d From;
        public PointContainment fromRelation;
        public Point3d To;
        public PointContainment toRelation;
        public SpanRelation spanRelation = SpanRelation.Indefinite;

        public Span(NurbsCurve _curve, Interval _domain)
        {
            curve = _curve;
            domain = _domain;
            line = new Line(curve.PointAt(domain.T0), curve.PointAt(domain.T1));

            From = line.From;
            To = line.To;
        }

        PointContainment PointInsidePolygon2d(NurbsCurve targetCurve, Point3d testPoint)
        {
            PointContainment contains = targetCurve.Contains(testPoint, Plane.WorldXY, 0.001);
            return contains;
        }

        public void ConfigureRelations(NurbsCurve targetCurve)
        {
            fromRelation = PointInsidePolygon2d(targetCurve, From);
            toRelation = PointInsidePolygon2d(targetCurve, To);
            if (fromRelation == PointContainment.Inside)
            {
                spanRelation = SpanRelation.Inside;
            }
            else if (fromRelation == PointContainment.Outside)
            {
                spanRelation = SpanRelation.Outside;
            }
            else if (fromRelation == PointContainment.Coincident)
            {
                if (toRelation == PointContainment.Inside)
                {
                    spanRelation = SpanRelation.Inside;
                }
                else if (toRelation == PointContainment.Outside)
                {
                    spanRelation = SpanRelation.Outside;
                }
                else
                {
                    Point3d testPoint = new Point3d((From.X + To.X) / 2, (From.Y + To.Y) / 2, 0);
                    if (PointInsidePolygon2d(targetCurve, testPoint) == PointContainment.Outside)
                    {
                        spanRelation = SpanRelation.Outside;
                    }
                    else
                    {
                        spanRelation = SpanRelation.Inside;
                    }
                }
            }
        }
    }


}
