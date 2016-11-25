using System;
using System.Collections.Generic;
using System.Linq;
using OMA_Project.Extensions;

namespace OMA_Project
{
    public class Solver
    {
        private readonly Problem problem;

        public Solver(Problem prob)
        {
            problem = prob;
        }
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
        public LinkedList<int[]> GreedySolution()
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
                SolveTasks(cell, problem.Tasks[cell], movings);
            }
            return movings;
        }

        private void SolveTasks(int destination, int tasks, LinkedList<int[]> movings)
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

        private void GenerateNeighborhood(LinkedList<int[]> currentSolution)
        {
            lock (problem.Availability)
            {
                int randIndex = new Random().Next(currentSolution.Count);
                int[] randTuple = currentSolution.ElementAt(randIndex);
                currentSolution.Remove(randTuple);
                int remainingUsers = problem.Availability[randTuple[0]][randTuple[2]][randTuple[3]];
                int totalUsers = remainingUsers + randTuple[4];
                problem.Availability[randTuple[0]][randTuple[2]][randTuple[3]] = 0;
                SolveTasks(randTuple[1], randTuple[5], currentSolution);
                problem.Availability[randTuple[0]][randTuple[2]][randTuple[3]] += totalUsers;
            }
        }

        public int ObjectiveFunction(IEnumerable<int[]> solution)
        {
            return solution.Sum(sol => (problem.Matrix.GetCost(sol[2], sol[3], sol[0], sol[1]) * sol[4]));
        }

        public int SimulatedAnnealing(ref LinkedList<int[]> currentSolution, double temperature)
        {
            LinkedList<int[]> neighborSolution = currentSolution.DeepClone();
            int[][][] availabilities = problem.Availability.DeepClone();
            GenerateNeighborhood(neighborSolution);

            int currentFitness = ObjectiveFunction(currentSolution);
            int neighborFitness = ObjectiveFunction(neighborSolution);

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
            problem.Availability = availabilities;
            return currentFitness;
        }
    }
}