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

        private static void SolveTasks(int destination, int tasks, Costs costs, Availabilities av, int[] taskPerUser, LinkedList<int[]> movings)
        {
            while (tasks != 0)
            {
                int[] minimum = costs.GetMin(destination, taskPerUser, av);
                int available = av.GetUserNumber(minimum[0], minimum[1], minimum[2]);
                if (available * taskPerUser[minimum[2]] >= tasks)
                {
                    int used = (int)Math.Ceiling(tasks / (double)taskPerUser[minimum[2]]);
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
        }



        private static void GenerateNeighborhood(LinkedList<int[]> currentSolution, Problem problem, Availabilities newAvailabilities)
        {
            Random generator = new Random();
            int randIndex = generator.Next(currentSolution.Count);
            int[] randTuple = currentSolution.ElementAt(randIndex);
            currentSolution.Remove(randTuple);
            int remainingUsers = newAvailabilities.GetUserNumber(randTuple[0], randTuple[2], randTuple[3]);
            int totalUsers = remainingUsers + randTuple[4];
            if (totalUsers > problem.Availabilty.GetUserNumber(randTuple[0], randTuple[2], randTuple[3]))
            {
                var x = currentSolution.Where(s => s[0] == randTuple[0] && s[3] == randTuple[3]).ToList();
                var y = problem.Availabilty.GetUserNumber(randTuple[0], randTuple[2], randTuple[3]);
            }
            newAvailabilities.DecreaseUser(randTuple[0], randTuple[2], randTuple[3], remainingUsers);
            SolveTasks(randTuple[1], randTuple[5], problem.Matrix, newAvailabilities, problem.TaskPerUser, currentSolution);
            newAvailabilities.IncreaseUser(randTuple[0], randTuple[2], randTuple[3], totalUsers);
            //Console.WriteLine("Rimanevano " + remainingUsers + "\nUsati " + randTuple[4] + "\nAggiungo " + totalUsers);
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