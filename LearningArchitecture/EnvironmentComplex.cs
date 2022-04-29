using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.IO;

namespace LearningArchitecture
{
    internal class EnvironmentComplex
    {
        /// <summary>
        /// // CODE BY GEORGINA MYERS
        // INSPIRED BY NICHOLAS RENOTTE https://github.com/nicknochnack/ReinforcementLearningCourse/
        // & https://www.learndatasci.com/tutorials/reinforcement-q-learning-scratch-python-openai-gym/
        // --ACTION SPACE-- array of shape [7]
        // | 0   | Place Voxel            | bool
        // | 1   | move forwards          | double
        // | 2   | move backwards         | double
        // | 3   | move up                | double
        // | 4   | move back              | double
        // | 5   | rotate +               | double
        // | 6   | rotate -               | double

        // --OBSERVATION SPACE-- array of shape [4]
        // | Num | Observation           | Min                  | Max                |
        // |-----|-----------------------|----------------------|--------------------|
        // | 0   | Agent Position X      | -10.0                | 10.0               |
        // | 1   | Agent Position Y      | -10.0                | 10.0               |
        // | 2   | Agent Position Z      | -10.0                | 10.0               |
        // | 3   | Context Ray forward   | 0.5                  | max                |
        // | 4   | Context Ray left      | 0.5                  | max                |
        // | 5   | Context Ray right     | 0.5                  | max                |
        // | 6   | Context Ray down      | 0.5                  | max                |
        // | 7   | Place Voxel           | false                | true               |

        // --REWARDS--
        // placing voxel
        // change in building height - in number of stories!
        // change in floor area
        // cantilever - of size 1 => +1 , of size > 1 => -5
        // symmetry - divide into 4 from center of mass (average
        // finish structure
        /// </summary>

        Discrete actionSpace;
        BoxSpace observationSpace;
        double[] location;
        double[] contextRays;
        double[] vector;
        bool build;
        bool busy;

        int episodeLength;
        double reward;
        State state;
        Random rand;

        double[] min;
        double[] max;
        int numPieces;
        int maxNumPieces;
        double movementDist;

        double goalDensity;
        double blockSize;
        double goalArea;
        double goalHeight;

        public EnvironmentComplex(double[] min, double[] max, int numPieces, double goalDensity, double goalArea, double goalHeight, double blockSize = 1, int tolerance = 0)
        {
            this.min = min;
            this.max = max;
            this.numPieces = numPieces;
            this.maxNumPieces = numPieces;
            this.movementDist = blockSize;
            this.goalDensity = goalDensity;
            this.goalArea = goalArea;
            this.goalHeight = goalHeight;
            rand = new Random();

            actionSpace = new Discrete(7); //number of possible actions
            observationSpace = new BoxSpace(min, max, tolerance); //what we are observing - coordinates
            
            state = ResetEnv();//initial state - (x, y, z, numBuilt, context)
            location = state.Location();
            contextRays = state.ContextRays();
            busy = state.Busy();
            vector = new double[3] { 1, 0, 0}; //initialise as X axis vector
            reward = 0;
        }

