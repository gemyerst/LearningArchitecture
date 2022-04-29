using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LearningArchitecture
{
    /// <summary>
    /// State class allows us to better control the observations made in the space, and be able to add more dimensions in one place
    /// stateVals: Location(x, y, z), RayObs(forward, left, right, down), busy
    /// </summary>
    internal class State
    {
        Tuple<double, double, double, double, double, double, double, Tuple<bool>> state;
        double[] stateLocation;
        bool busy;
        double[] contextRay;
        public State(double[] stateLoc, bool busy, double[] contextRay)
        {
            stateLocation = stateLoc.Select(x => Math.Round(x, 2)).ToArray();
            this.contextRay = contextRay.Select(x => Math.Round(x, 2)).ToArray();
            this.busy = busy;
            double item1 = stateLocation[0];
            double item2 = stateLocation[1];
            double item3 = stateLocation[2];
            double item4 = contextRay[0];
            double item5 = contextRay[1];
            double item6 = contextRay[2];
            double item7 = contextRay[3];
            Tuple<bool> rest = new Tuple<bool>(busy);

            state = new Tuple<double, double, double, double, double, double, double, Tuple<bool>>(item1, item2, item3, item4, item5, item6, item7, rest);
        }
        
        // need to ensure that each tuple is unique - equality is based on content not the object itself, the tuple also helps with this
        // Equals adapted from https://stackoverflow.com/questions/9317582/correct-way-to-override-equals-and-gethashcode
        public override bool Equals(object obj) 
        {
            var item = obj as State;
            if (item == null)  {return false;}
            return state.Equals(item.state);
        }
        
        public override int GetHashCode()
        { return state.GetHashCode(); }

        public double[] Location()
        { return stateLocation; }
        public double[] ContextRays()
        { return contextRay; }
        public bool Busy()
        { return busy; }
    }
}
