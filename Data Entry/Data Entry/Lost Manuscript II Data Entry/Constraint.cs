using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dialogue_Data_Entry
{
    public class Constraint
    {
        public string name;
        public List<Clause> clauses;

        public Constraint(string n, List<Clause> c)
        {
            name = n;
            clauses = c;
        }//end constructor Constraint
    }
}
