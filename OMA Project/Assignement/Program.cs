﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime;
using System.Threading;
using OMA_Project.Extensions;

namespace OMA_Project
{
    internal static class Program
    {
        public static readonly Random generator = new Random();
        public static Problem problem;

        private static bool timerEnabled = true;

        public static void Main(string[] args)
        {
            var assignement = Process.GetCurrentProcess();
            assignement.PriorityClass = ProcessPriorityClass.High;

            problem = Problem.ReadFromFile(args[0]);
            GC.Collect();
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;

            var r = new Timer(Callback, null, 5000, Timeout.Infinite);
            {
                var s = Stopwatch.StartNew();
                var currentSolution = Solver.InitialSolution();
                var bestSolution = currentSolution.DeepClone();
                var bestFitness = Solver.ObjectiveFunction(currentSolution);

                const int k_0 = 5;
                const int k_max = 25;

                var availabilities = problem.Availability.DeepClone();
                var users = problem.Users;
                try
                {
                    var k = k_0;
                    var accepted = false;
                    while (timerEnabled)
                    {
                        if (accepted)
                        {
                            availabilities = problem.Availability.DeepClone();
                            users = problem.Users;
                        }
                        currentSolution = Solver.VNS(currentSolution, k);
                        var tempFitness = Solver.ObjectiveFunction(currentSolution);
                        if (tempFitness < bestFitness)
                        {
                            accepted = true;
                            bestSolution = Solver.Compactizator(currentSolution);
                            bestFitness = tempFitness;
                            k = k_0;
                        }
                        else
                        {
                            accepted = false;
                            if (k == k_max)
                            {
                                k = k_0;
                            }
                            else
                                k++;
                            currentSolution = bestSolution.DeepClone();
                            problem.Availability = availabilities.DeepClone();
                            problem.Users = users;
                        }
                    }
                }
                catch (NoUserLeft)
                {
                    // Most likely it's an ST, try with a GRASP instead of VNS
                    var requiredUsers = new Dictionary<int, Dictionary<int, int>>();
                    if (timerEnabled)
                        for (var i = 0; i < bestSolution.Count; i += 6)
                        {
                            Dictionary<int, int> required;
                            if (!requiredUsers.ContainsKey(bestSolution[i + 1]))
                            {
                                required = new Dictionary<int, int>();
                                requiredUsers.Add(bestSolution[i + 1], required);
                            }
                            else
                                required = requiredUsers[bestSolution[i + 1]];
                            if (required.ContainsKey(bestSolution[i + 3]))
                                required[bestSolution[i + 3]] += bestSolution[i + 4];
                            else
                                required.Add(bestSolution[i + 3], bestSolution[i + 4]);
                        }
                    while (timerEnabled)
                        try
                        {
                            problem.Availability = problem.ImmutableAvailability.DeepClone();
                            problem.Users = problem.ImmutableUsers;
                            currentSolution = Solver.GRASP(requiredUsers);
                            var tempFitness = Solver.ObjectiveFunction(currentSolution);
                            if (tempFitness < bestFitness)
                            {
                                bestSolution = currentSolution.DeepClone();
                                bestFitness = tempFitness;
                            }
                            else
                            {
                                problem.Users = users;
                            }
                        }
                        catch (NoUserLeft)
                        {
                        }
                }
                s.Stop();
                WriteSolution.WriteMov(args[1], bestSolution);
                WriteSolution.Write(args[1], bestSolution, bestFitness, s.ElapsedMilliseconds, args[0]);
            }
        }

        private static void Callback(object state)
        {
            timerEnabled = false;
        }
    }
}