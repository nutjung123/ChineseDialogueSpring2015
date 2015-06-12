using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dialogue_Data_Entry
{
    class Constant
    {
        //An array of weights, for use in calculations.
        //The indices are as follows:
        //  0 - discuss amount weight
        //  1 - novelty weight
        //  2 - spatial constraint weight
        //  3 - hierarchy constraint weight
        public const int discussAmountWeightIndex = 0;
        public const int noveltyWeightIndex = 1;
        public const int spatialConstraintWeightIndex = 2;
        public const int hierarchyConstraintWeightIndex = 3;
        public const int numberOfWeight = 4;

    }
}