        public (State, double, bool, Dictionary<State, double>) Step(int action) // APPLY ACTION TO RENDER ENVIRONMENT - returns next state, reward, done, info
        {
            reward = 0;
            bool done;
            double rotationAngle = (Math.PI / 180) * 90; //right angled turns in degrees
            build = false;

            // APPLY IMPACT OF ACTION TO STATE
            switch (action)
            {
                case 0: // build - T or F
                    numPieces -= 1;
                    build = true;
                    busy = true;
                    break;
                case 1: //move + by distance
                    for (int i = 0; i < location.Length; i++)
                        location[i] += vector[i] * movementDist;
                    break;
                case 2: //move - by distance
                    for (int i = 0; i < location.Length; i++)
                        location[i] -= vector[i] * movementDist;
                    break;
                case 3:  // move up
                    location[2] += movementDist;
                    break;
                case 4: // move down
                    location[2] -= movementDist;
                    break;
                case 5: //rotate XY clockwise
                    vector = Rotate(vector, new double[3] { 0, 0, 1 }, rotationAngle);
                    break;
                case 6: //rotate XY anti-clockwise
                    vector = Rotate(vector, new double[3] { 0, 0, 1 }, -rotationAngle);
                    break;
                default:
                    break;
            }

            //CHECKS & UPDATE PARAMETERS
            var rays = new ObservationRaycasts(location);
            contextRays = rays.ReturnHits();
            location = KeepWithinBounds(location); //keep within boundary - CLIP CONTINUOUS ACTIONS IN TRAINING AND TESTING TO AVOID OUT OF BOUND ERROR
            busy = CheckBusy(location, action); //check to see if space already taken
            reward = Reward(action); //calculate reward based on action taken 

            //calculate change in state - create a new state and return it
            state = new State(location, busy, contextRays);
            Globals.currVec = vector; //update current vector

            //CHECK IF FINISHED - end of episode or total pieces placed
            episodeLength -= 1; // decrease episode length per step
            if (numPieces <= 0 || episodeLength <=0) { done = true; } //episode length is a failsafe
            else { done = false; }

            //ADD INFO TO DICTIONARY
            Dictionary<State, double> info = new Dictionary<State, double>();
            info.Add(state, reward);
            return (state, reward, done, info);
        }
        
        ///CALCULATE REWARDS
        public double Reward(int action)
        {
            ///BUILD
            if (action == 0)
            {
                reward += 5;// for placing voxel
                if (Globals.builtVoxels.Count > 0) //rewards only apply from second block onwards - we need something to compare it to
                {
                    reward += BuiltDensityReward(reward); //build density - goal is 1/3 - this makes us build rectilinear buildings
                    reward += FootPrintReward(reward, action); //build footprint - aiming for area size

                    // make sure you're building ontop of something else
                    if (contextRays[3] <= movementDist / 2)
                    {
                        reward += 5;
                        reward += BuiltUpRewards(location, reward, action); //check height change in building height - in number of stories! -- can't build higher if you dont have a base!
                    }
                    else { reward -= 5; }
                }
            }

            //if (busy) { reward -= 20; } //punish for staying in same place too

            ///MOVE
            else if (action == 1 || action == 2 || action == 3 || action == 4)
            {
                reward += 5; //staying in bounds
                if (busy) { reward += 5; }
            }

            ///ROTATE
            else
            {
                reward += 5;
                if (busy) { reward += 5; }
            }

            if(numPieces <= 0) { reward += 20; } //reward for finshing structure and placing all the pieces

            //cantilever - of size 1 => +1 , of size > 1 => -5
            //symmetry - divide into 4 from center of mass (average
            //reward creativity?
            return reward;
        }

