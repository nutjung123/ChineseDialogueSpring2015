using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dialogue_Data_Entry
{
    //A single state in a finite state machine
    class State
    {
        //The feature/node this state represents
        private Feature state_feature;
        //The name of this state, which is its feature's data
        private String state_name;
        //A list of names of states that may follow this one
        private List<String> next_state_names;

        public State(Feature f, string n, List<string> next)
        {
            state_feature = f;
            state_name = n;
            next_state_names = next;
        }//end constructor State
        public State(Feature f, List<string> next)
        {
            state_feature = f;
            state_name = state_feature.Data;
            next_state_names = next;
        }//end constructor State
        public State(Feature f)
        {
            state_feature = f;
            state_name = state_feature.Data;
            next_state_names = new List<String>();
        }//end constructor State
        public State()
        {
            state_feature = null;
            state_name = "";
            next_state_names = new List<String>();
        }//end constructor State

        public Feature getStateFeature()
        {
            return state_feature;
        }//end method getStateFeature
        public String getStateName()
        {
            return state_name;
        }//end method getStateName
        public List<String> getNextStateNames()
        {
            return next_state_names;
        }//end method getNextStateNames

        public void setNextStateNames(List<String> next)
        {
            next_state_names = next;
        }//end method setNextStateNames
    }
}
