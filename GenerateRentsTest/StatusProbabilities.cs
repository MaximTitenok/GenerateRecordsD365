using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenerateRentsTest
{
    internal class StatusProbabilities
    {
        public int Code { get; set; }
        public string Status { get; set; }
        public double Probability { get; set; }
        public StatusProbabilities(int Code, string Status, double Probability)
        {
            this.Code = Code;
            this.Status = Status;
            this.Probability = Probability;
        }
    }

}
