using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dialogue_Data_Entry
{
    //A clause in a constraint.
    public class Clause
    {
        //A clause consists of two feature names, the relationship
        //between them, and the clause's relationship to other clauses.
        //e.g. ....v(A > B) has:
        //  name_1 = "A"
        //  name_2 = "B"
        //  inner_relationship_id = 0 (greater-than)
        //  outer_relationship_id = 1 (OR)
        
        //Inner relationships:
        //  A > B, A later than B, is id 0
        //  A < B, A before B, is id 1
        //  Truth relationship, T, is id 2.
        //      For A T to be true, A has to have appeared in
        //      the history list at least once. B doesn't matter.

        //Outer relationships:
        // C1 V C2, C1 AND C2, is id 0
        // C1 ^ C2, C1 OR C2, is id 1

        //Negation:
        //If the NOT boolean is TRUE, then any condition
        //whethere this clause would be TRUE it is now FALSE.

        public string name_1;
        public string name_2;
        public int inner_relationship_id;
        public int outer_relationship_id;
        public bool not;

        public Clause(string n1, string n2, int in_rel_id, int out_rel_id, bool n)
        {
            name_1 = n1;
            name_2 = n2;
            inner_relationship_id = in_rel_id;
            outer_relationship_id = out_rel_id;
            not = n;
        }//end constructor Clause
        public Clause()
        {
            name_1 = "";
            name_2 = "";
            inner_relationship_id = -2;
            outer_relationship_id = -2;
        }//end constructor Clause

        //Accessors
        public String getName1()
        {
            return name_1;
        }//end method getName1
        public String getName2()
        {
            return name_2;
        }//end method getName2
        public int getInnerRelationshipId()
        {
            return inner_relationship_id;
        }//end method getInnerRelationshipId
        public int getOuterRelationshipId()
        {
            return outer_relationship_id;
        }//end method getOuterRelationshipId
        public bool getNot()
        {
            return not;
        }//end method getNot

        //Mutators
        public void setName1(string n1)
        {
            name_1 = n1;
        }//end method setName1
        public void setName2(string n2)
        {
            name_2 = n2;
        }//end method setName2
        public void setInnerRelationshipId(int inner_rel_id)
        {
            inner_relationship_id = inner_rel_id;
        }//end method setInnerRelationshipId
        public void setOuterRelationshipId(int outer_rel_id)
        {
            outer_relationship_id = outer_rel_id;
        }//end method setOuterRelationshipId
        public void setNot(bool n)
        {
            not = n;
        }//end method setNot
    }
}
