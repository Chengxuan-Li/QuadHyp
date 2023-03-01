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
    public class OffsetHandler
    {
        public Polyline polyline;
        public double distance;
        public double maxAllowableDistance;
        public double minApertureRatio;
        public bool inputCurveIsValid = false;
        public bool offsetSuccessful = false;

        double area;

        List<Line> lines = new List<Line>();
        List<double> angles = new List<double>();
        List<Point3d> points = new List<Point3d>();
        List<Point3d> resultPoints = new List<Point3d>();
        List<Line> resultLines = new List<Line>();
        List<Polyline> resultPolylines = new List<Polyline>();
        List<Mesh> resultMeshes = new List<Mesh>();
        

        public OffsetHandler()
        {
            //
        }

        public OffsetHandler(Polyline _polyline, double _distance, double _minApertureRatio)
        {
            polyline = _polyline;
            distance = _distance;
            minApertureRatio = _minApertureRatio;

            for (int i = 0; i < (polyline.Count - 1); i++)
            {
                points.Add(polyline[i]);
            }

            foreach (Line line in polyline.GetSegments())
            {
                lines.Add(line);
            }

            int count = polyline.Count;
            if ((count <= 6) & (count >= 4))
            {
                inputCurveIsValid = true;
                CalculateMaxAllowableDistance();
            }

        }

        void CalculateMaxAllowableDistance()
        {
            AreaMassProperties AMP = AreaMassProperties.Compute(polyline.ToNurbsCurve());
            area = AMP.Area;
            maxAllowableDistance = area * (1 - minApertureRatio) / polyline.Length;
            if (distance > maxAllowableDistance)
            {
                offsetSuccessful = false;
                return;
            }
            else
            {
                offsetSuccessful = GenerateOffset();
            }
        }

        bool GenerateOffset()
        {
            double crossProductResult = Vector3d.CrossProduct(new Vector3d(- lines[0].From + lines[0].To), new Vector3d(- lines[0].From + lines[1].To)).Z;
            LineSide side = (crossProductResult > 0) ? LineSide.Right : LineSide.Left;
            double rotateAngleCorrection = (side == LineSide.Left) ? - 1.0 : + 1.0;
            //TODO still problem with deciding side

            angles.Add(Math.PI - LineLineAngle(lines[lines.Count - 1], lines[0]));
            for (int i = 0; i < (lines.Count - 1); i++)
            {
                angles.Add(Math.PI - LineLineAngle(lines[i], lines[i + 1]));
            }
            
            for (int i = 0; i < points.Count; i++)
            {
                int linesIndex = (i - 1 >= 0) ? (i - 1) : (points.Count - 1);
                Vector3d direction = new Vector3d(lines[linesIndex].To - lines[linesIndex].From);
                direction.Unitize();
                direction.Rotate(rotateAngleCorrection * (Math.PI - 0.5 * angles[i]), Vector3d.ZAxis);
                direction = direction * (distance / Math.Sin(angles[i] / 2));
                resultPoints.Add(points[i] + direction);
            }

            for (int i = 0; i < (resultPoints.Count - 1); i++)
            {
                Line line = new Line(resultPoints[i + 1], resultPoints[i]);
                resultLines.Add(line);
            }
            resultLines.Add(new Line(resultPoints[0], resultPoints[resultPoints.Count - 1]));


            return true;
        }

        double LineLineAngle(Line line1, Line line2)
        {
            Vector3d vec1 = new Vector3d(line1.To - line1.From);
            Vector3d vec2 = new Vector3d(line2.To - line2.From);
            return Vector3d.VectorAngle(vec1, vec2);
        }

        public bool GetResultLines(out List<Line> result)
        {
            if (offsetSuccessful)
            {
                result = resultLines;
                return offsetSuccessful;
            } else
            {
                result = lines;
                return offsetSuccessful;
            }

        }

        public bool GetResultPolylines(out List<Polyline> result)
        {
            if (offsetSuccessful)
            {
                //Mesh mesh = new Mesh();
                for (int i = 0; i < lines.Count; i++)
                {
                    resultPolylines.Add(
                        new Polyline(
                            new List<Point3d>(){
                                lines[i].From,
                                lines[i].To,
                                resultLines[i].From,
                                resultLines[i].To,
                                lines[i].From
                            }));
                }
                result = resultPolylines;
                return offsetSuccessful;
            }
            else
            {
                resultPolylines.Add(polyline);
                result = resultPolylines;
                return offsetSuccessful;
            }
        }

 

        public bool GenerateMeshFromPolyLine(Polyline plyln, out Mesh mesh)
        {
            mesh = new Mesh();
            if (plyln.Count == 4)
            {
                mesh.Vertices.Add(plyln[0]);
                mesh.Vertices.Add(plyln[1]);
                mesh.Vertices.Add(plyln[2]);
                mesh.Faces.AddFace(0, 1, 2);
                return true;
            } else if (plyln.Count == 5)
            {
                mesh.Vertices.Add(plyln[0]);
                mesh.Vertices.Add(plyln[1]);
                mesh.Vertices.Add(plyln[2]);
                mesh.Vertices.Add(plyln[3]);
                mesh.Faces.AddFace(0, 1, 2, 3);
                return true;
            } else if (plyln.Count == 6)
            {
                mesh.Vertices.Add(plyln[0]);
                mesh.Vertices.Add(plyln[1]);
                mesh.Vertices.Add(plyln[2]);
                mesh.Vertices.Add(plyln[3]);
                mesh.Vertices.Add(plyln[4]);
                mesh.Faces.AddFace(0, 1, 4);
                mesh.Faces.AddFace(2, 3, 4);
                return true;
            } else if (plyln.Count == 7)
            {
                mesh.Vertices.Add(plyln[0]);
                mesh.Vertices.Add(plyln[1]);
                mesh.Vertices.Add(plyln[2]);
                mesh.Vertices.Add(plyln[3]);
                mesh.Vertices.Add(plyln[4]);
                mesh.Vertices.Add(plyln[5]);
                mesh.Faces.AddFace(0, 1, 4, 5);
                mesh.Faces.AddFace(1, 2, 3, 4);
                return true;
            } else
            {
                return false;
            }


        }



    }
}
