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
        public static LinkedList<int[]> GreedySolution(Problem problem)
        {
            int totCell = problem.Matrix.Cells;
            bool[] visited = new bool[totCell];
            Random generator = new Random(1); //seed per eliminare casualità
            LinkedList<int[]> movings = new LinkedList<int[]>();
            for (int i = totCell; --i >= 0;)
            {
                int cell;
                do
                {
                    cell = generator.Next(0, totCell);
                } while (visited[cell]);
                visited[cell] = true;
                SolveTasks(cell, problem.Tasks[cell], problem, movings);
            }
            return movings;
        }

        private static void SolveTasks(int destination, int tasks, Problem problem, LinkedList<int[]> movings)
        {
            while (tasks != 0)
            {
                int[] minimum = problem.Matrix.GetMin(destination, problem.TaskPerUser, problem.Availability);
                int available = problem.Availability[minimum[0]][minimum[1]][minimum[2]];
                if (available * problem.TaskPerUser[minimum[2]] >= tasks)
                {
                    int used = (int)Math.Ceiling(tasks / (double)problem.TaskPerUser[minimum[2]]);
                    problem.Availability[minimum[0]][minimum[1]][minimum[2]] -= used;
                    movings.AddFirst(new[] { minimum[0], destination, minimum[1], minimum[2], used, tasks });
                    tasks = 0;
                }
                else
                {
                    if (available != 0)
                    {
                        problem.Availability[minimum[0]][minimum[1]][minimum[2]] -= available;
                        tasks = unchecked(tasks - (available * problem.TaskPerUser[minimum[2]]));
                        movings.AddLast(new[] { minimum[0], destination, minimum[1], minimum[2], available, unchecked(available * problem.TaskPerUser[minimum[2]]) });
                    }
                }
            }
        }



        private static void GenerateNeighborhood(LinkedList<int[]> currentSolution, Problem problem)
        {
            int randIndex = new Random().Next(currentSolution.Count);
            int[] randTuple = currentSolution.ElementAt(randIndex);
            currentSolution.Remove(randTuple);
            int remainingUsers = problem.Availability[randTuple[0]][randTuple[2]][randTuple[3]];
            int totalUsers = remainingUsers + randTuple[4];
            problem.Availability[randTuple[0]][randTuple[2]][randTuple[3]] = 0;
            SolveTasks(randTuple[1], randTuple[5], problem, currentSolution);
            problem.Availability[randTuple[0]][randTuple[2]][randTuple[3]] += totalUsers;
            //Console.WriteLine("Rimanevano " + remainingUsers + "\nUsati " + randTuple[4] + "\nAggiungo " + totalUsers);
        }

        public static int ObjectiveFunction(IEnumerable<int[]> solution, Costs matrix)
        {
            return solution.Sum(sol => (matrix.GetCost(sol[2], sol[3], sol[0], sol[1]) * sol[4]));
        }

        public static int SimulatedAnnealing(ref LinkedList<int[]> currentSolution, Problem problem, double temperature)
        {
            LinkedList<int[]> neighborSolution = currentSolution.DeepClone();
            GenerateNeighborhood(neighborSolution, problem);

            int currentFitness = ObjectiveFunction(currentSolution, problem.Matrix);
            int neighborFitness = ObjectiveFunction(neighborSolution, problem.Matrix);

            if (neighborFitness < currentFitness)
            {
                currentSolution = neighborSolution;
                return neighborFitness;
            }

            double pHat = Math.Exp((currentFitness - neighborFitness) / temperature);
            double p = new Random().NextDouble();
            if (p < pHat)
            {
                currentSolution = neighborSolution;
                return neighborFitness;
            }
            return currentFitness;
        }
    }
}