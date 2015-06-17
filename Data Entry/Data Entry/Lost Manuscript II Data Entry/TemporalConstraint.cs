using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dialogue_Data_Entry
{
    public class TemporalConstraint
    {
        //first Argument is a topic/node
        //second Argument is symbol. > , >= , == , <= , <
        //third Argument is either a topic/node or turn
        private string firstArgument;   
        private string secondArgument;
        private string thirdArgument;
        private string thirdArgumentType = null;
        private bool satisfied = false;

        public TemporalConstraint(string first, string second, string third)
        {
            this.firstArgument = first;
            this.secondArgument = second;
            this.thirdArgument = third;
        }

        public string getThirdArgumentType()
        {
            if (thirdArgumentType == null)
            {
                try
                {
                    int result = Convert.ToInt32(thirdArgument);
                    thirdArgumentType = "turn";
                }
                catch (FormatException)
                {
                    thirdArgumentType = "topic";
                }
            }
            return thirdArgumentType;
        }

        public string FirstArgument
        {
            get
            {
                return this.firstArgument;
            }
            set
            {
                this.firstArgument = value;
            }
        }

        public string SecondArgument
        {
            get
            {
                return this.secondArgument;
            }
            set
            {
                this.secondArgument = value;
            }
        }

        public string ThirdArgument
        {
            get
            {
                return this.thirdArgument;
            }
            set
            {
                this.thirdArgument = value;
            }
        }

        public bool Satisfied
        {
            get
            {
                return this.satisfied;
            }
            set
            {
                this.satisfied = value;
            }
        }
    }
}
