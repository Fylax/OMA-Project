using System;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Timers;
using OMA_Project.Extensions;

namespace OMA_Project
{
    internal static class Program
    {
        public static readonly Random generator = new Random();

        public static void Main(string[] args)
        {
            var x = Problem.ReadFromFile(args[0]);

            GC.Collect();
            RuntimeHelpers.PrepareConstrainedRegions();
            GCSettings.LatencyMode = GCLatencyMode.LowLatency;
            GC.TryStartNoGCRegion(107400000);

            using (var r = new Timer(5000))
            {
                var s = Stopwatch.StartNew();
                var solver = new Solver(x);
                r.Elapsed += Callback;
                r.Enabled = true;
                var currentSolution = solver.InitialSolution();
                var bestSolution = currentSolution.DeepClone();
                var bestFitness = solver.ObjectiveFunction(currentSolution);

                const int k_0 = 30;
                const int k_max = 80;

                var k = k_0;

                var accepted = false;
                var availabilities = x.Availability.DeepClone();
                var users = x.Users;
                while (r.Enabled)
                {
                    try
                    {
                        if (accepted)
                        {
                            availabilities = x.Availability.DeepClone();
                            users = x.Users;
                        }
                        solver.VNS(currentSolution, k);
                        var tempFitness = solver.ObjectiveFunction(currentSolution);
                        if (tempFitness < bestFitness)
                        {
                            accepted = true;
                            bestSolution = currentSolution.DeepClone();
                            bestFitness = tempFitness;
                            k = k_0;
                        }
                        else
                        {
                            accepted = false;
                            k = k == k_max ? k_0 : k + 1;
                            currentSolution = bestSolution.DeepClone();
                            x.Availability = availabilities.DeepClone();
                            x.Users = users;
                        }
                    }
                    catch (NoUserLeft)
                    {
                        accepted = false;
                        currentSolution = bestSolution.DeepClone();
                        x.Availability = availabilities.DeepClone();
                        x.Users = users;
                    }
                }
                s.Stop();

                WriteSolution.Write(args[1], bestSolution, bestFitness, s.ElapsedMilliseconds, args[0]);
            }
        }

        private static void Callback(object sender, ElapsedEventArgs e)
        {
            ((Timer) sender).Enabled = false;
        }
    }
}