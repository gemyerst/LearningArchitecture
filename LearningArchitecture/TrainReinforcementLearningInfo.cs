  using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace LearningArchitecture
{
    public class TrainReinforcementLearningInfo : GH_AssemblyInfo
    {
        public override string Name => "LearningArchitecture";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "";

        public override Guid Id => new Guid("166C2E9C-C788-449A-9C4B-190A7FBF3ADD");

        //Return a string identifying you or your company.
        public override string AuthorName => "";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "";
    }
}