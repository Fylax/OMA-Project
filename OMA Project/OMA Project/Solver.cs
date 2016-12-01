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
            int[] partitioned = Partition(tasks);
            for (int i = partitioned.Length; i-- > 0;)
            {
                while (partitioned[i] != 0)
                {
                    int[] minimum = problem.Matrix.GetMin(destination, problem.Availability, i);
                    int available = problem.Availability[minimum[0]][minimum[1]][i];
                    if (available >= partitioned[i])
                    {
                        int used = partitioned[i];
                        problem.Availability[minimum[0]][minimum[1]][i] -= used;
                        movings.Add(new[]
                        {
                            minimum[0], destination, minimum[1], i, used
                        });
                        partitioned[i] = 0;
                    }
                    else
                    {
                        partitioned[i] -= available;
                        problem.Availability[minimum[0]][minimum[1]][i] -= available;
                        tasks = unchecked(tasks - (available * problem.TasksPerUser[i].Tasks));
                        movings.Add(new[]
                        {
                            minimum[0], destination, minimum[1], i, available,
                        });
                    }
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
                    toBeRestored[i][j] = new int[problem.TasksPerUser.Length];
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


        public int ObjectiveFunction(Solution solution)
        {
            int sum = 0;
            for (int i = solution.Count; i-- > 0;)
            {
                int costo = problem.Matrix.GetCost(solution.Movings[i][2], solution.Movings[i][3],
                    solution.Movings[i][0], solution.Movings[i][1]);
                costo = costo * (solution.Movings[i][4]);
            sum += costo;
            }
            return sum;
        }

        public int[] Partition(int toBePartitioned)
        {
            int[] returns = new int[problem.TasksPerUser.Length];
            int value = toBePartitioned;
            while (value > problem.TasksPerUser[problem.TasksPerUser.Length - 1].Tasks)
            {
                bool isDivisible = false;
                for (int i = 0; i < problem.TasksPerUser.Length && !isDivisible; ++i)
                {
                    if (problem.TasksPerUser[i].Tasks != 1 && value % problem.TasksPerUser[i].Tasks == 0)
                    {
                        isDivisible = true;
                        value -= problem.TasksPerUser[i].Tasks;
                        ++returns[i];
                    }
                }
                if (!isDivisible)
                {
                    value -= problem.TasksPerUser[2].Tasks;
                    ++returns[2];
                }
            }
            if (value != 0)
            {
                ++returns[problem.TasksPerUser.Length - 1];
            }
            return returns;
        }
    }
}