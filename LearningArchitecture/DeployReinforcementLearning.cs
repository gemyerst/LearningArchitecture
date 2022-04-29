using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace LearningArchitecture
{
    public class DeployReinforcementLearning : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the DeployReinforcementLearning class.
        /// </summary>
        public DeployReinforcementLearning()
          : base("DeployReinforcementLearning", "DrlM",
              "Deploy pre-trained model",
              "RLA", "Agents")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Policy Path", "P", "pre-trained policy to deploy in form of a Qtable", GH_ParamAccess.item);
            pManager.AddIntegerParameter("num", "n", "Number of Pieces", GH_ParamAccess.item, 50);
            pManager.AddBooleanParameter("iterate", "i", "iterate agent for new voxels", GH_ParamAccess.item, true);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Agent", "A", "Agent as moving though environment", GH_ParamAccess.list); //can also output lists or trees
            pManager.AddBoxParameter("VoxelBox", "VB", "Generated Voxel Boxes", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string policyPath = "";
            int numPieces = 15;
            bool reset = false;
            // access the input parameters individually. 
            if (!DA.GetData(0, ref policyPath)) return;
            if (!DA.GetData(1, ref numPieces)) return;
            if (!DA.GetData(2, ref reset)) return;

            if (reset == true)
            {
                reset = false;
                this.ExpireSolution(true);
            }

                /// CREATE ENVIRONMENT
                double[] envVals = UnpackEnvironment(policyPath + "_env");
            EnvironmentComplex env = CreateEnvironment(envVals, numPieces);
            
            /// RUN MODEL
            var qTable = UnpackQtable(policyPath + "_qVals"); //POLICY FROM TRAINING
            var model = new Qlearning(env, true, "none", qTable);
            model.Learn(1); //number of timesteps 1 as model is already trained

            model.Save(policyPath + "test");

            List<Point3d> voxelsOut = Globals.builtVoxels;
            List<Box> voxelBoxes = voxelsOut.Select(x => new Box(new Plane(x, new Vector3d(0, 0, 1)), Globals.bxSize, Globals.bxSize, Globals.bxSize)).ToList();

            /// OUTPUT DATA
            DA.SetDataList(0, Globals.locationHistory);
            DA.SetDataList(1, voxelBoxes);
        }
        //create animation in grasshopper
        

        public double[] UnpackEnvironment(string filePath)
        {
            string envValues = File.ReadAllText(filePath);
            double[] returnVals = envValues.Split(',').Select(x => Convert.ToDouble(x)).ToArray();
            return returnVals;
        }

        internal EnvironmentComplex CreateEnvironment(double[] envVals, int numPieces)
        {
            double[] min = new double[] { envVals[0], envVals[1], envVals[2] };
            double[] max = new double[] { envVals[3], envVals[4], envVals[5] };
            double goalDensity = envVals[7];
            double goalArea = envVals[8];
            double goalHeight = envVals[9];
            double blockSize = 1;
            EnvironmentComplex env = new EnvironmentComplex(min, max, numPieces, goalDensity, goalArea, goalHeight, blockSize);
            return env;
        }

        internal DictionaryQTable<State, double[]> UnpackQtable(string filePath)
        {
            List<string> labels = File.ReadAllLines(filePath).ToList();

            var qTable = new DictionaryQTable<State, double[]>(() => new double[] {0, 0, 0, 0, 0, 0, 0});
            for (int i = 0; i < labels.Count; i++)
            {
                var line = labels[i];
                string[] data = line.Split(',');

                double[] location = new string[] { data[0], data[1], data[2] }.Select(x => Convert.ToDouble(x)).ToArray();
                double[] contextRays = new string[] { data[3], data[4], data[5], data[6] }.Select(x => Convert.ToDouble(x)).ToArray();
                bool busy = (data[7] == "1") ? true : false;
                double[] qVals = new string[] { data[8], data[9], data[10], data[11], data[12], data[13], data[14] }.Select(x => Convert.ToDouble(x)).ToArray();

                State state = new State(location, busy, contextRays);
                qTable.UpdateQValues(state, qVals);
            }
            return qTable;
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.RL_color_backdrop;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("F03B8DBE-295A-4174-BD68-03CD78CC44B9"); }
        }
    }
}