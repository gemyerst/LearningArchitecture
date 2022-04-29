using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LearningArchitecture
{
    public interface IQtable<S, Qvals> //states, qValue for each action[i]  eg. multi-dimensional observation of state (x,y,z) : qValues for each action (0,1,2); 
    {
        Qvals GetValue(S key); //get get qvalues of actions (eg. X,Y,Z)
        void UpdateQValues(S key, Qvals updatedQ); //update row of qValues
        KeyValuePair<S, Qvals> ElementAt(int i);
        int GetLength();
    }
}
