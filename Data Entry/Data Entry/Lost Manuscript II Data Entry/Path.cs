using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dialogue_Data_Entry
{
    //A path segment between two features in the knowledge graph.
    class Path
    {
        public Feature feature = null;
        public int distance = 0;

        public Path(Feature feat, int dist)
        {
            feature = feat;
            distance = dist;
        }//end constructor Path
    }//end class Path
}
