using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Timers;
using OMA_Project.Extensions;
using static OMA_Project.Solver;

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

            // optimization block
            RuntimeHelpers.PrepareConstrainedRegions();
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
            GC.TryStartNoGCRegion(174000000);

            var originalAvailability = problem.ImmutableAvailability;
            var originalUsers = problem.ImmutableUsers;
            // end optimization block
            using (var r = new Timer(5000))
            {
                var s = Stopwatch.StartNew();
                r.Elapsed += Callback;
                r.Enabled = true;
                var currentSolution = InitialSolution();
                var bestSolution = currentSolution;
                var bestFitness = ObjectiveFunction(bestSolution);
                var currentBestSolution = currentSolution;
                var currentBestFitness = bestFitness;

                var availabilities = problem.Availability.DeepClone();
                var users = problem.Users;
                try
                {
                    const int k_0 = 5;
                    const int k_max = 30;
                    var k = k_0;
                    while (r.Enabled)
                    {
                        currentSolution = VNS(currentSolution, k);
                        var tempFitness = ObjectiveFunction(currentSolution);
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
                            if (k == k_max)
                            {
                                k = k_0;
                                // restore problem to inital status
                                problem.Availability = originalAvailability.DeepClone();
                                problem.Users = originalUsers;
                                // solve from start
                                currentSolution = InitialSolution();
                                currentBestFitness = ObjectiveFunction(currentSolution);
                                currentBestSolution = currentSolution;
                                availabilities = problem.Availability.DeepClone();
                                users = problem.Users;
                            }
                            else
                            {
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
                            problem.Availability = originalAvailability.DeepClone();
                            problem.Users = originalUsers;
                            currentSolution = GRASP(requiredUsers);
                            var tempFitness = ObjectiveFunction(currentSolution);
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
                bool isOk = Solution.IsFeasible(bestSolution);
                WriteSolution.Write(args[1], bestSolution, bestFitness, s.Elapsed.TotalSeconds, args[0]);
            }
        }

        private static void Callback(object sender, ElapsedEventArgs e)
        {
            ((Timer) sender).Enabled = false;
        }
    }
}