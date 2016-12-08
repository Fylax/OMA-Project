using System;
using System.Collections.Generic;
using System.Linq;

namespace OMA_Project
{
    public class Solver
    {
        private readonly Problem problem;

        public Solver(Problem prob)
        {
            problem = prob;
        }

        public List<int[]> InitialSolution()
        {
            var solution = new List<int[]>();
            var newTask = (int[]) problem.Tasks.Clone();
            var orderedTask = newTask.Select((t, c) => new {cell = c, task = t})
                .Where(t => t.task != 0).OrderBy(t => t.task).ToArray();
            for (var i = orderedTask.Length; i-- > 0;)
                SolvePreciseTasks(orderedTask[i].cell, orderedTask[i].task, solution);
            return solution;
        }

        private void SolvePreciseTasks(int destination, int tasks, List<int[]> movings)
        {
            var totUsers = problem.TotalUsers();
            var usable = new bool[totUsers.Length];
            for (var i = totUsers.Length; i-- > 0;)
                if (totUsers[i] != 0)
                    usable[i] = true;
            var partitioned = OptimizeSolving(tasks, usable);
            for (var i = partitioned.Length; i-- > 0;)
                while (partitioned[i] != 0)
                {
                    var minimum = problem.Matrix.GetMin(destination, problem.Availability, i);
                    var available = problem.Availability[minimum[0]][minimum[1]][i];
                    if (available >= partitioned[i])
                    {
                        var doneTasks = tasks < problem.TasksPerUser[i].Tasks*partitioned[i]
                            ? tasks
                            : partitioned[i]*problem.TasksPerUser[i].Tasks;
                        tasks -= partitioned[i]*problem.TasksPerUser[i].Tasks;
                        problem.Availability[minimum[0]][minimum[1]][i] -= partitioned[i];
                        problem.Users -= partitioned[i];
                        movings.Add(new[]
                        {
                            minimum[0], destination, minimum[1], i, partitioned[i], doneTasks
                        });
                        partitioned[i] = 0;
                    }
                    else
                    {
                        var doneTasks = tasks < problem.TasksPerUser[i].Tasks*partitioned[i]
                            ? tasks
                            : available*problem.TasksPerUser[i].Tasks;
                        tasks -= available*problem.TasksPerUser[i].Tasks;
                        partitioned[i] -= available;
                        problem.Availability[minimum[0]][minimum[1]][i] -= available;
                        problem.Users -= available;
                        movings.Add(new[]
                        {
                            minimum[0], destination, minimum[1], i, available, doneTasks
                        });
                    }
                    if (problem.TotalUser(i) == 0)
                    {
                        usable[i] = false;
                        partitioned = OptimizeSolving(tasks, usable);
                        i = partitioned.Length - 1;
                    }
                }
        }

        private void SolveTasks(int destination, int tasks, List<int[]> movings)
        {
            while (tasks != 0)
            {
                if (problem.Users == 0)
                    throw new NoUserLeft();
                var minimum = problem.Matrix.GetMin(destination, problem);
                var available = problem.Availability[minimum[0]][minimum[1]][minimum[2]];
                if (available*problem.TasksPerUser[minimum[2]].Tasks >= tasks)
                {
                    var used = (int) Math.Ceiling(tasks/(double) problem.TasksPerUser[minimum[2]].Tasks);
                    problem.Availability[minimum[0]][minimum[1]][minimum[2]] -= used;
                    problem.Users -= used;
                    movings.Add(new[] {minimum[0], destination, minimum[1], minimum[2], used, tasks});
                    tasks = 0;
                }
                else
                {
                    problem.Availability[minimum[0]][minimum[1]][minimum[2]] -= available;
                    problem.Users -= available;
                    tasks = tasks - available*problem.TasksPerUser[minimum[2]].Tasks;
                    movings.Add(new[]
                    {
                        minimum[0], destination, minimum[1], minimum[2], available,
                        available*problem.TasksPerUser[minimum[2]].Tasks
                    });
                }
            }
        }

        public int ObjectiveFunction(List<int[]> solution)
        {
            var sum = 0;
            for (var i = solution.Count; i-- > 0;)
                sum += problem.Matrix.GetCost(solution[i][2], solution[i][3],
                           solution[i][0], solution[i][1])*solution[i][4];
            return sum;
        }

        private int[] OptimizeSolving(int tasks, bool[] usable)
        {
            var d = new int[problem.UserTypes];
            for (var i = 0; i < 3; i++)
                d[i] = problem.TasksPerUser[i].Tasks;

            var user = 0;
            var c = new int[tasks + 1];
            var s = new int[tasks + 1];
            c[0] = 0;
            s[0] = 0;


            for (var k = 1; k <= tasks; k++)
            {
                var min = int.MaxValue;
                var p = k;
                var overBooking = int.MinValue;
                for (var j = 0; j < d.Length; j++)
                    if ((d[j] - d[0] < p) && usable[j])
                    {
                        int tempMin;
                        int tempUser;
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
                        var neededUsers = UsersNeeded(p, s);
                        var tempOverBooking = p;
                        for (var z = 0; z < problem.UserTypes; z++)
                            tempOverBooking -= neededUsers[z]*problem.TasksPerUser[z].Tasks;
                        if (tempOverBooking >= overBooking)
                        {
                            min = tempMin;
                            user = tempUser;
                            overBooking = tempOverBooking;
                        }
                    }
                c[k] = min;
                s[k] = user;
            }
            return UsersNeeded(tasks, s);
        }

        private int[] UsersNeeded(int tasks, IReadOnlyList<int> s)
        {
            var returns = new int[problem.UserTypes];
            for (var i = tasks; i > 0;)
            {
                returns[s[i]]++;
                i -= problem.TasksPerUser[s[i]].Tasks;
            }
            return returns;
        }

        public void VNS(List<int[]> movings, int percentage)
        {
            var toBeRecomputed = new Dictionary<int, int>();
            var counter = movings.Count*percentage/100;
            for (var i = 0; i < counter; i++)
            {
                var droppedIndex = Program.generator.Next(movings.Count);
                var tuple = movings.ElementAt(droppedIndex);
                movings.RemoveAt(droppedIndex);
                problem.Availability[tuple[0]][tuple[2]][tuple[3]] += tuple[4];
                problem.Users += tuple[4];
                if (toBeRecomputed.ContainsKey(tuple[1]))
                    toBeRecomputed[tuple[1]] += tuple[5];
                else
                    toBeRecomputed.Add(tuple[1], tuple[5]);
            }
            using (var enumerator = toBeRecomputed.GetEnumerator())
            {
                while (enumerator.MoveNext())
                    SolveTasks(enumerator.Current.Key, enumerator.Current.Value, movings);
            }
        }
    }
}