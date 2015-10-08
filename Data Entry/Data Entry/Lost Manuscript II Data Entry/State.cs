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

        //A history of which speak was used
        private List<int> speak_index_history;

        public State(Feature f, string n, List<string> next)
        {
            state_feature = f;
            state_name = n;
            next_state_names = next;
            speak_index_history = new List<int>();
        }//end constructor State
        public State(Feature f, List<string> next)
        {
            state_feature = f;
            state_name = state_feature.Data;
            next_state_names = next;
            speak_index_history = new List<int>();
        }//end constructor State
        public State(Feature f)
        {
            state_feature = f;
            state_name = state_feature.Data;
            next_state_names = new List<String>();
            speak_index_history = new List<int>();
        }//end constructor State
        public State()
        {
            state_feature = null;
            state_name = "";
            next_state_names = new List<String>();
            speak_index_history = new List<int>();
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

        //Have this state select its feature's speak value.
        //Selects the Least Recently Used speak.
        //TODO: Use better selection criteria
        public Feature getFeatureWithNextSpeak()
        {
            //Find least recently used index
            int LRU_index = 0;
            int lowest_index_position = state_feature.Speaks.Count;
            for (int i = 0; i < state_feature.Speaks.Count; i++)
            {
                if (speak_index_history.LastIndexOf(i) < lowest_index_position)
                {
                    LRU_index = i;
                    lowest_index_position = speak_index_history.LastIndexOf(i);
                }//end if
            }//end for
            
            //Set the state feature's speak index to the LRU index
            state_feature.speak_index = LRU_index;
            //Add the LRU index to the history list
            speak_index_history.Add(LRU_index);

            //Return the state's feature
            return state_feature;
        }//end method getFeatureWithNextSpeak
    }
}
