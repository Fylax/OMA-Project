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
                        int doneTasks = (tasks < problem.TasksPerUser[i].Tasks * partitioned[i])
                            ? tasks
                            : partitioned[i] * problem.TasksPerUser[i].Tasks;
                        tasks -= partitioned[i] * problem.TasksPerUser[i].Tasks;
                        problem.Availability[minimum[0]][minimum[1]][i] -= partitioned[i];
                        movings.Add(new[]
                        {
                            minimum[0], destination, minimum[1], i, partitioned[i], doneTasks
                        });
                        partitioned[i] = 0;
                    }
                    else
                    {
                        int doneTasks = (tasks < problem.TasksPerUser[i].Tasks * partitioned[i])
                            ? tasks
                            : available * problem.TasksPerUser[i].Tasks;
                        tasks -= available * problem.TasksPerUser[i].Tasks;
                        partitioned[i] -= available;
                        problem.Availability[minimum[0]][minimum[1]][i] -= available;
                        movings.Add(new[]
                        {
                            minimum[0], destination, minimum[1], i, available, doneTasks
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
                sum += (problem.Matrix.GetCost(solution.Movings[i][2], solution.Movings[i][3],
                    solution.Movings[i][0], solution.Movings[i][1]) * solution.Movings[i][4]);
            }
            return sum;
        }

        private int[] Partition(int toBePartitioned)
        {
            int[] returns = new int[problem.TasksPerUser.Length];
            int value = toBePartitioned;
            int[] available = problem.TotalUsers();
            while (value > problem.TasksPerUser[0].Tasks)
            {
                bool[] isDivisible = new bool[problem.TasksPerUser.Length];
                for (int i = problem.TasksPerUser.Length; i-- > 0;)
                {
                    if (problem.TasksPerUser[i].Tasks != 1
                        && available[i] != 0
                        && value % problem.TasksPerUser[i].Tasks == 0)
                    {
                        isDivisible[i] = true;
                    }
                }
                int tempMax = 0;
                int tempUserMax = -1;
                if (isDivisible.Any(s => s))
                {
                    for (int i = problem.TasksPerUser.Length; i-- > 0;)
                    {
                        if (isDivisible[i] && tempMax < available[i])
                        {
                            tempMax = available[i];
                            tempUserMax = i;
                        }
                    }
                }
                if (isDivisible.All(s => !s))
                {
                    /*
                    bool isAvailable = false;
                    for (int i = problem.TasksPerUser.Length; i-- > 0;)
                    {
                        isAvailable = available[i] != 0;
                        if (isAvailable && value > problem.TasksPerUser[i].Tasks)
                        {
                            --available[i];
                            value -= problem.TasksPerUser[i].Tasks;
                            ++returns[i];
                        }
                    }*/
                    tempMax = 0;
                    tempUserMax = -1;
                    for (int i = problem.TasksPerUser.Length; i-- > 0;)
                    {
                        if (tempMax < available[i])
                        {
                            tempMax = available[i];
                            tempUserMax = i;
                        }
                    }
                }
                ++returns[tempUserMax];
                --available[tempUserMax];
                value -= problem.TasksPerUser[tempUserMax].Tasks;
            }
            while (value > 0)
            {
                for (int i = 0; i < problem.TasksPerUser.Length; ++i)
                {
                    if (available[i] != 0)
                    {
                        ++returns[i];
                        value -= problem.TasksPerUser[i].Tasks;
                        --available[i];
                    }
                }
            }
            // check block
            int totalTask = problem.TasksPerUser.Select((t, i) => returns[i]*t.Tasks).Sum();
            if (totalTask != toBePartitioned)
            {
                var x = 0;
            }
            // end check block
            return returns;
        }
    }
}