        public double BuiltDensityReward(double r)
        {
            double volume = BuiltVolume(location);
            double density = Globals.builtVoxels.Count /volume; // density value 0-1
            double checkDist = (density / goalDensity) * 5 + 0.5; //long-term reward
            r += checkDist;

            for (int i = 0; i < state.ContextRays().Length; i++)
            {
                if (state.ContextRays()[i] <= 1)
                    r += 20 * goalDensity;
            }
            return r;
        }
        public double FootPrintReward(double r, int action)
        {
            bool footprintChange = BuiltArea(location).Item1;
            double footprint = BuiltArea(location).Item2;

            //change in floor area
            if (footprint >= goalArea - 2 && footprint <= goalArea + 2) { r += 5; }
            else if (footprint <= goalArea) { r += 2; }
            else if (footprint == goalArea && !footprintChange) { reward += 10; }
            else if (footprint > goalArea && footprintChange){ reward -= 5; }

            if (location[2] == 0) //encourage building a base
                r += 10;

            return r;
        }
        public double BuiltUpRewards(double[] newLoc, double r, int action)
        {
            double currHeight = newLoc[2];
            double builtHeight = min[2];
            double[] storey = new double[3] { goalHeight / 3, (goalHeight / 3)*2, goalHeight / 3};

            if(action == 3 && goalHeight >= observationSpace.Mid(2)) { r += 2; } //UP
            else if (action == 4 && goalHeight < observationSpace.Mid(2)) { r += 2; } //DOWN
            if (action == 4 && state.ContextRays()[3] > 0.5) { r += 5; } //reward doing building back down to the ground
            else if (action == 3 && state.ContextRays()[3] > 0.5) { r -= 5; } // - reward for trying to go up when there's nothing beneath

            if (currHeight > builtHeight) //changing height
            {
                r += 2;
                if (currHeight >= storey[0]) { r += 2; }
                else if (currHeight >= storey[1]) { r += 2; }
                else if (currHeight >= storey[2]) { r += 2; }
            }

            if (currHeight >= goalHeight - 2 * blockSize && currHeight <= currHeight + 2 * blockSize) { r += 5; }
            else if (currHeight <= goalHeight * blockSize) { r += 1; }
            else if (currHeight > goalHeight + 2) { reward -= 3; }
            return r;
        }

        public Tuple<bool, double> BuiltArea(double[] newLoc)
        {
            bool changed = false;
            double minBoundX = Globals.builtVoxels.Select(x => x.X).Min() - movementDist / 2; //take blockSize into account
            double minBoundY = Globals.builtVoxels.Select(x => x.Y).Min() - movementDist / 2;
            double maxBoundX = Globals.builtVoxels.Select(x => x.X).Max() + movementDist / 2;
            double maxBoundY = Globals.builtVoxels.Select(x => x.Y).Max() + movementDist / 2;

            if (newLoc[0] <= minBoundX) {
                minBoundX = newLoc[0] - movementDist / 2;
                changed = true; }
            else if (newLoc[0] >= maxBoundX) {
                maxBoundX = newLoc[0] + movementDist / 2; 
                changed = true; }
            if (newLoc[1] <= minBoundY) { 
                minBoundY = newLoc[1] - movementDist / 2;
                changed = true;}
            else if (newLoc[1] >= maxBoundY) { 
                maxBoundY = newLoc[1] + movementDist / 2;
                changed = true; }

            double area = (maxBoundX - minBoundX) * (maxBoundY - minBoundY);
            return new Tuple<bool, double>(changed, area);
        }
        public double BuiltVolume(double[] newLoc)
        {
            double minBoundZ = Globals.builtVoxels.Select(x => x.Z).Min() - movementDist / 2;
            double maxBoundZ = Globals.builtVoxels.Select(x => x.Z).Max() + movementDist / 2;

            if (newLoc[2] <= minBoundZ){ minBoundZ = newLoc[2] - movementDist / 2; }
            else if (newLoc[2] >= maxBoundZ) { maxBoundZ = newLoc[2] + movementDist / 2; }
            double volume = BuiltArea(newLoc).Item2 * (maxBoundZ - minBoundZ);
            return volume;
        }

