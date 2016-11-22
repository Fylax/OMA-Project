using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;
using OMA_Project.Extensions;

namespace OMA_Project
{
    internal static class Program
    {
        public static void Main()
        {
            Problem x = Problem.ReadFromFile(@"C:\Users\Fylax\Desktop\Material_assignment\input\Co_30_1_NT_0.txt");
            using (Timer r = new Timer(5000))
            {
                r.Elapsed += Callback;
                r.Enabled = true;
                var m = x.Availabilty.Clone();
                SortedSet<int[]> currentSolution = Solver.GreedySolution(x, m);
                SortedSet<int[]> bestSolution = currentSolution.DeepClone();
                int iterations = 0;
                int interval = 1;
                const double alpha = 0.8;
                const int T0 = 10000;
                int bestFitness = Solver.ObjectiveFunction(bestSolution, x.Matrix);
                while (r.Enabled)
                {
                    int tempFitness = Solver.SimulatedAnnealing(ref currentSolution, x, Math.Pow(alpha, interval) * T0);
                    if (tempFitness < bestFitness)
                    {
                        bestSolution = currentSolution.DeepClone();
                        bestFitness = tempFitness;
                    }
                    if (++iterations % interval == 0)
                    {
                        ++interval;
                        iterations = 0;
                    }

                }
                bestFitness = Solver.ObjectiveFunction(bestSolution, x.Matrix);
                Console.WriteLine(bestFitness);
                Console.Read();
            }
        }

        private static void Callback(object sender, ElapsedEventArgs e)
        {
            ((Timer)sender).Enabled = false;
        }
    }
}
