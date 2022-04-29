using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LearningArchitecture
{
    internal class Discrete
    {
        /// <summary>
        ///  create discrete action space with set number of
        ///  ADAPTED FROM OPEN AI GYM https://github.com/openai/gym/blob/master/gym/spaces/box.py
        /// </summary>

        int[] range;
        Random rand;
        int num;
        public Discrete(int num)
        {
            range = new int[num];
            rand = new Random();
            this.num = num;
        }
        public int[] Test()
        {
            int[] arr = Enumerable.Range(0, ++num).ToArray();
            return arr;
        }

        public int Sample()
        { return rand.Next(num); }
        public int Size()
        { return range.Length; }
    }
}
