using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;
using OMA_Project.Extensions;

namespace OMA_Project
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            Problem x = Problem.ReadFromFile(@args[0]);
            using (Timer r = new Timer(4850))
            {
                Stopwatch s = Stopwatch.StartNew();
                r.Elapsed += Callback;
                r.Enabled = true;
                var m = x.Availabilty.Clone();
                LinkedList<int[]> currentSolution = Solver.GreedySolution(x, m.Clone());
                LinkedList<int[]> bestSolution = currentSolution.DeepClone();
                //int iterations = 0;
                //int exponent = 0;
                //const int plateauLength = 15;
                //const double alpha = 0.8;
                //const int T0 = 1000;
                int bestFitness = Solver.ObjectiveFunction(currentSolution, x.Matrix);
                //ulong counter = 0;
                while (r.Enabled)
                {
                    //counter++;
                    currentSolution = Solver.GreedySolution(x, m.Clone());
                    int tempFitness = Solver.ObjectiveFunction(currentSolution, x.Matrix);
                    
                    //int tempFitness = Solver.SimulatedAnnealing(ref currentSolution, x, m, Math.Pow(alpha, exponent) * T0);
                    if (tempFitness < bestFitness)
                    {
                        bestSolution = currentSolution.DeepClone();
                        bestFitness = tempFitness;
                    }
                    /*if (++iterations % plateauLength == 0)
                    {
                        ++exponent;
                        iterations = 0;
                    }*/
                }
                s.Stop();
                WriteSolution.Write(args[1], bestSolution, bestFitness, s.ElapsedMilliseconds, args[0]);
            }
        }

        private static void Callback(object sender, ElapsedEventArgs e)
        {
            ((Timer)sender).Enabled = false;
        }
    }
}
