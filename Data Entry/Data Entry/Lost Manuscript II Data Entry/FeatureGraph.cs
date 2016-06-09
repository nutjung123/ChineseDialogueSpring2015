using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dialogue_Data_Entry
{
    [Serializable]
	public class FeatureGraph
	{
		private List<Feature> features;
		private Feature root;
		private int maxDepth;
		private double maxDistance;

}
