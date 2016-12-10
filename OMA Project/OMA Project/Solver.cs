using System.Collections.Generic;
using System.Linq;
using static OMA_Project.Program;

namespace OMA_Project
{
    public class Solver
    {
        public List<int> InitialSolution()
        {
            var solution = new List<int>(500);
            var orderedTask = problem.Tasks.Select((t, c) => new {cell = c, task = t})
                .Where(t => t.task != 0).OrderBy(t => t.task).ToArray();
            int[] totalUsers = problem.TotalUsers();
            for (var i = orderedTask.Length; i-- > 0;)
                SolvePreciseTasks(solution, totalUsers, orderedTask[i].cell, orderedTask[i].task);
            return solution;
        }

        private void SolvePreciseTasks(List<int> movings, int[] totUsers, int destination, int tasks)
        {
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
                        totUsers[i] -= partitioned[i];
                        movings.Add(minimum[0]); //start
                        movings.Add(destination); //destination
                        movings.Add(minimum[1]); //timeslot
                        movings.Add(i); //usertype
                        movings.Add(partitioned[i]); //usernumber
                        movings.Add(doneTasks); //perfomed tasks
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
                        totUsers[i] -= available;
                        movings.Add(minimum[0]); //start
                        movings.Add(destination); //destination
                        movings.Add(minimum[1]); //timeslot
                        movings.Add(i); //usertype
                        movings.Add(available); //usernumber
                        movings.Add(doneTasks); //perfomed tasks
                    }
                    if (totUsers[i] == 0)
                    {
                        usable[i] = false;
                        partitioned = OptimizeSolving(tasks, usable);
                        i = partitioned.Length - 1;
                    }
                }
        }

        private void SolveTasks(List<int> movings, int destination, int tasks)
        {
            while (tasks != 0)
            {
                if (problem.Users == 0)
                    throw new NoUserLeft();
                var minimum = problem.Matrix.GetMin(destination, problem);
                var available = problem.Availability[minimum[0]][minimum[1]][minimum[2]];
                if (available*problem.TasksPerUser[minimum[2]].Tasks >= tasks)
                {
                    var used = 32768 - (int)(32768d - (tasks / (double)problem.TasksPerUser[minimum[2]].Tasks)); // shift based ceiling function (way faster than Math.Ceiling)
                    problem.Availability[minimum[0]][minimum[1]][minimum[2]] -= used;
                    problem.Users -= used;
                    movings.Add(minimum[0]); //start
                    movings.Add(destination); //destination
                    movings.Add(minimum[1]); //timeslot
                    movings.Add(minimum[2]); //usertype
                    movings.Add(used); //used users
                    movings.Add(tasks); //performed tasks
                    tasks = 0;
                }
                else
                {
                    problem.Availability[minimum[0]][minimum[1]][minimum[2]] -= available;
                    problem.Users -= available;
                    tasks = tasks - available*problem.TasksPerUser[minimum[2]].Tasks;
                    movings.Add(minimum[0]); //start
                    movings.Add(destination); //destination
                    movings.Add(minimum[1]); //timeslot
                    movings.Add(minimum[2]); //usertype
                    movings.Add(available); //used users
                    movings.Add(available * problem.TasksPerUser[minimum[2]].Tasks); //performed tasks
                }
            }
        }

        public static int ObjectiveFunction(List<int> solution)
        {
            int sum = 0;
            int times = solution.Count;
            for (int i = times; (i -= 6) >= 0;)
            {
                sum = sum + problem.Matrix.GetCost(solution[i+2], solution[i+3],
                    solution[i], solution[i+1]) * solution[i+4];

            }
            return sum;
        }

        private int[] OptimizeSolving(int tasks, IReadOnlyList<bool> usable)
        {
            var d = new int[problem.UserTypes];
            for (var i = problem.UserTypes; i-- > 0;)
                d[i] = problem.TasksPerUser[i].Tasks;

            var user = 0;
            var c = new int[tasks + 1];
            var s = new int[tasks + 1];

            for (var p = 1; p <= tasks; p++)
            {
                var min = int.MaxValue;
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
                        for (var z = problem.UserTypes; z-- > 0;)
                            tempOverBooking -= neededUsers[z]*problem.TasksPerUser[z].Tasks;
                        if (tempOverBooking >= overBooking)
                        {
                            min = tempMin;
                            user = tempUser;
                            overBooking = tempOverBooking;
                        }
                    }
                c[p] = min;
                s[p] = user;
            }
            return UsersNeeded(tasks, s);
        }

        private static int[] UsersNeeded(int tasks, IReadOnlyList<int> s)
        {
            var returns = new int[problem.UserTypes];
            for (var i = tasks; i > 0;)
            {
                returns[s[i]]++;
                i -= problem.TasksPerUser[s[i]].Tasks;
            }
            return returns;
        }

        public List<int> VNS(List<int> movings, int percentage)
        {
            var counter = movings.Count*percentage/500;
            var toBeRecomputed = new Dictionary<int, int>(counter * 10);
            var toBeDropped = new bool[movings.Count];
            for (var i = counter; i-- > 0;)
            {
                int droppedIndex;
                do
                {
                    droppedIndex = generator.Next(movings.Count / 6);
                } while (toBeDropped[droppedIndex * 6]);
                droppedIndex *= 6;
                toBeDropped[droppedIndex] = true;
                problem.Availability[movings[droppedIndex]][movings[droppedIndex+2]][movings[droppedIndex+3]] += movings[droppedIndex+4];
                problem.Users += movings[droppedIndex+4];
                if (toBeRecomputed.ContainsKey(movings[droppedIndex+1]))
                    toBeRecomputed[movings[droppedIndex+1]] += movings[droppedIndex+5];
                else
                    toBeRecomputed.Add(movings[droppedIndex+1], movings[droppedIndex+5]);
            }
            var tempList = new List<int>(movings.Count);
            for (var i =0; i < movings.Count; i+=6)
            {
                if (!toBeDropped[i])
                {
                    tempList.Add(movings[i]);
                    tempList.Add(movings[i+1]);
                    tempList.Add(movings[i+2]);
                    tempList.Add(movings[i+3]);
                    tempList.Add(movings[i+4]);
                    tempList.Add(movings[i+5]);
                }
            }
            movings = tempList;
            using (var enumerator = toBeRecomputed.GetEnumerator())
            {
                while (enumerator.MoveNext())
                    SolveTasks(movings, enumerator.Current.Key, enumerator.Current.Value);
            }
            return movings;
        }
    }
}