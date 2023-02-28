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
    public class LineLineAnalyticalIntersection2d
    {
        public Line line1;
        public Line line2;
        public double t1;
        public double t2;
        public bool isIntersecting;
        public bool isAtEnds = false;
        public Point3d intersectionPoint;

        double p0, p1, p2, p3;
        double x0, x1, x2, x3;
        double y0, y1, y2, y3;

        public LineLineAnalyticalIntersection2d(Line _line1, Line _line2)
        {
            line1 = _line1;
            line2 = _line2;
            x0 = line1.From.X;
            x1 = line1.To.X;
            x2 = line2.From.X;
            x3 = line2.To.X;

            y0 = line1.From.Y;
            y1 = line1.To.Y;
            y2 = line2.From.Y;
            y3 = line2.To.Y;

            CalculateIntersection();

        }

        void CalculateIntersection()
        {
            p0 = (y3 - y2) * (x3 - x0) - (x3 - x2) * (y3 - y0);
            p1 = (y3 - y2) * (x3 - x1) - (x3 - x2) * (y3 - y1);
            p2 = (y1 - y0) * (x1 - x2) - (x1 - x0) * (y1 - y2);
            p3 = (y1 - y0) * (x1 - x3) - (x1 - x0) * (y1 - y3);
            isIntersecting = ((p0 * p1) <= 0) & ((p2 * p3) <= 0);
            if (isIntersecting)
            {
                double det;
                det = (x1 - x0) * (y3 - y2) - (y1 - y0) * (x3 - x2);
                t1 = 1 / det * (
                  (y3 - y2) * (x2 - x0) - (x3 - x2) * (y2 - y0)
                  );
                t2 = 1 / det * (
                  (y1 - y0) * (x2 - x0) - (x1 - x0) * (y2 - y0)
                  );
                intersectionPoint = line1.PointAt(t1);
            }
            if ((((t1 == 0) ? 1 : 0) + ((t2 == 0) ? 1 : 0) + ((t1 == 1) ? 1 : 0) + ((t2 == 1) ? 1 : 0)) >= 1)
            {
                isAtEnds = true;
            }
        }




    }
}
