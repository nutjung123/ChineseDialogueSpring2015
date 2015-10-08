using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dialogue_Data_Entry
{
    //A finite state machine
    class StateMachine
    {
        //A map of all states with their names as keys
        Dictionary<String, State> all_states;
        //The FSM's current state
        State current_state;
        //A history of the names of states previously traversed
        List<String> state_history;

        //Pass in the first state and a list of states
        public StateMachine(State first, Dictionary<String, State> states)
        {
            all_states = states;
            current_state = first;
            state_history = new List<String>();
            state_history.Add(current_state.getStateName());
        }//end constructor StateMachine
        //Pass in only the list of states
        public StateMachine(Dictionary<String, State> states)
        {
            all_states = states;
            current_state = null;
            state_history = new List<String>();
        }//end constructor StateMachine
        public StateMachine()
        {
            all_states = new Dictionary<String, State>();
            current_state = null;
            state_history = new List<String>();
        }//end constructor StateMachine

        //Get the current state
        public State getCurrentState()
        {
            return current_state;
        }//end method getCurrentState

        //Go to the next state
        public void goToNextState()
        {
            //Record the current state in the history
            state_history.Add(current_state.getStateName());
            //Traverse to the next state
            current_state = determineNextState();
        }//end method goToNextState

        //From the current state, choose which state to traverse to next.
        public State determineNextState()
        {
            //Go through each next state name and check its latest occurence
            //in the state history. Pick the least recently used one.
            //A higher last index value means a state was used more recently.
            //TODO: Better selection criteria for next state.
            string LRU_state_name = "";
            int lowest_last_index = state_history.Count;
            int temp_index = -1;

            foreach (String temp_name in current_state.getNextStateNames())
            {
                //Get the latest index of this name
                temp_index = state_history.LastIndexOf(temp_name);
                //If temp_index is lesser than lowest_last_index, then this state
                //was used less recently.
                if (temp_index < lowest_last_index)
                {
                    LRU_state_name = temp_name;
                    lowest_last_index = temp_index;
                }//end if
            }//end foreach

            State return_state = null;
            //Get the state corresponding to this state name.
            all_states.TryGetValue(LRU_state_name, out return_state);

            return return_state;
        }//end method determineNextState
    }
}
