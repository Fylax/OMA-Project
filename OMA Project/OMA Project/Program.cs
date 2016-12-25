using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Timers;
using OMA_Project.Extensions;

namespace OMA_Project
{
    /// <summary>
    ///     Application's entry point.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        ///     Random Number Generator used among various method.
        ///     <para />
        ///     As it internally avoids extracting same value multiple consecutive
        ///     times, it has been designed to be a static shared field.
        /// </summary>
        public static readonly Random generator = new Random();

        /// <summary>
        ///     Data concerning the problem.
        /// </summary>
        public static Problem problem;

        public static void Main(string[] args)
        {
            var assignement = Process.GetCurrentProcess();
            assignement.PriorityClass = ProcessPriorityClass.High;

            problem = Problem.ReadFromFile(args[0]);
            GC.Collect();
            RuntimeHelpers.PrepareConstrainedRegions();
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
            GC.TryStartNoGCRegion(174000000);

            using (var r = new Timer(5000))
            {
                var s = Stopwatch.StartNew();
                r.Elapsed += Callback;
                r.Enabled = true;
                var currentSolution = Solver.InitialSolution(false);
                var bestSolution = currentSolution;
                var bestFitness = Solver.ObjectiveFunction(bestSolution);
                var currentBestSolution = currentSolution;
                var currentBestFitness = bestFitness;

                var availabilities = problem.Availability.DeepClone();
                var users = problem.Users;
                try
                {
                    const int k_0 = 5;
                    const int k_max = 25;
                    var k = k_0;
                    var maxCounter = 0;
                    while (r.Enabled)
                    {
                        currentSolution = Solver.VNS(currentSolution, k);
                        var tempFitness = Solver.ObjectiveFunction(currentSolution);
                        if (tempFitness < currentBestFitness)
                        {
                            currentBestFitness = tempFitness;
                            currentBestSolution = currentSolution;
                            availabilities = problem.Availability.DeepClone();
                            users = problem.Users;
                            if (currentBestFitness < bestFitness)
                            {
                                bestSolution = currentBestSolution;
                                bestFitness = currentBestFitness;
                            }
                            k = k_0;
                        }
                        else
                        {
                            if (k == k_max && maxCounter < 10)
                            {
                                k = k_0;
                                maxCounter = 0;
                                // restore problem to inital status
                                problem.Availability = problem.ImmutableAvailability.DeepClone();
                                problem.Users = problem.ImmutableUsers;
                                // solve from start
                                currentSolution = Solver.InitialSolution(true);
                                currentBestFitness = Solver.ObjectiveFunction(currentSolution);
                                currentBestSolution = currentSolution;
                                availabilities = problem.Availability.DeepClone();
                                users = problem.Users;
                            }
                            else
                            {
                                if (k == k_max)
                                    maxCounter++;
                                else
                                    k++;
                                currentSolution = currentBestSolution;
                                problem.Availability = availabilities.DeepClone();
                                problem.Users = users;
                            }
                        }
                    }
                }
                catch (NoUserLeft)
                {
                    // Most likely it's an ST, try with a GRASP instead of VNS
                    var requiredUsers = new Dictionary<int, Dictionary<int, int>>();
                    if (r.Enabled)
                        for (var i = 0; i < bestSolution.Count; i += 6)
                        {
                            Dictionary<int, int> required;
                            if (!requiredUsers.ContainsKey(bestSolution[i + 1]))
                            {
                                required = new Dictionary<int, int>();
                                requiredUsers.Add(bestSolution[i + 1], required);
                            }
                            else
                            {
                                required = requiredUsers[bestSolution[i + 1]];
                            }
                            if (required.ContainsKey(bestSolution[i + 3]))
                                required[bestSolution[i + 3]] += bestSolution[i + 4];
                            else
                                required.Add(bestSolution[i + 3], bestSolution[i + 4]);
                        }
                    while (r.Enabled)
                        try
                        {
                            problem.Availability = problem.ImmutableAvailability.DeepClone();
                            problem.Users = problem.ImmutableUsers;
                            currentSolution = Solver.GRASP(requiredUsers);
                            var tempFitness = Solver.ObjectiveFunction(currentSolution);
                            if (tempFitness < bestFitness)
                            {
                                bestSolution = currentSolution;
                                bestFitness = tempFitness;
                            }
                            else
                            {
                                problem.Users = users;
                            }
                        }
                        catch (Exception)
                        {
                            // ignored
                        }
                }

                s.Stop();
                //bool isOk = Solution.IsFeasible(bestSolution);
                WriteSolution.Write(args[1], bestSolution, bestFitness, s.ElapsedMilliseconds, args[0]);
            }
        }

        private static void Callback(object sender, ElapsedEventArgs e)
        {
            ((Timer) sender).Enabled = false;
        }
    }
}