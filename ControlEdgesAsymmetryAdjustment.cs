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
    class ControlEdgesAsymmetryAdjustment
    {
        Point3d A, B, C, D;
        Point3d newA, newB, newC, newD;
        Vector3d a, b, c, d, h;
        double paramA, paramB, paramC, paramD;

        public NurbsSurface surface;

        public ControlEdgesAsymmetryAdjustment(Point3d pA, Point3d pB, Point3d pC, Point3d pD)
        {
            A = new Point3d(pA.X, pA.Y, 0);
            B = new Point3d(pB.X, pB.Y, 0);
            C = new Point3d(pC.X, pC.Y, 0);
            D = new Point3d(pD.X, pD.Y, 0);

            a = new Vector3d(B - A);
            b = new Vector3d(C - B);
            c = new Vector3d(D - C);
            d = new Vector3d(A - D);

            h = a - c;
            /* original code - seem to be working IN THEORY but not working in practice
            paramA = (-h * d) / (a * h);
            paramA = (paramA < 0) ? paramA : 0;

            paramB = (h * b) / (a * h);
            paramB = (paramB > 0) ? paramB : 0;

            paramC = (h * b) / (c * h);
            paramC = (paramC < 0) ? paramC : 0;

            paramD = (-h * d) / (c * h);
            paramD = (paramD > 0) ? paramD : 0;

            newA = A + paramA * a;
            newA.Z = pA.Z;
            newB = B + paramB * a;
            newB.Z = pB.Z;
            newC = C + paramC * c;
            newC.Z = pC.Z;
            newD = D + paramD * c;
            newD.Z = pD.Z;
            */

            /// adjusted code according to experiments - i do not know how and why it works but it does work in practice
            paramA = (-h * d) / (a * h);
            paramA = (paramA < 0) ? paramA : 0;

            paramB = (h * b) / (a * h);
            paramB = (paramB > 0) ? paramB : 0;

            paramC = (h * b) / (c * h);
            paramC = (paramC > 0) ? paramC : 0;

            paramD = (-h * d) / (c * h);
            paramD = (paramD < 0) ? paramD : 0;

            newA = A + paramA * a;
            newA.Z = pA.Z;
            newB = B + paramB * a;
            newB.Z = pB.Z;
            newC = C - paramC * c;
            newC.Z = pC.Z;
            newD = D - paramD * c;
            newD.Z = pD.Z;


            surface = NurbsSurface.CreateFromCorners(newA, newB, newC, newD);

        }

    }
}
