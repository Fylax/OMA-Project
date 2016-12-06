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
                SolvePreciseTasks(orderedTask[i].cell, orderedTask[i].task, solution);
            }
            return solution;
        }

        private void SolvePreciseTasks(int destination, int tasks, Solution movings)
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
            HashSet<int[]> avoid = new HashSet<int[]>();
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

        public void VNS(Solution movings, int percentage)
        {
            Dictionary<int, int> toBeRecomputed = new Dictionary<int, int>();
            int droppedIndex;
            int counter = movings.Count * percentage / 100;
            for (int i = 0; i < counter; i++)
            {
                droppedIndex = Program.generator.Next(movings.Count);
                int[] tuple = movings.ElementAt(droppedIndex);
                tabuList.Add(tuple);
                movings.Remove(tuple);
                problem.Availability[tuple[0]][tuple[2]][tuple[3]] += tuple[4];
                if (toBeRecomputed.ContainsKey(tuple[1]))
                {
                    toBeRecomputed[tuple[1]] += tuple[5];
                }
                else
                {
                    toBeRecomputed.Add(tuple[1], tuple[5]);
                }
            }
            foreach (var tuple in toBeRecomputed)
            {
                SolveTasks(tuple.Key, tuple.Value, movings);
            }
        }

    }
}
