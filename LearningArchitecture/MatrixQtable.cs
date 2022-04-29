using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LearningArchitecture
{ /// <summary>
/// Creating a dictionary to store the Q-Values rather than a pre-populated table allows us to deal with higher bound spaces,
/// as the state/action combinations are only created as we need them.
/// </summary>
/// <typeparam name="S"></typeparam> STATE
/// <typeparam name="Qvals"></typeparam> calculated Q values for action A and state S
    internal class DictionaryQTable<S, Qvals> : IQtable<S, Qvals> //is generic in terms of S, Qvals, ruled by interface IQtable
    {
        Func<Qvals> initFn;
        IDictionary<S, Qvals> dictionary;

        public DictionaryQTable(Func<Qvals> initFn) //num states, num actions
        {
            this.initFn = initFn;
            this.dictionary = new Dictionary<S, Qvals>();
        }

        public Qvals GetValue(S state) // get qValues for all the states
        {
            if (!dictionary.ContainsKey(state))
            {
                dictionary[state] = initFn.Invoke();
            }
            return dictionary[state];
        }

        public void UpdateQValues(S state, Qvals updatedQvals)
        {
            dictionary[state] = updatedQvals;
        }

        public int GetLength()
        { return dictionary.Count; }

        public KeyValuePair<S, Qvals> ElementAt(int i)
        { return dictionary.ElementAt(i); }
    }
}
