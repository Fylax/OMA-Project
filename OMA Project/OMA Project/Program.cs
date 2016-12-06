using System.Diagnostics;
using System.Timers;
using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using OMA_Project.Extensions;

namespace OMA_Project
{
    internal static class Program
    {
        public static Random generator = new Random();
        public static void Main(string[] args)
        {
            Problem x = Problem.ReadFromFile(@"C:\Users\Fylax\Desktop\Material_assignment\input\Co_30_1_NT_0.txt");

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
                int bestFitness = solver.ObjectiveFunction(currentSolution);
                int tempFitness;

                const int k_0 = 20;
                const int k_max = 70;

                int k = k_0;

                ulong counter = 0;
                bool accepted = true;
                int[][][] availabilities = x.Availability.DeepClone();
                while (r.Enabled)
                {
                    if (!accepted)
                    {
                        availabilities = x.Availability.DeepClone();
                    }
                    solver.VNS(currentSolution, k);
                    tempFitness = solver.ObjectiveFunction(currentSolution);
                    if (tempFitness < bestFitness)
                    {
                        accepted = true;
                        bestSolution = currentSolution.Clone();
                        bestFitness = tempFitness;
                        k = k_0;
                    }
                    else
                    {
                        accepted = false;
                        k = (k == k_max) ? k_0: k + 1;
                        currentSolution = bestSolution.Clone();
                        x.Availability = availabilities.DeepClone();
                    }
                    counter++;
                }
                bool feasible = bestSolution.isFeasible(x);
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
