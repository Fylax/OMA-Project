using System.Diagnostics;
using System.Timers;
using System;
using System.Runtime;
using System.Runtime.CompilerServices;

namespace OMA_Project
{
    internal static class Program
    {
        public static Random generator = new Random();
        public static void Main(string[] args)
        {
            Problem x = Problem.ReadFromFile(@"C:\Users\vergo\Google Drive\PoliTO - Magistrale\• 1.1 Optimization Methods and Algorithms\Assignement\Materiale\material_assignment_v2\Material_assignment\input\Co_100_1_T_7.txt");

            GC.Collect();
            RuntimeHelpers.PrepareConstrainedRegions();
            GCSettings.LatencyMode = GCLatencyMode.LowLatency;
            GC.TryStartNoGCRegion(107400000);

            using (Timer r = new Timer(4850))
            {
                Stopwatch s = Stopwatch.StartNew();
                Solver solver = new Solver(x);
                r.Elapsed += Callback;
                r.Enabled = true;
                Solution currentSolution = solver.InitialSolution();
                Solution bestSolution = currentSolution.Clone();
                bool feasible = bestSolution.isFeasible(x);
                int bestFitness = solver.ObjectiveFunction(currentSolution);
                int tempFitness;

                const int k_0 = 20;
                const int k_max = 70;

                int k = k_0;

                ulong counter = 0;
                while (r.Enabled)
                {
                    solver.VNS(currentSolution, k);
                    tempFitness = solver.ObjectiveFunction(currentSolution);
                    if (tempFitness < bestFitness)
                    {
                        bestSolution = currentSolution.Clone();
                        bestFitness = tempFitness;
                        k = k_0;
                    }
                    else
                    {
                        k = (k == k_max) ? k_0: k++;
                        currentSolution = bestSolution.Clone();
                    }
                    counter++;
                }
                s.Stop();

                //WriteSolution.Write(args[1], bestSolution, bestFitness, s.ElapsedMilliseconds, args[0]);
            }
        }

        private static void Callback(object sender, ElapsedEventArgs e)
        {
            ((Timer)sender).Enabled = false;
        }
    }
}
