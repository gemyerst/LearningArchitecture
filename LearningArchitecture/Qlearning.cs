using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Rhino.Geometry;

namespace LearningArchitecture
{
    /// <summary>
    /// model based on Qlearning algorithm
    /// inspiration from https://www.learndatasci.com/tutorials/reinforcement-q-learning-scratch-python-openai-gym/
    /// </summary>
    internal class Qlearning
    {
        IQtable<State, double[]> qTable; //matrix [state, action]
        EnvironmentComplex env;
        Discrete actionSpace;
        BoxSpace observationSpace;
        string output;

        double alpha;
        double gamma;
        double epsilon;

        bool log;
        string logPath;
        Random rand;
        public Qlearning(EnvironmentComplex env, bool log, string logPath, IQtable<State, double[]> qTable)
        {
            this.qTable = qTable;
            this.env = env;
            this.log = log;
            this.logPath = logPath;
            rand = new Random();
            this.output = ""; //for evaluation of polivy

            actionSpace = env.ActionSpace(); //indexes what action we take
            observationSpace = env.ObservationSpace(); //double value of observed details of agent based on action

            // HYPERPARAMETERS //
            alpha = 0.5; // LEARNING RATE : 0 < alpha <= 1 : how much q value is updated per iteration
            gamma = 0.75; // DISCOUNT FACTOR 0 <= gamma <= 1 : empahsis on future rewards (1 for long-term, 0 for short-term)
            epsilon = 0.5;// BALANCE TRADEOFF BETWEEN EXPLORATION (random, low val) & EXPLOITATION (based on Qvals, high val) + stop overfitting
        }

        // TRANING AGORITHM TO UPDATE Qtable OVER EPISODES(timesteps)
        public void Learn(int totalTimesteps = 200)
        {
            var info = new Dictionary<State, double>();

            for (int i = 0; i < totalTimesteps; i++)
            {
                Globals.builtVoxels.Clear(); //locations where action = 0 (build)
                Globals.locationHistory.Clear(); //keep track of agent locations
                State state = env.ResetEnv(); //OBSERVATIONS - pass these to agent to determine best action for max reward
                double score = 0; //penalties & rewards
                bool done = false;
                int epochs = 0;

                while (done != true) 
                {
                    int action = DecideAction(state); //decide action - explore or exploit based on the epsilon value
                    env.RenderEnv(state); //view environment

                    //EXCECUTE ACTION IN ENV - get next state & reward
                    double reward;
                    State stateNext;
                    (stateNext, reward, done, info) = env.Step(action);

                    //CALCULATE MAX Q VAL FOR ACTION TO S' (next state)
                    double newQ = Predict(state, action, stateNext, reward);

                    //update Qdictionary
                    double[] updatedQrow = qTable.GetValue(state);
                    updatedQrow[action] = newQ;
                    qTable.UpdateQValues(state, updatedQrow);

                    score += reward;
                    epochs++;
                    state = stateNext;
                }

                //view for debugging
                string iSummary = String.Format("episode: {0}, score: {1} \n ", new object[] { i, score });
                output += iSummary;
            }
        }

        public string EvaluatePolicy()
        { return output; }

        public int DecideAction(State state)
        {
            int action;
            //DECIDE - EXPLORE OR EXPLOIT
            if (rand.Next(actionSpace.Size()) < epsilon) //EXPLORE
            {
                action = env.ActionSpace().Sample();
                while (state.Busy() == true && action == 0) //ILLEGAL ACTION - can't place voxel if its already there
                    action = env.ActionSpace().Sample();
            }
            else //EXPLOIT
            {
                double[] row = qTable.GetValue(state);
                double maxVal = row.Max();
                action = Array.IndexOf(row, maxVal); //get action with highest resulting qValue
                if (state.Busy() == true && action == 0) //ILLEGAL ACTION - can't place voxel if its already there
                {
                    row.ToList().RemoveAt(0);//take second best move
                    row.ToArray();
                    double maxVal2 = row.Max();
                    action = Array.IndexOf(row, maxVal2) + 1;
                }
            }
            return action;
        }

        public double Predict(State state, int action, State stateNext, double reward)  
        {
            //get oldQ value and get highest next possible reward
            double oldQ = qTable.GetValue(state)[action]; 
            double nextMax = qTable.GetValue(stateNext).Max();

            //UPDATE Q OF CURR STATE & ACTION : weighted Q(old) + learned value (reward for curr action in curr state + discounted max reward from next state
            double newQ = (1 - alpha) * oldQ + alpha * (reward + gamma * nextMax);
            return newQ; 
        }

        public string OutputQtableToGH() //use to output the final trained Qtable to a GH panel in a readable format
        {
            string output = "states   (X,Y,Z), (forward, left, right, down) busy? |  qVal: build  |  qVal: move + |  qVal: move -  |  move up  |  move down |  rotate +   | rotate -  |\n";

            for (int i = 0; i < qTable.GetLength(); i++)
            {
                KeyValuePair<State, double[]> info = qTable.ElementAt(i);
                State key = info.Key;
                double[] qVals = qTable.GetValue(key);
                string[] qValsString = qVals.Select(x => Convert.ToString(Math.Round(x, 2))).ToArray();

                string[] location = key.Location().Select(x => Convert.ToString(x)).ToArray();
                string[] contextRays = key.ContextRays().Select(x => Convert.ToString(x)).ToArray();
                output += Convert.ToString(i) + ".";
                output += String.Format("({0}, {1}, {2})", location);
                output += String.Format(":F{0}, L{1}, R{2}, D{3}):", contextRays); //front left right down
                output += Convert.ToString(key.Busy()) + " ||";
                for (int j = 0; j < qVals.Length; j++)
                {
                    output += Convert.ToString(qValsString[j]) + "    | ";
                }
                output += "\n";
            }
            return output;
        }

        public void Save(string logPath)
        {
            ///state: 9 vals, action: 6 vals.
            /// i, x, y, z, forward, left, right, down, busy, action 0, action 1, action 2, action 3, action 4, action 5,
            var csv = new StringBuilder();

            for (int i = 0; i < qTable.GetLength(); i++)
            {
                KeyValuePair<State, double[]> info = qTable.ElementAt(i);
                State k = info.Key;
                double[] qVals = qTable.GetValue(k);
                string[] qValsString = qVals.Select(x => Convert.ToString(x)).ToArray();
                string[] location = k.Location().Select(x => Convert.ToString(x)).ToArray();
                string[] contextRays = k.ContextRays().Select(x => Convert.ToString(x)).ToArray();

                string loc = string.Join(",", location);
                string cr = string.Join(",", new string[] { contextRays[0], contextRays[1], contextRays[2], contextRays[3] });
                string busy = (k.Busy() ? 1:0).ToString(); //booleans don't convert to CSV well, so replace with 1 or 0/ T or F;

                string key = loc + "," + cr + "," + busy;
                string val = string.Join(",", qValsString);
                string line = key + "," + val;

                csv.AppendLine(line);
            }
            File.WriteAllText(logPath, csv.ToString());
        }
    }
}
