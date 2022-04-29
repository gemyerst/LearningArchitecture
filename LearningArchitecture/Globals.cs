using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace LearningArchitecture
{
    public static class Globals
    {
        public static bool init;//maybe not needed
        public static bool isRunning; // link to done in q learning
        public static Random rnd = new Random(42);

        public static List<Point3d> builtVoxels = new List<Point3d>(); //keeps track of all the Points that have been built into voxels
        public static Interval bxSize = new Interval(-0.5, 0.5); 
        public static List<Mesh> builtBoxes = new List<Mesh>(); //creating meshBoxes to speed up implementation
        public static Mesh joinedMesh;

        public static List<Point3d> locationHistory = new List<Point3d>(); // keep track of every location the agent has been to
        public static Point3d currPoint; //agents current location
        public static double[] currVec; //agents current vector

        
    }
}
