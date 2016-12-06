using System;
using System.Collections.Generic;
using System.Linq;

namespace OMA_Project
{
    public class Solver
    {
        private readonly Problem problem;
        private readonly TabuList tabuList;

        public Solver(Problem prob)
        {
            problem = prob;
            tabuList = new TabuList();
        }

        public Solution InitialSolution()
        {
            Solution solution = new Solution(problem.Cells);
            var newTask = (int[])problem.Tasks.Clone();
            var orderedTask = newTask.Select((t, c) => new { cell = c, task = t })
                .Where(t => t.task != 0).OrderBy(t => t.task).ToArray();
            for (int i = orderedTask.Length; i-- > 0;)
            {
                SolveTasks(orderedTask[i].cell, orderedTask[i].task, solution);

                int[] totUsers = problem.TotalUsers();
                bool[] usable = new bool[totUsers.Length];
                for (int k = totUsers.Length; k-- > 0;)
                {
                    if (totUsers[k] != 0)
                    {
                        usable[k] = true;
                    }
                }
                int[] partitioned = OptimizeSolving(orderedTask[i].task, usable);
                for (int j = partitioned.Length; j-- > 0;)
                {
                    while (partitioned[j] != 0)
                    {
                        int[] minimum = problem.Matrix.GetMin(orderedTask[j].cell, problem.Availability, j);
                        int available = problem.Availability[minimum[0]][minimum[1]][j];
                        int tasks = orderedTask[j].task;
                        if (available >= partitioned[j])
                        {
                            int doneTasks = (orderedTask[j].task < problem.TasksPerUser[j].Tasks * partitioned[j])
                                ? orderedTask[j].task
                                : partitioned[j] * problem.TasksPerUser[i].Tasks;
                            tasks -= partitioned[j] * problem.TasksPerUser[j].Tasks;
                            problem.Availability[minimum[0]][minimum[1]][j] -= partitioned[j];
                            solution.Add(new[]
                            {
                            minimum[0], orderedTask[j].cell, minimum[1], j, partitioned[j], doneTasks
                        });
                            partitioned[j] = 0;
                        }
                        else
                        {
                            int doneTasks = (orderedTask[j].task < problem.TasksPerUser[j].Tasks * partitioned[j])
                                ? orderedTask[j].task
                                : available * problem.TasksPerUser[j].Tasks;
                            tasks -= available * problem.TasksPerUser[j].Tasks;
                            partitioned[j] -= available;
                            problem.Availability[minimum[0]][minimum[1]][j] -= available;
                            solution.Add(new[]
                            {
                            minimum[0], orderedTask[j].cell, minimum[1], j, available, doneTasks
                        });
                        }
                        totUsers = problem.TotalUsers();
                        if (totUsers[j] == 0)
                        {
                            usable[j] = false;
                            partitioned = OptimizeSolving(orderedTask[j].task, usable);
                        }
                    }
                }
            }
            return solution;
        }

        public Solution GreedySolution()
        {
            Solution solution = new Solution(problem.Cells);
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
            List<int[]> avoid = new List<int[]>();
            foreach (var m in tabuList.List)
            {
                if (m[1] == destination)
                {
                    avoid.Add(new[] { m[0], m[2], m[3] });
                }
            }
            while (tasks != 0)
            {
                int[] minimum = problem.Matrix.GetMin(destination, problem.TasksPerUser, problem.Availability, avoid);
                int available = problem.Availability[minimum[0]][minimum[1]][minimum[2]];
                if (available * problem.TasksPerUser[minimum[2]].Tasks >= tasks)
                {
                    int used = (int)Math.Ceiling(tasks / (double)problem.TasksPerUser[minimum[2]].Tasks);
                    problem.Availability[minimum[0]][minimum[1]][minimum[2]] -= used;
                    movings.Add(new[] { minimum[0], destination, minimum[1], minimum[2], used, tasks });
                    tasks = 0;
                }
                else
                {
                    problem.Availability[minimum[0]][minimum[1]][minimum[2]] -= available;
                    tasks = tasks - (available * problem.TasksPerUser[minimum[2]].Tasks);
                    movings.Add(new[] { minimum[0], destination, minimum[1], minimum[2], available, available * problem.TasksPerUser[minimum[2]].Tasks });
                }
            }
        }
        /*
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

            int[][][] toBeRestored = new int[problem.Cells][][];
            int timeSlots = problem.Availability[0].Length;
            for (int i = toBeRestored.Length; i-- > 0;)
            {
                toBeRestored[i] = new int[timeSlots][];
                for (int j = timeSlots; j-- > 0;)
                {
                    toBeRestored[i][j] = new int[problem.UserTypes];
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
        */
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

        public int[] OptimizeSolving(int tasks, bool[] usable)
        {
            int[] d = new int[problem.UserTypes];
            for (int i = 0; i < 3; i++)
            {
                d[i] = problem.TasksPerUser[i].Tasks;
            }

            int user = 0;
            int[] c = new int[tasks + 1];
            int[] s = new int[tasks + 1];
            c[0] = 0;
            s[0] = 0;


            for (int k = 1; k <= tasks; k++)
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
                            tempUser = problem.TasksPerUser[j].UserType;
                        }

                        else if (1 + c[p - d[j]] < min)
                        {
                            tempMin = 1 + c[p - d[j]];
                            tempUser = problem.TasksPerUser[j].UserType;
                        }

                        else break;
                        int[] neededUsers = UsersNeeded(p, s);
                        int tempOverBooking = p;
                        for (int z = 0; z < problem.UserTypes; z++)
                        {
                            tempOverBooking -= neededUsers[z] * problem.TasksPerUser[z].Tasks;
                        }
                        tempOverBooking *= -1;
                        if (tempOverBooking <= overBooking)
                        {
                            min = tempMin;
                            user = tempUser;
                            overBooking = tempOverBooking;
                        }
                    }
                }
                c[k] = min;
                s[k] = user;

            }

            return UsersNeeded(tasks, s);
        }

        private int[] UsersNeeded(int tasks, IReadOnlyList<int> s)
        {
            int[] returns = new int[problem.UserTypes];
            for (int i = tasks; i > 0;)
            {
                returns[s[i]]++;
                i -= problem.TasksPerUser[s[i]].Tasks;
            }
            return returns;
        }

        public void VNS(Solution movings, int counter)
        {
            int droppedIndex;
            for (int i = 0; i < counter; i++)
            {
                droppedIndex = Program.generator.Next(movings.Count);
                tabuList.Add(movings.ElementAt(droppedIndex));
                movings.RemoveAt(droppedIndex);
            }
        }

    }
}
