using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace OMA_Project
{
    public static class Solver
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="problem"></param>
        /// <param name="startCell"></param>
        /// <param name="newAvailabilities"></param>
        /// <returns>
        /// Array di (in ordine):
        /// 0. Cella di partenza
        /// 1. Cella di arrivo
        /// 2. Time Slot
        /// 3. Tipo utente
        /// 4. Utenti utilizzati
        /// </returns>
        public static List<int[]> GreedySolution(Problem problem, Availabilities newAvailabilities)
        {
            int[] tasks = new int[problem.Tasks.Length];
            int totCell = problem.Matrix.Cells;
            bool[] visited = new bool[totCell];
            Random generator = new Random();
            HashSet<int[]> movings = new HashSet<int[]>();
            problem.Tasks.CopyTo(tasks, 0);
            Availabilities av = problem.Availabilty.Clone();
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
                        av.DecreaseUser(minimum[0], minimum[1], minimum[2], available);
                        tasks[cell] -= available;
                        if (available != 0)
                        {
                            movings.Add(new[] { minimum[0], cell, minimum[1], minimum[2], available });
                        }
                    }
                }
            }
            newAvailabilities = av;
            return movings.OrderBy(s => s[0]).ThenBy(s => s[1]).ToList();
        }

        public static bool GenerateNeighborhood(List<int[]> currentSolution, Availabilities availabilities, int[] taskPerUser)
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
                baseTuple[1] = nextDestination;
                nextTuple[1] = baseDestination;
                baseTuple[4] = baseRequiredUsers;
                nextTuple[4] = nextRequiredUsers;
                availabilities.DecreaseUser(baseTuple[0], baseTuple[2], baseTuple[3], nextRequiredUsers);
                availabilities.DecreaseUser(nextTuple[0], nextTuple[2], nextTuple[3], baseRequiredUsers);
                currentSolution.ElementAt(randTuple)[1] = nextDestination;
                currentSolution.ElementAt(randTuple)[4] = baseRequiredUsers;
                currentSolution.ElementAt(randMov)[1] = baseDestination;
                currentSolution.ElementAt(randMov)[4] = nextRequiredUsers;
            }
            return swappable;
        }

        public static int ObjectiveFunction(List<int[]> solution, Problem prob)
        {
            int sum = 0;
            foreach (var sol in solution)
                sum += (prob.Matrix.GetCost(sol[2], sol[3], sol[0], sol[1]) * sol[4]);
            return sum;
        }
    }
}
