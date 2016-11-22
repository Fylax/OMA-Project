using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using OMA_Project.Extensions;

namespace OMA_Project
{
    public static class Solver
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="problem"></param>
        /// <param name="av"></param>
        /// <returns>
        /// Array di (in ordine):
        /// 0. Cella di partenza
        /// 1. Cella di arrivo
        /// 2. Time Slot
        /// 3. Tipo utente
        /// 4. Utenti utilizzati
        /// </returns>
        public static SortedSet<int[]> GreedySolution(Problem problem, Availabilities av)
        {
            int[] tasks = new int[problem.Tasks.Length];
            int totCell = problem.Matrix.Cells;
            bool[] visited = new bool[totCell];
            Random generator = new Random();
            SortedSet<int[]> movings = new SortedSet<int[]>(new SolutionComparer());
            problem.Tasks.CopyTo(tasks, 0);
            for (int i = totCell; --i >= 0;)
            {
                int cell;
                do
                {
                    cell = generator.Next(0, totCell);
                } while (visited[cell]);
                visited[cell] = true;
                while (tasks[cell] != 0)
                {
                    int[] minimum = problem.Matrix.GetMin(cell, problem.TaskPerUser, av);
                    int available = av.GetUserNumber(minimum[0], minimum[1], minimum[2]);
                    if (available * problem.TaskPerUser[minimum[2]] >= tasks[cell])
                    {
                        int used = (int)Math.Ceiling(tasks[cell] / (double)problem.TaskPerUser[minimum[2]]);
                        av.DecreaseUser(minimum[0], minimum[1], minimum[2], used);
                        tasks[cell] = 0;
                        movings.Add(new[] { minimum[0], cell, minimum[1], minimum[2], used });
                    }
                    else
                    {
                        if (available != 0)
                        {
                            av.DecreaseUser(minimum[0], minimum[1], minimum[2], available);
                            tasks[cell] -= available;
                            movings.Add(new[] { minimum[0], cell, minimum[1], minimum[2], available });
                        }
                    }
                }
            }
            return movings;
        }

        public static bool GenerateNeighborhood(SortedSet<int[]> currentSolution, Availabilities availabilities, int[] taskPerUser)
        {
            Random seed = new Random();
            int randTuple = seed.Next(currentSolution.Count - 1);
            int randMov = seed.Next(randTuple - 2, randTuple + 2);
            if (randMov < 0)
            {
                randMov = (randTuple == 0) ? 1 : 0;
            }
            else if (randMov > currentSolution.Count - 1)
            {
                randMov = (randTuple == currentSolution.Count - 1) ? currentSolution.Count - 2 : currentSolution.Count - 1;
            }
            int[] baseTuple = currentSolution.ElementAt(randTuple);
            int[] nextTuple = currentSolution.ElementAt(randMov);
            int baseTasks = baseTuple[4] * taskPerUser[baseTuple[3]];
            int nextTasks = nextTuple[4] * taskPerUser[nextTuple[3]];
            bool swappable = false;
            if (baseTasks > nextTasks)
            {
                int totalNextUsers = availabilities.GetUserNumber(nextTuple[0], nextTuple[2], nextTuple[3]) + nextTuple[4];
                int totalNextTasks = totalNextUsers * taskPerUser[nextTuple[3]];
                if (totalNextTasks >= baseTasks)
                {
                    swappable = true;
                }
            }
            else if (baseTasks < nextTasks)
            {
                int totalBaseUsers = availabilities.GetUserNumber(baseTuple[0], baseTuple[2], baseTuple[3]) + baseTuple[4];
                int totalBaseTasks = totalBaseUsers * taskPerUser[baseTuple[3]];
                if (totalBaseTasks >= baseTasks)
                {
                    swappable = true;
                }
            }
            else
            {
                swappable = true;
            }
            if (swappable)
            {
                int baseDestination = baseTuple[1];
                int nextDestination = nextTuple[1];
                availabilities.IncreaseUser(baseTuple[0], baseTuple[2], baseTuple[3], baseTuple[4]);
                availabilities.IncreaseUser(nextTuple[0], nextTuple[2], nextTuple[3], nextTuple[4]);
                int baseRequiredUsers = (int)Math.Ceiling(baseTasks / (double)taskPerUser[nextTuple[3]]);
                int nextRequiredUsers = (int)Math.Ceiling(nextTasks / (double)taskPerUser[baseTuple[3]]);
                availabilities.DecreaseUser(baseTuple[0], baseTuple[2], baseTuple[3], nextRequiredUsers);
                availabilities.DecreaseUser(nextTuple[0], nextTuple[2], nextTuple[3], baseRequiredUsers);
                currentSolution.Remove(baseTuple);
                currentSolution.Remove(nextTuple);
                baseTuple[1] = nextDestination;
                baseTuple[4] = baseRequiredUsers;
                currentSolution.Add(baseTuple);
                nextTuple[1] = baseDestination;
                nextTuple[4] = nextRequiredUsers;
                currentSolution.Add(nextTuple);
            }
            return swappable;
        }

        public static int ObjectiveFunction(IEnumerable<int[]> solution, Costs matrix)
        {
            return solution.Sum(sol => (matrix.GetCost(sol[2], sol[3], sol[0], sol[1]) * sol[4]));
        }

        public static int SimulatedAnnealing(ref SortedSet<int[]> currentSolution, Problem problem, double temperature)
        {
            SortedSet<int[]> neighborSolution = currentSolution.DeepClone();
            while (!GenerateNeighborhood(neighborSolution, problem.Availabilty, problem.TaskPerUser)) { }

            int currentFitness = ObjectiveFunction(currentSolution, problem.Matrix);
            int neighborFitness = ObjectiveFunction(neighborSolution, problem.Matrix);

            if (neighborFitness < currentFitness)
            {
                currentSolution = neighborSolution;
                return neighborFitness;
            }

            double pHat =
                Math.Exp((-neighborFitness-currentFitness) / temperature);
            Random r = new Random();
            double p = r.NextDouble();
            if (p < pHat)
            {
                currentSolution = neighborSolution;
                return neighborFitness;
            }
            return currentFitness;
        }
    }

    internal class SolutionComparer : IComparer<int[]>
    {
        public int Compare(int[] x, int[] y)
        {
            if (x[1] > y[1]) return 1;
            if (x[1] < y[1]) return -1;
            if (x[0] > y[0]) return 1;
            if (x[0] < y[0]) return -1;
            if (x[2] > y[2]) return 1;
            if (x[2] < y[2]) return -1;
            if (x[3] > y[3]) return 1;
            if (x[3] < y[3]) return -1;
            if (x[4] > y[4]) return 1;
            if (x[4] < y[4]) return -1;
            return 0;
        }
    }
}