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
                Solution currentSolution = solver.GreedySolution();
                Solution bestSolution = currentSolution.Clone();
                bool feasible = bestSolution.isFeasible(x);
                int bestFitness = solver.ObjectiveFunction(currentSolution);
                int tempFitness;

                ulong counter = 0;
                while (r.Enabled)
                {
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
