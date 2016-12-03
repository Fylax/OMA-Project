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
            int[] totUsers = problem.TotalUsers();
            bool[] usable = new bool[totUsers.Length];
            for (int i = totUsers.Length; i-- > 0;)
            {
                if (totUsers[i] != 0)
                {
                    usable[i] = true;
                }
            }
            int[] partitioned = OptimizeSolving(tasks, usable);
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
                    totUsers = problem.TotalUsers();
                    if (totUsers[i] == 0)
                    {
                        usable[i] = false;
                        partitioned = OptimizeSolving(tasks, usable);
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
        
        public int[] OptimizeSolving(int task, bool[] usable)
        {
            int[] d = new int[problem.TasksPerUser.Length];
            int[] av = problem.TotalUsers();
            for (int i = 0; i < 3; i++)
            {
                d[i] = problem.TasksPerUser[i].Tasks;
            }

            int a = task;
            int user = 0;
            int[] c = new int[a + 1];
            int[] s = new int[a + 1];
            c[0] = 0;
            s[0] = 0;


            for (int k = 1; k <= a; k++)
            {
                int min = int.MaxValue;
                int p = k;
                int tempMin;
                int tempUser;
                int overBooking = int.MaxValue;
                for (int j = 0; j < d.Length; j++)
                {
                    if (d[j] - d[0] < p && usable[j])
                    {
                        if (p - d[j] < 0)
                        {
                            tempMin = 1;
                            tempUser = j;
                        }

                        else if (1 + c[p - d[j]] < min)
                        {
                            tempMin = 1 + c[p - d[j]];
                            tempUser = j;
                        }

                        else break;
                        int[] neededUsers = UsersNeeded(p, s);
                        int tempOverBooking = p;
                        for (int z = 0; z < problem.TasksPerUser.Length; z++)
                        {
                            tempOverBooking -= neededUsers[z] * problem.TasksPerUser[z].Tasks;
                        }
                        tempOverBooking *= -1;
                        if (tempOverBooking <= overBooking)
                        {
                            min = tempMin;
                            user = tempUser;

                        }
                    }
                }
                c[k] = min;
                s[k] = user;

            }

            return UsersNeeded(task, s);
        }

        private int[] UsersNeeded(int tasks, int[] s)
        {
            int[] returns = new int[problem.TasksPerUser.Length];
            for (int i = tasks; i > 0;)
            {
                returns[s[i]]++;
                i -= problem.TasksPerUser[s[i]].Tasks;
            }
            return returns;
        }

    }
}
