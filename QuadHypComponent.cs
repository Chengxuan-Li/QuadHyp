using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino;
using Rhino.Geometry;
using Rhino.Collections;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace QuadHyp
{

    public enum EdgeFixtureMethod
    {
        Top,
        Bottom
    }

    public enum IterateMethod
    {
        XFixYIter,
        XIterYFix
    }

    public enum Adjacency
    {
        Left,
        Right,
        Top,
        Bottom
    }

    public enum LineSide
    {
        Left,
        Right
    }

    public enum IntersectionPointRelation
    {
        Enter,
        Leave,
        Inside, // Both sides of the intersection are inside
        Outside, // Both sides of the intersection are outside
        Indefinite
    }

    public enum SpanRelation
    {
        Inside,
        Outside,
        Indefinite
    }


    public class QuadHypComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public QuadHypComponent()
          : base("QuadHyp", "QH",
              "QuadHyp",
              "Extra", "Simple")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddSurfaceParameter("QuadSurface", "QS", "QuadSurface", GH_ParamAccess.item);
            pManager.AddNumberParameter("UDim", "UD", "UDimension", GH_ParamAccess.item);
            pManager.AddNumberParameter("VDim", "VD", "VDimension", GH_ParamAccess.item);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            
            pManager.AddTextParameter("info", "info", "info", GH_ParamAccess.item);
            pManager.AddGeometryParameter("TestOut", "TO", "TestOutput", GH_ParamAccess.list);
          


        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {


            // Declare a variable for the input SURFACE
            Surface srf = null;
            double UDim = 99999999.99;
            double VDim = 99999999;

            // Use the DA object to retrieve the data inside the first input parameter.
            // If the retieval fails (for example if there is no data) we need to abort.
            if (!DA.GetData(0, ref srf)) { return; }
            if (!DA.GetData(1, ref UDim)) { return; }
            if (!DA.GetData(2, ref VDim)) { return; }

            // If the retrieved data is Nothing, we need to abort.
            if (srf == null) { return; }
            if (UDim == 99999999.99) { return; }
            if (VDim == 99999999.99) { return; }

            DA.SetData(0, "123a");
            Brep brep = srf.ToBrep();
            RhinoList<BrepEdge> edges = new RhinoList<BrepEdge>(brep.Edges);
            RhinoList<BrepEdge> sortedEdges = new RhinoList<BrepEdge>(brep.Edges);
            RhinoList<int> indices = new RhinoList<int>();
            sortedEdges.Sort(CompareMidPtZ);
            foreach (BrepEdge edge in sortedEdges)
            {
                int id = edges.FindIndex(x => x.GetHashCode().Equals(edge.GetHashCode()));
                indices.Add(id);
            }
            

            // .Net C# additional checks required to avoid negative indexing
            // GH C# is fine with negative indexing
            BrepEdge bottomEdge = edges[indices[0]];
            BrepEdge topEdge = edges[indices[3]];
            BrepEdge rightEdge = edges[(indices[0] - 1)>=0? (indices[0] - 1): (indices.Count - 1)];
            BrepEdge leftEdge = edges[(indices[3] - 1)>=0? (indices[3] - 1): (indices.Count - 1)];

            List<NurbsCurve> trimCurves = new List<NurbsCurve>(){
        topEdge.ToNurbsCurve(),
        rightEdge.ToNurbsCurve(),
        bottomEdge.ToNurbsCurve(),
        leftEdge.ToNurbsCurve()
        };

            NurbsCurve trimCurve = Curve.JoinCurves(trimCurves)[0].ToNurbsCurve();


            NurbsCurve bottomCurve = AdjustEdge(bottomEdge, rightEdge, leftEdge, EdgeFixtureMethod.Bottom);
            NurbsCurve topCurve = AdjustEdge(topEdge, leftEdge, rightEdge, EdgeFixtureMethod.Top);
            Line leftLine = new Line(bottomCurve.PointAtEnd, topCurve.PointAtStart);
            NurbsCurve leftCurve = leftLine.ToNurbsCurve();
            Line rightLine = new Line(topCurve.PointAtEnd, bottomCurve.PointAtStart);
            NurbsCurve rightCurve = rightLine.ToNurbsCurve();



            double targetWidth, targetHeight;
            targetWidth = Math.Max(topCurve.GetLength(), bottomCurve.GetLength());
            targetWidth = Math.Ceiling(targetWidth / UDim) * UDim;



            // Height targets not used in this version
            targetHeight = Math.Max(leftCurve.GetLength(), rightCurve.GetLength());
            targetHeight = Math.Ceiling(targetHeight / VDim) * VDim;


            int UCount, VCount;
            UCount = (int)Math.Ceiling(targetWidth / UDim);
            VCount = (int)Math.Ceiling(targetHeight / VDim);

            // Adjust lengths of top and bottom curves to be targetWidth
            ScaleCurve(topCurve, targetWidth);
            ScaleCurve(bottomCurve, targetWidth);





            NurbsSurface newSrf = NurbsSurface.CreateFromCorners(topCurve.PointAtStart, topCurve.PointAtEnd, bottomCurve.PointAtStart, bottomCurve.PointAtEnd);
            //A = newSrf;

            SurfaceQuadSubdivision SQS = new SurfaceQuadSubdivision(newSrf, UCount, VCount);
            SQS.AddTrim(trimCurve);
            var A = SQS.GetAllTriangulatedPolygons();

            SQS.CalculateTrims(true);

            var B = SQS.resultCurves2d;


            
            
            DA.SetDataList(1, B);




        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("67fbe529-82b6-4301-897b-db3541adaa5a"); }
        }


        int CompareMidPtZ(BrepEdge edge1, BrepEdge edge2) // IComparer that compares two BrepEdge by their Midpoint Z Value
        {
            double midParam1 = edge1.EdgeCurve.Domain.Mid;
            double midParam2 = edge2.EdgeCurve.Domain.Mid;
            return edge1.EdgeCurve.PointAt(midParam1).Z.CompareTo(edge2.EdgeCurve.PointAt(midParam2).Z);
        }



        NurbsCurve AdjustEdge(BrepEdge edge, BrepEdge prevEdge, BrepEdge nextEdge, EdgeFixtureMethod edgeFixtureMethod)
        {
            Point3d PtA = edge.PointAtStart;
            Point3d PtB = edge.PointAtEnd;
            Vector3d AB = new Vector3d(PtB - PtA);
            Vector3d prev = new Vector3d(prevEdge.PointAtEnd - prevEdge.PointAtStart);
            Vector3d next = new Vector3d(nextEdge.PointAtEnd - nextEdge.PointAtStart);
            double factor;
            int condition;
            if (PtA.Z == PtB.Z)
            {
                condition = 0;
            }
            else if (edgeFixtureMethod == EdgeFixtureMethod.Bottom && PtA.Z > PtB.Z)
            {
                condition = 1;
            }
            else if (edgeFixtureMethod == EdgeFixtureMethod.Top && PtA.Z < PtB.Z)
            {
                condition = 1;
            }
            else
            {
                condition = 2;
            }

            if (condition == 0)
            {
                return edge.ToNurbsCurve();
            }
            else if (condition == 1) // Move A
            {
                factor = (PtB.Z - PtA.Z) / prev.Z;
                Point3d PtA1 = PtA + factor * prev;
                Line line = new Line(PtA1, PtB);
                return line.ToNurbsCurve();
            }
            else // PtA.Z < PtB.Z => Move B
            {
                factor = (PtA.Z - PtB.Z) / next.Z;
                Point3d PtB1 = PtB + factor * next;
                Line line = new Line(PtA, PtB1);
                return line.ToNurbsCurve();
            }

        }

        void ScaleCurve(NurbsCurve crv, double targetLength)
        {
            double factor = targetLength / crv.GetLength();
            Transform scale = Transform.Scale((crv.PointAtStart + crv.PointAtEnd) / 2, factor);
            crv.Transform(scale);
        }

    }


}