        public bool CheckBusy(double[] location, int action)
        {
            List<string> builtHistory = Globals.builtVoxels.Select(x => Convert.ToString(x.X) + Convert.ToString(x.Y) + Convert.ToString(x.Z)).ToList();
            string loc = String.Join("", location.Select(x => Convert.ToString(Math.Round(x, 2))));
            if (builtHistory.Contains(loc) || action == 0) { busy = true; }
            else { busy = false; }
            return busy;
        }
        public double[] KeepWithinBounds(double[] location)
        {
            for (int i = 0; i < location.Length; i++)
            {
                if (location[i] > max[i]) { location[i] = max[i]; }//keep within maxBound
                else if (location[i] < min[i]) { location[i] = min[i]; }//keep within minBound    
            }
            return location;
        }
        public void RenderEnv(State state ) //VISUALISE ENVIRONMENT -- THIS IS WHERE WE CONNECT TO GRASSHOPPER
        {
            double[] locationS = state.Location();
            double xVal = locationS[0];
            double yVal = locationS[1];
            double zVal = locationS[2];
            Globals.currPoint = new Point3d(xVal, yVal, zVal); // set current point to keep track

            Box box = new Box(new Plane(Globals.currPoint, new Vector3d(0, 0, 1)), Globals.bxSize, Globals.bxSize, Globals.bxSize);
            Mesh meshBx = Mesh.CreateFromBox(box, 1, 1, 1);
            Globals.locationHistory.Add(Globals.currPoint);

            if (build == true)
            {
                if(Globals.builtVoxels.Count <= 0)
                    Globals.joinedMesh = meshBx;
                else
                    Globals.joinedMesh.Append(meshBx); //this speeds up processing speeds hugely as we only have to raycast to one mesh
                Globals.builtVoxels.Add(Globals.currPoint);
                Globals.builtBoxes.Add(meshBx);
            }
        }
        
        public Discrete ActionSpace()
        { return actionSpace; }
        public BoxSpace ObservationSpace()
        { return observationSpace; }

        public State ResetEnv() //RESET & OBTAIN INITIAL OBSERVATIONS - pass these to agent to determine best action for max reward
        {
            vector = new double[3] { 1, 0, 0 }; //initialise as X axis vector
            busy = false; //state is empty

            location = new double[3];
            for (int i = 0; i < 2; i++)
            {
                double obvsMid = observationSpace.Mid(i);
                double lowQ = (obvsMid + max[i]) / 2;
                double highQ = (obvsMid + max[i]) / 2;
                location[i] = rand.Next(Convert.ToInt32(lowQ), Convert.ToInt32(highQ)); //set X,Y location to somewhere in the middle half of the bounds
            }
            location[2] = min[2]; //set z coordinate to start ground level

            double[] contextRays = new double[4];
            for (int i = 0; i < contextRays.Length; i++)
                contextRays[i] = 0;

            State statesT = new State(location, busy, contextRays);
            episodeLength = maxNumPieces * 10; // reset episode
            numPieces = maxNumPieces; // reset number of pieces
            return statesT;
        }

        public void Save(string logPath)
        {
            double[] variables = new double[] { min[0], min[1], min[2], max[0], max[1], max[2], numPieces, goalDensity, goalArea, goalHeight, blockSize };
            string varString = string.Join(",", variables);
            File.WriteAllText(logPath, varString);
        }

        // CODE BELOW ADAPTED FROM http://blog.kjeldby.dk/wp-content/uploads/rotate.txt so we can rotate a vector using arrays rather than rhino geometry to speed everything up
        public static double[] Rotate(double[] v, double[] axis, double angle)
        {
            double[] result = new double[3];

            double tr = 1 - Math.Cos((double)angle); //tan
            double cos = Math.Cos((double)angle); ;
            double sin = Math.Sin((double)angle);

            double a1 = (tr * axis[0] * axis[0]) +cos;
            double a2 = (tr * axis[0] * axis[1]) - (sin * axis[2]);
            double a3 = (tr * axis[0] * axis[2]) + (sin * axis[1]);

            double b1 = (tr * axis[0] * axis[1]) + (sin * axis[2]);
            double b2 = (tr * axis[1] * axis[1]) + cos;
            double b3 = (tr * axis[1] * axis[2]) - (sin * axis[0]);

            double c1 = (tr * axis[0] * axis[2]) - (sin * axis[1]);
            double c2 = (tr * axis[1] * axis[2]) + (sin * axis[0]);
            double c3 = (tr * axis[2] * axis[2]) + cos;

            result[0] = a1 * v[0] + a2 * v[1] + a3 * v[2];
            result[1] = b1 * v[0] + b2 * v[1] + b3 * v[2];
            result[2] = c1 * v[0] + c2 * v[1] + c3 * v[2];

            return result;
        }

    }
}
