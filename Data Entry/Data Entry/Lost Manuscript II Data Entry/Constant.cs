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
        public const int spatialWeightIndex = 2;
        public const int hierarchyWeightIndex = 3;
        public const int weightArraySize = 4;

        //Store score components, and score, in return array.
        //Indices are as follows:
        //0 = score
        //1 = novelty
        //2 = discussed amount
        //3 = expected dramatic value
        //4 = spatial constraint value
        //5 = hierarchy constraint value
        public const int scoreArrayScoreIndex = 0;
        public const int scoreArrayNoveltyIndex = 1;
        public const int scoreArrayDiscussedAmountIndex = 2;
        public const int scoreArrayExpectedDramaticIndex = 3;
        public const int scoreArraySpatialIndex = 4;
        public const int scoreArrayHierarchyIndex = 5;
        public const int scoreArraySize = 6;

    }
}
