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
        public const int DiscussAmountWeightIndex = 0;
        public const int NoveltyWeightIndex = 1;
        public const int SpatialWeightIndex = 2;
        public const int HierarchyWeightIndex = 3;
        public const int TemporalWeightIndex = 4;
        public const int JointWeightIndex = 5;
        public const int WeightArraySize = 6;

        //Store score components, and score, in return array.
        //Indices are as follows:
        //0 = score
        //1 = novelty
        //2 = discussed amount
        //3 = expected dramatic value
        //4 = spatial constraint value
        //5 = hierarchy constraint value
        public const int ScoreArrayScoreIndex = 0;
        public const int ScoreArrayNoveltyIndex = 1;
        public const int ScoreArrayDiscussedAmountIndex = 2;
        public const int ScoreArrayExpectedDramaticIndex = 3;
        public const int ScoreArraySpatialIndex = 4;
        public const int ScoreArrayHierarchyIndex = 5;
        public const int ScoreArraySize = 6;

        //Language mode constants
        //  0 = English
        //  1 = Chinese
        public const int EnglishMode = 0;
        public const int ChineseMode = 1;

    }
}
