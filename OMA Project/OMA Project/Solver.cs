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
        /// 5. Task svolti
        /// </returns>
        public static LinkedList<int[]> GreedySolution(Problem problem, Availabilities av)
        {
            int totCell = problem.Matrix.Cells;
            int[] tasks = new int[totCell];
            bool[] visited = new bool[totCell];
            Random generator = new Random();
            LinkedList<int[]> movings = new LinkedList<int[]>();
            for (int i = totCell; --i >= 0;)
            {
                int cell;
                do
                {
                    cell = generator.Next(0, totCell);
                } while (visited[cell]);
                visited[cell] = true;
                SolveTasks(cell, problem.Tasks[cell], problem.Matrix, av, problem.TaskPerUser, movings);
            }
            return movings;
        }

        private static LinkedList<int[]> SolveTasks(int destination, int tasks, Costs costs, Availabilities av, int[] taskPerUser, LinkedList<int[]> movings)
        {
            while (tasks != 0)
            {
                int[] minimum = costs.GetMin(destination, taskPerUser, av);
                int available = av.GetUserNumber(minimum[0], minimum[1], minimum[2]);
                if (available * taskPerUser[minimum[2]] >= tasks)
                {
                    int used = (int)Math.Ceiling(unchecked(tasks / (double)taskPerUser[minimum[2]]));
                    av.DecreaseUser(minimum[0], minimum[1], minimum[2], used);
                    movings.AddFirst(new[] { minimum[0], destination, minimum[1], minimum[2], used, tasks });
                    tasks = 0;
                }
                else
                {
                    if (available != 0)
                    {
                        av.DecreaseUser(minimum[0], minimum[1], minimum[2], available);
                        tasks = unchecked(tasks - (available * taskPerUser[minimum[2]]));
                        movings.AddLast(new[] { minimum[0], destination, minimum[1], minimum[2], available, unchecked(available * taskPerUser[minimum[2]]) });
                    }
                }
            }
            return movings;
        }



        public static void GenerateNeighborhood(LinkedList<int[]> currentSolution, Problem problem, Availabilities newAvailabilities)
        {

            Random seed = new Random();
            int randTuple = seed.Next(currentSolution.Count - 1);            
            int[] droppedTuple = currentSolution.ElementAt(randTuple);
            currentSolution.Remove(droppedTuple);
            int currentlyAvailableUsers = newAvailabilities.GetUserNumber(droppedTuple[0],droppedTuple[2], droppedTuple[3]);
            newAvailabilities.DecreaseUser(droppedTuple[0], droppedTuple[2], droppedTuple[3], currentlyAvailableUsers);
            int totalAvailableUsers = currentlyAvailableUsers + droppedTuple[4];
            int tasks = droppedTuple[5];
            SolveTasks(droppedTuple[1], tasks, problem.Matrix, newAvailabilities, problem.TaskPerUser, currentSolution);
            newAvailabilities.IncreaseUser(droppedTuple[0], droppedTuple[2], droppedTuple[3], totalAvailableUsers);
            /*
            int randMov;
            do
            {
                randMov = seed.Next(currentSolution.Count - 1);
            }
            while (randMov != randTuple);

            int[] baseTuple = currentSolution.ElementAt(randTuple);
            int[] nextTuple = currentSolution.ElementAt(randMov);
            int baseTasks = baseTuple[4] * problem.TaskPerUser[baseTuple[3]];
            int nextTasks = nextTuple[4] * problem.TaskPerUser[nextTuple[3]];
            bool swappable = false;
            if (baseTasks > nextTasks)
            {
                int totalNextUsers = newAvailabilities.GetUserNumber(nextTuple[0], nextTuple[2], nextTuple[3]) + nextTuple[4];
                int totalNextTasks = totalNextUsers * problem.TaskPerUser[nextTuple[3]];
                if (totalNextTasks >= baseTasks)
                {
                    swappable = true;
                }
            }
            else if (baseTasks < nextTasks)
            {
                int totalBaseUsers = newAvailabilities.GetUserNumber(baseTuple[0], baseTuple[2], baseTuple[3]) + baseTuple[4];
                int totalBaseTasks = totalBaseUsers * problem.TaskPerUser[baseTuple[3]];
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
                newAvailabilities.IncreaseUser(baseTuple[0], baseTuple[2], baseTuple[3], baseTuple[4]);
                newAvailabilities.IncreaseUser(nextTuple[0], nextTuple[2], nextTuple[3], nextTuple[4]);
                int baseRequiredUsers = (int)Math.Ceiling(baseTasks / (double)problem.TaskPerUser[nextTuple[3]]);
                int nextRequiredUsers = (int)Math.Ceiling(nextTasks / (double)problem.TaskPerUser[baseTuple[3]]);
                newAvailabilities.DecreaseUser(baseTuple[0], baseTuple[2], baseTuple[3], nextRequiredUsers);
                newAvailabilities.DecreaseUser(nextTuple[0], nextTuple[2], nextTuple[3], baseRequiredUsers);
                currentSolution.Remove(baseTuple);
                currentSolution.Remove(nextTuple);
                baseTuple[1] = nextDestination;
                baseTuple[4] = baseRequiredUsers;
                currentSolution.AddFirst(baseTuple);
                nextTuple[1] = baseDestination;
                nextTuple[4] = nextRequiredUsers;
                currentSolution.AddLast(nextTuple);
            }
        */
        }

        public static int ObjectiveFunction(IEnumerable<int[]> solution, Costs matrix)
        {
            return solution.Sum(sol => (matrix.GetCost(sol[2], sol[3], sol[0], sol[1]) * sol[4]));
        }

        public static int SimulatedAnnealing(ref LinkedList<int[]> currentSolution, Problem problem, Availabilities newAvailabilities, double temperature)
        {
            LinkedList<int[]> neighborSolution = currentSolution.DeepClone();
            for (int i = 0; i < 5; i++)
            {
                GenerateNeighborhood(neighborSolution, problem, newAvailabilities);
            }

            int currentFitness = ObjectiveFunction(currentSolution, problem.Matrix);
            int neighborFitness = ObjectiveFunction(neighborSolution, problem.Matrix);

            if (neighborFitness < currentFitness)
            {
                currentSolution = neighborSolution;
                return neighborFitness;
            }

            double pHat =
                Math.Exp((currentFitness - neighborFitness) / temperature);
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
}