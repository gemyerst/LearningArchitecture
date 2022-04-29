using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;

namespace LearningArchitecture
    
{ 
    internal class ObservationRaycasts
    {
        /// <summary>
        /// creates raycasts to be able to process and understand environment - we check in 4 directions - f, l, r, d
        /// returns the distance to the nearest mesh box in each direction
        /// </summary>

        Point3d agent;
        List<Vector3d> isovist = new List<Vector3d>(); //vector ray from agents curr location to check neighbors in each direction, returns rounded distance or void
        public ObservationRaycasts(double[] position)
        {
            Vector3d forward;
            if(Globals.currVec == null) //for first item
                forward = new Vector3d(1, 0, 0);
            else
                forward = new Vector3d(Globals.currVec[0], Globals.currVec[1], Globals.currVec[2]); //forward
            Vector3d left = forward;
            left.Rotate((Math.PI / 180) * 90, new Vector3d(0, 0, 1));
            Vector3d right = -left;
            Vector3d down = new Vector3d(0, 0, -1);

            agent = new Point3d(position[0], position[1], position[2]);
            isovist = new List<Vector3d>{
                forward, left, right, down}; //FORWARD, LEFT, RIGHT, DOWN
        }

        public double[] ReturnHits()
        {
            var hitsDist = new double[6];
             
            for (int i = 0; i < isovist.Count; i++)
            {
                double distance = 0;
                var ray = new Ray3d(agent, isovist[i]);
                List<Point3d> eventsBoxes = new List<Point3d>();

                if (Globals.builtBoxes.Count > 0)
                {
                    double events = Rhino.Geometry.Intersect.Intersection.MeshRay(Globals.joinedMesh, ray); //using mesh ray as this is the fastest intersection calculation
                    if (events >= 0)
                    {
                        eventsBoxes.Add(ray.PointAt(events));
                        distance = eventsBoxes.Select(x => agent.DistanceTo(x)).Min(); //find closest point
                    }
                    else { distance = 100; }
                }
                hitsDist[i] = distance;
            }
            return hitsDist;
        }
    }
}