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
            Problem x = Problem.ReadFromFile(@"D:\Dropbox\Uni Mattia\Magistrale\Primo semestre\Optimization methods and algorithms\material_assignment\Material_assignment\input\Co_30_1_NT_0.txt");
            using (Timer r = new Timer(5000))
            {
                r.Elapsed += Callback;
                r.Enabled = true;
                var m = x.Availabilty.Clone();
                LinkedList<int[]> currentSolution = Solver.GreedySolution(x, m.Clone());
                LinkedList<int[]> bestSolution = currentSolution.DeepClone();
                int iterations = 0;
                int exponent = 0;
                const int plateauLength = 30;
                const double alpha = 0.9;
                const int T0 = 1000;
                int bestFitness = Solver.ObjectiveFunction(bestSolution, x.Matrix);
                ulong counter = 0;
                while (r.Enabled)
                {
                    counter++;
                    //currentSolution = Solver.GreedySolution(x, m.Clone());
                    //int tempFitness = Solver.ObjectiveFunction(currentSolution, x.Matrix);
                    
                    int tempFitness = Solver.SimulatedAnnealing(ref currentSolution, x, m, Math.Pow(alpha, exponent) * T0);
                    if (tempFitness < bestFitness)
                    {
                        bestSolution = currentSolution.DeepClone();
                        bestFitness = tempFitness;
                    }
                    if (++iterations % plateauLength == 0)
                    {
                        ++exponent;
                        iterations = 0;
                    }
                    
                }
                Console.WriteLine("Obj "+bestFitness);
                Console.Read();
            }
        }

        private static void Callback(object sender, ElapsedEventArgs e)
        {
            ((Timer)sender).Enabled = false;
        }
    }
}
