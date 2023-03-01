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
    public class PolygonPolygonAnalyticalIntersection2d
    {
        public NurbsCurve testCurve;
        public NurbsCurve trimCurve;
        public List<double> tsTest = new List<double>();
        public List<double> tsTrim = new List<double>();
        public List<IntersectionPointRelation> relations = new List<IntersectionPointRelation>();
        public List<Point3d> intersectionPoints = new List<Point3d>();
        public bool hasResult = false;


        int testCurveSpanCount;
        List<Interval> testCurveDomains = new List<Interval>();
        List<double> testCurveTurningTs = new List<double>();
        List<Line> testCurveSegments = new List<Line>();

        public List<Span> testSpans = new List<Span>();


        int trimCurveSpanCount;
        List<Interval> trimCurveDomains = new List<Interval>();
        List<double> trimCurveTurningTs = new List<double>();
        List<Line> trimCurveSegments = new List<Line>();

        public List<Span> trimSpans = new List<Span>();

        public List<Span> resultSpans = new List<Span>();
        public List<NurbsCurve> resultCurves = new List<NurbsCurve>();

        public NurbsCurve resultCurve;


        public PolygonPolygonAnalyticalIntersection2d(NurbsCurve _testCurve, NurbsCurve _trimCurve)
        {
            testCurve = _testCurve;
            trimCurve = _trimCurve;

            testCurveSpanCount = testCurve.SpanCount;
            for (int i = 0; i < testCurveSpanCount; i++)
            {
                testCurveDomains.Add(testCurve.SpanDomain(i));
                testCurveSegments.Add(new Line(testCurve.PointAt(testCurve.SpanDomain(i).T0), testCurve.PointAt(testCurve.SpanDomain(i).T1)));
                testCurveTurningTs.Add(testCurve.SpanDomain(i).T0);
            }
            testCurveTurningTs.Add(testCurve.SpanDomain(testCurve.SpanCount - 1).T1);

            trimCurveSpanCount = trimCurve.SpanCount;
            for (int i = 0; i < trimCurveSpanCount; i++)
            {
                trimCurveDomains.Add(trimCurve.SpanDomain(i));
                trimCurveSegments.Add(new Line(trimCurve.PointAt(trimCurve.SpanDomain(i).T0), trimCurve.PointAt(trimCurve.SpanDomain(i).T1)));
                trimCurveTurningTs.Add(trimCurve.SpanDomain(i).T0);
            }
            trimCurveTurningTs.Add(trimCurve.SpanDomain(trimCurve.SpanCount - 1).T1);

            CalculateIntersections();

        }

        public PolygonPolygonAnalyticalIntersection2d(NurbsCurve _testCurve, NurbsCurve _trimCurve, bool coarse)
        {
            testCurve = _testCurve;
            trimCurve = _trimCurve;

            testCurveSpanCount = testCurve.SpanCount;
            for (int i = 0; i < testCurveSpanCount; i++)
            {
                testCurveDomains.Add(testCurve.SpanDomain(i));
                testCurveSegments.Add(new Line(testCurve.PointAt(testCurve.SpanDomain(i).T0), testCurve.PointAt(testCurve.SpanDomain(i).T1)));
                testCurveTurningTs.Add(testCurve.SpanDomain(i).T0);
            }
            testCurveTurningTs.Add(testCurve.SpanDomain(testCurve.SpanCount - 1).T1);

            trimCurveSpanCount = trimCurve.SpanCount;
            for (int i = 0; i < trimCurveSpanCount; i++)
            {
                trimCurveDomains.Add(trimCurve.SpanDomain(i));
                trimCurveSegments.Add(new Line(trimCurve.PointAt(trimCurve.SpanDomain(i).T0), trimCurve.PointAt(trimCurve.SpanDomain(i).T1)));
                trimCurveTurningTs.Add(trimCurve.SpanDomain(i).T0);
            }
            trimCurveTurningTs.Add(trimCurve.SpanDomain(trimCurve.SpanCount - 1).T1);

            if (coarse)
            {
                int numInside = 0;
                foreach (double t in testCurveTurningTs)
                {
                    if (PointInsidePolygon2d(trimCurve, testCurve.PointAt(t)) != PointContainment.Outside)
                    {
                        numInside++;
                    }
                }
                if (numInside == testCurveTurningTs.Count)
                {
                    resultCurves.Add(testCurve);
                    resultCurve = testCurve;
                    hasResult = true;
                    return;
                }
            }
            CalculateIntersections();
        }


        public PointContainment PointInsidePolygon2d(NurbsCurve targetCurve, Point3d testPoint)
        {
            PointContainment contains = targetCurve.Contains(testPoint, Plane.WorldXY, 0.001);
            return contains;
        }

        void sortAndRemoveDuplicates(List<double> list)
        {
            list.Sort();
            int i = 0;
            while (i < list.Count - 1)
            {
                if (list[i] == list[i + 1])
                {
                    list.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
        }

        void CalculateIntersections()
        {
            LineLineAnalyticalIntersection2d LLI2;
            List<double> intersectionTestCurveTs = new List<double>();
            List<double> intersectionTrimCurveTs = new List<double>();
            List<Point3d> ttpt = new List<Point3d>();

            for (int iTest = 0; iTest < testCurve.SpanCount; iTest++)
            {

                for (int iTrim = 0; iTrim < trimCurve.SpanCount; iTrim++)
                {
                    LLI2 = new LineLineAnalyticalIntersection2d(testCurveSegments[iTest], trimCurveSegments[iTrim]);
                    if (LLI2.isIntersecting)
                    {
                        intersectionTestCurveTs.Add(LLI2.t1 * (-testCurveDomains[iTest].T0 + testCurveDomains[iTest].T1) + testCurveDomains[iTest].T0);
                        intersectionTrimCurveTs.Add(LLI2.t2 * (-trimCurveDomains[iTrim].T0 + trimCurveDomains[iTrim].T1) + trimCurveDomains[iTrim].T0);
                    }
                }
            }

            intersectionTestCurveTs.AddRange(testCurveTurningTs);
            sortAndRemoveDuplicates(intersectionTestCurveTs);

            intersectionTrimCurveTs.AddRange(trimCurveTurningTs);
            sortAndRemoveDuplicates(intersectionTrimCurveTs);

            Span span;
            for (int iTest = 0; iTest < intersectionTestCurveTs.Count - 1; iTest++)
            {
                span = new Span(testCurve, new Interval(intersectionTestCurveTs[iTest], intersectionTestCurveTs[iTest + 1]));
                span.ConfigureRelations(trimCurve);
                testSpans.Add(span);
            }

            for (int iTrim = 0; iTrim < intersectionTrimCurveTs.Count - 1; iTrim++)
            {
                span = new Span(trimCurve, new Interval(intersectionTrimCurveTs[iTrim], intersectionTrimCurveTs[iTrim + 1]));
                span.ConfigureRelations(testCurve);
                trimSpans.Add(span);
            }

            resultSpans.AddRange(testSpans.FindAll(x => !(x.spanRelation.Equals(SpanRelation.Outside))));
            resultSpans.AddRange(trimSpans.FindAll(x => !(x.spanRelation.Equals(SpanRelation.Outside))));

            if ((resultSpans.Count != 0) & (resultSpans != null))
            {
                

                foreach (Span s in resultSpans)
                {
                    resultCurves.Add(s.line.ToNurbsCurve());
                }

                var joined = Curve.JoinCurves(resultCurves);
                if ((joined.Length >= 1) & (joined != null))
                {
                    resultCurve = joined[0].ToNurbsCurve();
                    hasResult = true;
                } else
                {
                    hasResult = false;
                }
                

                
            }
            else
            {
                hasResult = false;
            }

        }

    }

}
