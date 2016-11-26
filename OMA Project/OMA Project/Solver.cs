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

        public Solution GreedySolution()
        {
            Solution solution = new Solution(problem.Matrix.Cells);
            var newTask = (int[]) problem.Tasks.Clone();
            var orderedTask = newTask.Select((t, c) => new {cell = c, task = t})
                .Where(t => t.task != 0).OrderBy(t => t.task).ToArray();
            for (int i = orderedTask.Length; i-- > 0;)
            {
                SolveTasks(orderedTask[i].cell, orderedTask[i].task, solution);
            }
            return solution;
        }

        private void SolveTasks(int destination, int tasks, Solution movings)
        {
            while (tasks != 0)
            {
                int[] minimum = problem.Matrix.GetMin(destination, problem.TaskPerUser, problem.Availability);
                int available = problem.Availability[minimum[0]][minimum[1]][minimum[2]];
                if (available * problem.TaskPerUser[minimum[2]] >= tasks)
                {
                    int used = (int)Math.Ceiling(tasks / (double)problem.TaskPerUser[minimum[2]]);
                    problem.Availability[minimum[0]][minimum[1]][minimum[2]] -= used;
                    movings.Add(new[] { minimum[0], destination, minimum[1], minimum[2], used, tasks });
                    tasks = 0;
                }
                else
                {
                    problem.Availability[minimum[0]][minimum[1]][minimum[2]] -= available;
                    tasks = unchecked(tasks - (available * problem.TaskPerUser[minimum[2]]));
                    movings.Add(new[] { minimum[0], destination, minimum[1], minimum[2], available, unchecked(available * problem.TaskPerUser[minimum[2]]) });
                }
            }
        }

        private void GenerateNeighborhood(Solution currentSolution)
        {
            int[][] max = currentSolution.MovingsToRandomCell();
            currentSolution.RemoveCell(max[0][1]);
            int[][][] toBeRestored = new int[problem.Matrix.Cells][][];
            int timeSlots = problem.Availability[0].Length;
            int userTypes = problem.TaskPerUser.Length;
            for (int i = toBeRestored.Length; i-- > 0;)
            {
                toBeRestored[i] = new int[timeSlots][];
                for (int j = timeSlots; j-- > 0;)
                {
                    toBeRestored[i][j] = new int[userTypes];
                }
            }
            for (int i = max.Length; i-- > 0;)
            {
                int source = max[i][0];
                int timeSlot = max[i][2];
                int userType = max[i][3];
                toBeRestored[source][timeSlot][userType] = unchecked(
                    toBeRestored[source][timeSlot][userType] + problem.Availability[source][timeSlot][userType]);
                problem.Availability[source][timeSlot][timeSlot] = 0;
            }
            SolveTasks(max[0][1], problem.Tasks[max[0][1]], currentSolution);
            for (int i = max.Length; i-- > 0;)
            {
                int source = max[i][0];
                int timeSlot = max[i][2];
                int userType = max[i][3];
                problem.Availability[source][timeSlot][userType] = toBeRestored[source][timeSlot][userType];
            }
        }

        public int ObjectiveFunction(Solution solution)
        {
            int sum = 0;
            for (int i = solution.Count; i-- > 0;)
                sum = unchecked(sum + problem.Matrix.GetCost(solution.Movings[i][2], solution.Movings[i][3],
                    solution.Movings[i][0], solution.Movings[i][1]) * solution.Movings[i][4]);
            return sum;
        }

        public int SimulatedAnnealing(ref Solution currentSolution, double temperature)
        {
            Solution neighborSolution = currentSolution.Clone();
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