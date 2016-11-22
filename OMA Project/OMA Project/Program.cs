using System;
using System.Collections.Generic;
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
                int q = Solver.ObjectiveFunction(currentSolution, x);
                int bestUntilNow = q;
                while (r.Enabled)
                {
                    SortedSet<int[]> tempSolution = currentSolution.DeepClone();
                    while (!Solver.GenerateNeighborhood(tempSolution, m, x.TaskPerUser)) { }
                    int partial = Solver.ObjectiveFunction(tempSolution, x);
                    if (partial < bestUntilNow)
                    {
                        bestUntilNow = partial;
                        currentSolution = tempSolution.DeepClone();
                    }
                }
                Console.WriteLine(bestUntilNow);
                Console.Read();
            }
        }

        private static void Callback(object sender, ElapsedEventArgs e)
        {
            ((Timer)sender).Enabled = false;
        }
    }
}
