using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LearningArchitecture
{
    internal class DRAFT_MonteCarlo
    {

        /// <summary>
        /// CODE BY GEORGINA MYERS
        /// INSPIRED BY https://github.com/adityagilra/simple-A2C
        ///             https://towardsdatascience.com/monte-carlo-simulations-with-python-part-1-f5627b7d60b0
        /// </summary>
        /// 
        Random rand;
        public DRAFT_MonteCarlo()
        {
            Random rand = new Random();
        }

        public double Model(double min, double max, int numSamples)
        {

            double sumSamples = 0;
            for (int i = 0; i < numSamples; i++)
            {
                double x = GetRandomNum(min, max);
                sumSamples += FofX(x);
            }
            return (max - min) * (double)(sumSamples / numSamples);
        }
        public double GetRandomNum(double min, double max)
        {
            double range = max - min;
            int choice = Convert.ToInt32(rand.NextDouble());
            return min + range * choice;
        }

        public double FofX(double x)
        {
            return (Math.Exp(-1 * x) / (1 + Math.Pow(x - 1, 2)));
        }
        public void A1C(string policyName, EnvironmentSimple env, bool t, string logPath) //BASED ON MONTE CARLO ALGORITHM
        {

            //var agent = PPO('MlpPolicy', env, true, log_path); //policy, environment, log results, log location
        }
    }
}
