using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LearningArchitecture
{
    /// <summary>
    ///  create continuous observation space - we must discretize the continuous variables with set tolerance as 0.1 (https://www.cs.cmu.edu/~rll/overview/danieln_01/)
    ///  ADAPTED FROM OPEN AI GYM https://github.com/openai/gym/blob/master/gym/spaces/box.py
    /// </summary>
    internal class BoxSpace
    {
        Random rand;
        double[] low;
        double[] high;
        int[,] shape;
        int size;
        int tolerance;

        public BoxSpace(double[] low, double[] high, int tolerance=0) //independent bounds for each dimension
        {
            rand = new Random();
            this.tolerance = tolerance;
            this.low = low;
            this.high = high;

            for (int i = 0; i < low.Length; i++)
                low[i] = Math.Round(low[i], tolerance);
            
            for (int i = 0; i < high.Length; i++)
                high[i] = Math.Round(high[i], tolerance);
            size = low.Length;

            int [,] arr = new int[low.Length, high.Length];
            this.shape = arr;
        }

        public int Size()
        { return size; }

        public double[] Min()
        {  return low;  }

        public double[] Max()
        { return high;  }

        public double Mid(int i)
        {
            double mid = (high[i]+low[i])/2;
            return mid;
        }

        public string Test() //for debugging
        {
            string output = "";
            for (int i = 0; i < size; i++)
                output += Convert.ToString(Min()[i]) + " to " + Convert.ToString(Max()[i]) + " \n";
            return output;
        }
        public double[,] Sample()
        {
            double[,] sample = new double[shape.GetLength(0), shape.GetLength(1)];

            for (int i = 0; i < shape.GetLength(0)-1; i++)
            {
                for (int j = 0; j < shape.GetLength(0)-1; j++)
                {
                    sample[i, j] = rand.NextDouble() * (high[j] - low[i]) + low[i];
                }
            }
            return sample;
        }
    }
}
