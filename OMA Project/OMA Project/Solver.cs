using System;
using System.Diagnostics;
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
            var newTask = (int[])problem.Tasks.Clone();
            var orderedTask = newTask.Select((t, c) => new { cell = c, task = t })
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

        private void DropNeighborhood(Solution currentSolution, bool max)
        {
            int[][] solutionsToCell;
            if (max)
            {
                solutionsToCell = currentSolution.MovementsMaxDestination();
                currentSolution.RemoveMax();
            }
            else
            {
                solutionsToCell = currentSolution.MovingsToRandomCell();
                currentSolution.RemoveCell(solutionsToCell[0][1]);
            }

            int[][][] toBeRestored = new int[problem.Matrix.Cells][][];
            int timeSlots = problem.Availability[0].Length;
            for (int i = toBeRestored.Length; i-- > 0;)
            {
                toBeRestored[i] = new int[timeSlots][];
                for (int j = timeSlots; j-- > 0;)
                {
                    toBeRestored[i][j] = new int[problem.TaskPerUser.Length];
                }
            }
            for (int i = solutionsToCell.Length; i-- > 0;)
            {
                int source = solutionsToCell[i][0];
                int timeSlot = solutionsToCell[i][2];
                int userType = solutionsToCell[i][3];
                toBeRestored[source][timeSlot][userType] = unchecked(
                    solutionsToCell[i][4] + problem.Availability[source][timeSlot][userType]);
                problem.Availability[source][timeSlot][timeSlot] = 0;
            }
            SolveTasks(solutionsToCell[0][1], problem.Tasks[solutionsToCell[0][1]], currentSolution);
            for (int i = solutionsToCell.Length; i-- > 0;)
            {
                int source = solutionsToCell[i][0];
                int timeSlot = solutionsToCell[i][2];
                int userType = solutionsToCell[i][3];
                problem.Availability[source][timeSlot][userType] = toBeRestored[source][timeSlot][userType];
            }
        }

        private void SwapNeighborhood(Solution currentSolution)
        {
            int[][] destinationMovings = currentSolution.MovingsToRandomCell();
            int destination = destinationMovings[0][1];
            bool found = false;
            int delta = int.MaxValue;
            int tempDelta;
            int[] proposedDestinationMoving = null;
            int[] proposedSourceMoving = null;
            int offset = 1;
            do
            {
                if (destination + offset < problem.Matrix.Cells &&
                    currentSolution.MovingsFromSource[destination + offset] != 0)
                {
                    int[][] sourceMovings = currentSolution.MovingsFromCell(destination + offset);
                    for (int i = destinationMovings.Length; i-- > 0;)
                    {
                        for (int j = sourceMovings.Length; j-- > 0;)
                        {
                            if (sourceMovings[j][1] != destination)
                            {
                                tempDelta = (destinationMovings[i][5] > sourceMovings[j][5])
                                    ? destinationMovings[i][5] - sourceMovings[j][5]
                                    : sourceMovings[j][5] - destinationMovings[i][5];
                                if (tempDelta < delta)
                                {
                                    proposedDestinationMoving = destinationMovings[i];
                                    proposedSourceMoving = sourceMovings[j];
                                    delta = tempDelta;
                                    if (delta == 0)
                                    {
                                        goto swap;
                                    }
                                }
                                found = true;
                            }
                        }
                    }
                }
                if (destination - offset < 0)
                {
                    ++offset;
                }
                else if (offset > 0)
                {
                    offset *= -1;
                }
                else
                {
                    offset *= -1;
                    ++offset;
                }
            } while (!found);
            swap:
            int[] max;
            int[] min;
            if (proposedSourceMoving[5] > proposedDestinationMoving[5])
            {
                max = proposedSourceMoving;
                min = proposedDestinationMoving;
            }
            else
            {
                max = proposedDestinationMoving;
                min = proposedSourceMoving;
            }
            currentSolution.Remove(min);
            currentSolution.Remove(max);
            int minDestination = min[1];
            int minTask = min[5];
            min[1] = max[1];
            int requiredUsers = (int)Math.Ceiling((max[5] - min[4] * problem.TaskPerUser[min[3]]) / (double)problem.TaskPerUser[min[3]]);
            int available = problem.Availability[min[0]][min[2]][min[3]];
            if (available >= requiredUsers)
            {
                problem.Availability[min[0]][min[2]][min[3]] -= requiredUsers;
                min[4] += requiredUsers;
                min[5] = max[5];
            }
            else
            {
                min[4] += available;
                min[5] += available * problem.TaskPerUser[min[3]];
                problem.Availability[min[0]][min[2]][min[3]] = 0;
                SolveTasks(min[1], max[5] - min[5], currentSolution);
            }
            max[1] = minDestination;
            int exceedingUsers = (max[4] * problem.TaskPerUser[max[3]] - minTask) / problem.TaskPerUser[max[3]];
            max[5] = minTask;
            problem.Availability[max[0]][max[2]][max[3]] += exceedingUsers;
            max[4] -= exceedingUsers;
            currentSolution.Add(min);
            currentSolution.Add(max);
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
            switch (Program.generator.Next(3))
            {
                case 0: DropNeighborhood(neighborSolution, true); break;
                case 1: DropNeighborhood(neighborSolution, false); break;
                case 2: SwapNeighborhood(neighborSolution); break;
            }

            int currentFitness = ObjectiveFunction(currentSolution);
            int neighborFitness = ObjectiveFunction(neighborSolution);

            if (neighborFitness < currentFitness)
            {
                currentSolution = neighborSolution;
                return neighborFitness;
            }

            double pHat = Math.Exp((currentFitness - neighborFitness) / temperature);
            double p = Program.generator.NextDouble();
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