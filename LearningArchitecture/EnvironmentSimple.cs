using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace LearningArchitecture
{        /// <summary> PLEASE READ
        /// THIS IS THE SIMPLE TEST ENVIRONMENT, IT WILL NOT BE RUN AS PART OF THE MAIN CODE
        /// I HAVE INCLUDED IT IN CASE YOU WOULD LIKE TO SEE MY PROCESS!
        /// </summary>
        ///  INSPIRED BY NICHOLAS RENOTTE https://github.com/nicknochnack/ReinforcementLearningCourse/blob/main/Project%203%20-%20Custom%20Environment.ipynb
    internal class EnvironmentSimple
    {


        /* LETS START 1D
        --ACTION SPACE-- array of shape [1,]
        | Num | Action                 |
        |-----|------------------------|
        | 0   | Move -X                |
        | 1   | Move +X                |
        | 2   | Move -Y                |
        | 3   | Move +Y                |
        | 4   | Move -Z                |
        | 5   | Move +Z                |

        --OBSERVATION SPACE-- array of shape [4,]
        | Num | Observation           | Min                  | Max                |
        |-----|-----------------------|----------------------|--------------------|
        | 0   | Agent Position X      | -10.0                | 10.0               |
        | 1   | Agent Position Y      | -10.0                | 10.0               |
        | 2   | Agent Position Z      | -10.0                | 10.0               |

        --REWARDS--
        -1 for each step taken
        +3 for staying in range 4-6
        */

        Discrete actionSpace;
        BoxSpace observationSpace;
        double[] state;
        int episodeLength;

        Random rand;

        double[] min;
        double[] max;

        public EnvironmentSimple(double[] min, double[] max, int tolerance = 0)
        {
            this.min = min;
            this.max = max;

            actionSpace = new Discrete(6); //number of possible actions
            observationSpace = new BoxSpace(min, max, tolerance); //what we are observing - coordinates

            rand = new Random();
            Tuple<double, double, double>  stateT = ResetEnv();
            state = new double[] {stateT.Item1, stateT.Item2, stateT.Item3};//initial state

            episodeLength = 60;
        }

        public (Tuple<double, double, double>, double, bool, Dictionary<Tuple<double, double, double>, double>) Step(int action) // APPLY ACTION TO RENDER ENVIRONMENT
        {
            bool done;
            double reward = 0;

            // APPLY IMPACT OF ACTION TO STATE
            double[] movement = new double[] {0,0,0};
            switch (action)
            {
                case 0:
                    movement[0] = -1; //-X
                    break;
                case 1:
                    movement[0] = 1; //+X
                    break;
                case 2:
                    movement[1] = -1; //-Y
                    break;
                case 3:
                    movement[1] = 1; //+Y
                    break;
                case 4:
                    movement[2] = 1; //-Z
                    break;
                case 5:
                    movement[2] = -1; //+Z
                    break;
                default:
                    break;
            }
            
            //KEEP WITHIN BOUNDS
            for (int i = 0; i < state.Length; i++)
            {
                state[i] += movement[i];
                double minBound = min[i];
                double maxBound = max[i];
                if (state[i] >= maxBound)
                    state[i] = maxBound;
                else if (state[i] <= minBound)
                    state[i] = minBound;
                
                // CALCULATE REWARD FOR ACTION BASED ON CURRENT STATE - reward matrix needed?
                if (state[i] >= 4 && state[i] <= 6) { reward += 1; } //CUSTOMISE
                else { reward += -1; }
            }
            episodeLength -= 1; // DECREASE EPISODE LENGTH PER STEP

            //CHECK IF FINISHED
            if (episodeLength <= 0) { done = true; }
            else { done = false; }

            //ADD INFO TO DICTIONARY (optional)
            Dictionary<Tuple<double, double, double>, double> info = new Dictionary<Tuple<double, double, double>, double>();
            Tuple<double, double, double> stateT = new Tuple<double, double, double>(state[0], state[1], state[2]);
            info.Add(stateT, reward);

            return (stateT, reward, done, info);
        }
        
        public Discrete ActionSpace()
        { return actionSpace; }
        public BoxSpace ObservationSpace()
        { return observationSpace; }
        public void RenderEnv(Tuple<double, double, double> state) //VISUALISE ENVIRONMENT -- THIS IS WHERE WE CONNECT TO GRASSHOPPER
        {
            //we need to create a render loop, so we can dynamically visualise our environment
            //Box boundingbox = new Box(); 
            //
            //.Clear();
            double xVal = state.Item1;
            double yVal = state.Item2;
            double zVal = state.Item3;

            Globals.currPoint = new Point3d(xVal, yVal, zVal);
            Globals.builtVoxels.Add(Globals.currPoint);
        }
        public Tuple<double, double, double> ResetEnv() //RESET & OBTAIN INITIAL OBSERVATIONS - pass these to agent to determine best action for max reward
        {
            double[] states = new double[observationSpace.Size()];
            for (int i = 0; i < observationSpace.Size(); i++)
            {
                double obvsMid = observationSpace.Mid(i);
                double lowQ = (obvsMid + max[i]) / 2;
                double highQ = (obvsMid + max[i]) / 2;
                int s = rand.Next(Convert.ToInt32(lowQ), Convert.ToInt32(highQ)); //might need to change +1?
                states[i] = s;
            }
            Tuple<double, double, double> statesT = new Tuple<double, double, double>(states[0], states[1], states[2]);

            episodeLength = 60; // RESET EPISODE- might need changing
            return statesT;
        }
    }
}
 