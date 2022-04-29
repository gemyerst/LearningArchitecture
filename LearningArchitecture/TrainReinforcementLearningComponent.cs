using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace LearningArchitecture
{
    public class TrainReinforcementLearningComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, new tabs/panels will automatically be created.
        /// </summary>
        public TrainReinforcementLearningComponent() //this is the constructor class! remember we have an example code in here, so use it as a template and build around it..
          : base("Train Reinforcement Learning Model", "TrlM", 
            "Use reinforcement Learning to generate architectural masses",
            "RLA", "Agents") //name, nickname, description, category, subcategory
        {
        }

        /// RESGISTER INPUT PARAMS
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            /// INPUTS: ORIGIN, CONTEXT, NUM AGENTS, MODEL TYPE
            pManager.AddIntegerParameter("timeSteps", "t", "number of Timesteps to train on", GH_ParamAccess.item, 200);
            pManager.AddBoxParameter("bounds", "b", "bounding box that provides limits to otherwise continuous space", GH_ParamAccess.item, new Box(Plane.WorldXY, new Interval(-10, 10), new Interval(-10, 10), new Interval(-10, 10)));
            pManager.AddIntegerParameter("num", "n", "Number of Pieces", GH_ParamAccess.item, 15);
            pManager.AddNumberParameter("goalDensity", "d", "goal building density -  bigger number = wider building, smaller number = taller building", GH_ParamAccess.item, 0.5);
            pManager.AddNumberParameter("goalArea", "a", "goal building area -  bigger number = wider building", GH_ParamAccess.item, 5);
            pManager.AddNumberParameter("goalHeight", "h", "goal building height -  bigger number = taller building", GH_ParamAccess.item, 5);
            pManager.AddTextParameter("save model path", "p", "save as pre-trained model: saves to given path, if no path is supplied it does not save.", GH_ParamAccess.item, "none"); //input options for the user go here

            // If you want to change properties of certain parameters, you can use the pManager instance to access them by index: pManager[0].Optional = true;
        }

        /// RESGISTER OUTPUT PARAMS
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            // Use the pManager object to register your output parameters. Output parameters do not have default values, but they too must have the correct access type.
            /// INPUTS: 
            pManager.AddPointParameter("Agent", "A", "Agent as moving though environment", GH_ParamAccess.list); //can also output lists or trees
            pManager.AddBoxParameter("VoxelBox", "VB", "Generated Voxel Boxes", GH_ParamAccess.list);
            pManager.AddTextParameter("Qtable", "Q", " Qtable describing the estimated reward at each state for each possible action", GH_ParamAccess.item);
            pManager.AddTextParameter("Evaluation", "E", " Evaluation of Trained Policy", GH_ParamAccess.item);
        }

        /// METHOD
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // retrieve all data from the input parameters - declaring variables and assigning them starting values.
            int timeSteps = 200;
            Interval size = new Interval(0, 10);
            Box bounds = new Box(new Plane(), size, size, size);

            int numPieces = 15;
            string saveModelPath = "";
            
            double goalDensity = 0.77;
            double goalArea = 10;
            double goalHeight = 10;
            double blockSize = 1;

            /// INPUT PARAMS
            if (!DA.GetData(0, ref timeSteps)) return; //ref is passing by reference !makes sure theres an input, if not it doesnt run
            if (!DA.GetData(1, ref bounds)) return;
            if (!DA.GetData(2, ref numPieces)) return; 
            if (!DA.GetData(3, ref goalDensity)) return;
            if (!DA.GetData(4, ref goalArea)) return;
            if (!DA.GetData(5, ref goalHeight)) return;
            if (!DA.GetData(6, ref saveModelPath)) return;
            
            /// CREATE ENVIRONMENT
            Point3d minB = bounds.BoundingBox.Min;
            Point3d maxB = bounds.BoundingBox.Max;
            double[] min = new double[] {minB.X, minB.Y, minB.Z};
            double[] max = new double[] {maxB.X, maxB.Y, maxB.Z};
            EnvironmentComplex env = new EnvironmentComplex(min, max, numPieces, goalDensity, goalArea, goalHeight, blockSize);

            /// TRAIN MODEL
            var qTable = new DictionaryQTable<State, double[]>(() => new double[]{ 0, 0, 0, 0, 0, 0, 0 });
            var model = new Qlearning(env, true, saveModelPath, qTable);
            model.Learn(timeSteps); //number of timesteps for training here (200 for test, 2000 for deployment)

            /// EVALUATE POLICY
            string policyEvaluation = model.EvaluatePolicy(); // returns episode number and score acchieved

            /// SAVE QVALUES
            string qTableVals = model.OutputQtableToGH();
            if (saveModelPath != "none")
            {
                model.Save(saveModelPath + "_qVals");
                env.Save(saveModelPath + "_env");
            }
            
            List<Point3d> voxelsOut = Globals.builtVoxels;
            List<Box> voxelBoxes = voxelsOut.Select(x => new Box(new Plane(x, new Vector3d(0,0,1)), Globals.bxSize, Globals.bxSize, Globals.bxSize)).ToList();

            /// OUTPUT DATA
            DA.SetDataList(0, Globals.locationHistory);
            DA.SetDataList(1, voxelBoxes);
            DA.SetData(2, qTableVals);
            DA.SetData(3, policyEvaluation);

        }
        ///HELPER FUNCTION BELOW


        /// <summary>
        /// The Exposure property controls where in the panel a component icon will appear. There are seven possible locations (primary to septenary), 
        /// each of which can be combined with the GH_Exposure.obscure flag, which ensures the component will only be visible on panel dropdowns.
        /// </summary>
        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface. Icons need to be 24x24 pixels.
        /// You can add image files to your project resources and access them like this: return Resources.IconForThisComponent;
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.RL_BW_backdrop;
            }
        } //CREATE AN ICON HERE :D

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("28DC739F-3545-49BF-B688-097724E1046B");
    }
}