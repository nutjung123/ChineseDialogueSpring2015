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
        private int thirdArgument;
        private string thirdArgumentType = null;
        private string fourthArgument;
        private string fifthArgument;
        private bool satisfied = false;

        public TemporalConstraint(string first, string second, int third,string fourth,string fifth)
        {
            this.firstArgument = first;
            this.secondArgument = second;
            this.thirdArgument = third;
            this.fourthArgument = fourth;
            this.fifthArgument = fifth;
            getThirdArgumentType();
        }

        public string getThirdArgumentType()
        {
            if (thirdArgumentType == null)
            {
                try
                {
                    int result = Convert.ToInt32(thirdArgument);
                    this.thirdArgumentType = "turn";
                }
                catch (FormatException)
                {
                    this.thirdArgumentType = "topic";
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

        public int ThirdArgument
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

        public string FourthArgument
        {
            get
            {
                return this.fourthArgument;
            }
            set
            {
                this.fourthArgument = value;
            }
        }

        public string FifthArgument
        {
            get
            {
                return this.fifthArgument;
            }
            set
            {
                this.fifthArgument = value;
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
