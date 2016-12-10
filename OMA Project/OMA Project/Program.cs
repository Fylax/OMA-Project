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
        public static Problem problem;
        public static void Main(string[] args)
        {
            Process assignement = Process.GetCurrentProcess();
            assignement.PriorityClass = ProcessPriorityClass.High;

            problem = Problem.ReadFromFile(args[0]);
            GC.Collect();
            RuntimeHelpers.PrepareConstrainedRegions();
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
            GC.TryStartNoGCRegion(174000000);

            using (var r = new Timer(5000))
            {
                var s = Stopwatch.StartNew();
                var solver = new Solver();
                r.Elapsed += Callback;
                r.Enabled = true;
                var currentSolution = solver.InitialSolution();
                var bestSolution = currentSolution.DeepClone();
                var bestFitness = Solver.ObjectiveFunction(currentSolution);

                const int k_0 = 5;
                const int k_max = 25;

                var k = k_0;

                var accepted = false;
                var availabilities = problem.Availability.DeepClone();
                var users = problem.Users;
                while (r.Enabled)
                {
                    try
                    {
                        if (accepted)
                        {
                            availabilities = problem.Availability.DeepClone();
                            users = problem.Users;
                        }
                        currentSolution = solver.VNS(currentSolution, k);
                        var tempFitness = Solver.ObjectiveFunction(currentSolution);
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
                            problem.Availability = availabilities.DeepClone();
                            problem.Users = users;
                        }
                    }
                    catch (NoUserLeft)
                    {
                        accepted = false;
                        currentSolution = bestSolution.DeepClone();
                        problem.Availability = availabilities.DeepClone();
                        problem.Users = users;
                    }
                }
                s.Stop();

                //WriteSolution.Write(args[1], bestSolution, bestFitness, s.ElapsedMilliseconds, args[0]);
            }
        }

        private static void Callback(object sender, ElapsedEventArgs e)
        {
            ((Timer) sender).Enabled = false;
        }
    }
}