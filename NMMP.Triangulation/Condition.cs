using System.Collections.Generic;

namespace NMMP.Triangulation
{
    public class Condition
    {
        public double Beta { get; private set; }
        public double Sigma { get; private set; }
        public double UcCof { get; private set; }
        public List<Line> Segments { get; set; }
        public Condition(double beta, double sigma, double ucCof)
        {
            Beta = beta;
            Sigma = sigma;
            UcCof = ucCof;
        }
    }
}
