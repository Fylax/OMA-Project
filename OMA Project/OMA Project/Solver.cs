﻿using System.Collections.Generic;
using System.Linq;
using static OMA_Project.Program;

namespace OMA_Project
{
    public static class Solver
    {
        public static List<int> InitialSolution()
        {
            var solution = new List<int>(600);
            var orderedTask = problem.Tasks.Select((t, c) => new {cell = c, task = t})
                .Where(t => t.task != 0).OrderBy(t => t.task).ToArray();
            var totalUsers = problem.TotalUsers();
            for (var i = orderedTask.Length; i-- > 0;)
                SolvePreciseTasks(solution, totalUsers, orderedTask[i].cell, orderedTask[i].task);
            return solution;
        }

        private static void SolvePreciseTasks(List<int> movings, int[] totUsers, int destination, int tasks)
        {
            // Optimization block (not really required, just more readability ed enforced inling)
            var costs = problem.Matrix;
            var tasksPerUser = problem.TasksPerUser;
            var availability = problem.Availability;
            var timeSlots = problem.TimeSlots;
            var userTypes = problem.UserTypes;
            // end optimization block;
            var usable = new bool[userTypes];
            for (var i = totUsers.Length; i-- > 0;)
                if (totUsers[i] != 0)
                    usable[i] = true;
            var partitioned = OptimizeSolving(tasks, usable);
            for (var i = partitioned.Length; i-- > 0;)
                while (partitioned[i] != 0)
                {
                    var minimum = costs.GetMin(destination, i);
                    var available =
                        availability[(minimum[0] * timeSlots + minimum[1]) * userTypes + i];
                    if (available >= partitioned[i])
                    {
                        var doneTasks = tasks < tasksPerUser[i].Tasks * partitioned[i]
                            ? tasks
                            : partitioned[i] * tasksPerUser[i].Tasks;
                        tasks -= partitioned[i] * tasksPerUser[i].Tasks;
                        availability[(minimum[0] * timeSlots + minimum[1]) * userTypes + i] -=
                            partitioned[i];
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
                        var doneTasks = tasks < tasksPerUser[i].Tasks * partitioned[i]
                            ? tasks
                            : available * tasksPerUser[i].Tasks;
                        tasks -= available * tasksPerUser[i].Tasks;
                        partitioned[i] -= available;
                        availability[(minimum[0] * timeSlots + minimum[1]) * userTypes + i] -=
                            available;
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

        private static void SolveTasks(List<int> movings, int destination)
        {
            // Optimization block (not really required, just more readability ed enforced inling)
            var costs = problem.Matrix;
            var tasksPerUser = problem.TasksPerUser;
            var availability = problem.Availability;
            var timeSlots = problem.TimeSlots;
            var userTypes = problem.UserTypes;
            // end optimization block;
            var tasks = problem.Tasks[destination];
            var lookup = new HashSet<int>();
            var droppable = new List<int>();
            for (var i = movings.Count; (i -= 6) >= 0;)
                if (movings[i + 1] == destination)
                {
                    lookup.Add(i);
                    tasks -= movings[i + 5];
                }
            while (tasks != 0)
            {
                if (problem.Users == 0)
                    throw new NoUserLeft();
                var minimum = costs.GetMin(destination);
                var available =
                    availability[(minimum[0] * timeSlots + minimum[1]) * userTypes + minimum[2]];
                if (available * tasksPerUser[minimum[2]].Tasks >= tasks)
                {
                    // shift based ceiling function (way faster than Math.Ceiling)
                    var used = 32768 - (int) (32768d - tasks / (double) tasksPerUser[minimum[2]].Tasks);
                    availability[(minimum[0] * timeSlots + minimum[1]) * userTypes + minimum[2]] -=
                        used;
                    problem.Users -= used;

                    var overBooking = used * tasksPerUser[minimum[2]].Tasks - tasks;
                    var solvedOverBooking = 0;
                    if (overBooking != 0 && minimum[2] != 0)
                        for (var j = minimum[2]; j-- > 0 && overBooking != 0;)
                        {
                            if (tasksPerUser[j].Tasks > overBooking)
                                continue;
                            foreach (var ptr in lookup)
                            {
                                if (movings[ptr + 3] != j) continue;
                                var toBeRemoved = overBooking / tasksPerUser[j].Tasks;
                                if (movings[ptr + 4] < toBeRemoved) continue;
                                movings[ptr + 4] -= toBeRemoved;
                                movings[ptr + 5] = movings[ptr + 4] * tasksPerUser[j].Tasks;
                                overBooking -= toBeRemoved * tasksPerUser[j].Tasks;
                                solvedOverBooking += toBeRemoved * tasksPerUser[j].Tasks;
                                availability[
                                        (movings[ptr] * timeSlots + movings[ptr + 2]) * userTypes + movings[ptr + 3]] +=
                                    toBeRemoved;
                                problem.Users += toBeRemoved;
                                if (movings[ptr + 4] == 0)
                                    droppable.Add(ptr);
                                if (overBooking == 0) break;
                            }
                        }
                    foreach (var ptr in droppable.OrderByDescending(s => s))
                        movings.RemoveRange(ptr, 6);
                    movings.Add(minimum[0]);
                    movings.Add(destination);
                    movings.Add(minimum[1]);
                    movings.Add(minimum[2]);
                    movings.Add(used);
                    movings.Add(tasks + solvedOverBooking);
                    tasks = 0;
                }
                else
                {
                    availability[(minimum[0] * timeSlots + minimum[1]) * userTypes + minimum[2]] -=
                        available;
                    problem.Users -= available;
                    tasks = tasks - available * tasksPerUser[minimum[2]].Tasks;
                    movings.Add(minimum[0]);
                    movings.Add(destination);
                    movings.Add(minimum[1]);
                    movings.Add(minimum[2]);
                    movings.Add(available);
                    movings.Add(available * tasksPerUser[minimum[2]].Tasks);
                }
            }
        }

        public static int ObjectiveFunction(List<int> solution)
        {
            // Optimization block (not really required, just more readability ed enforced inling)
            var costs = problem.Matrix;
            var cells = problem.Cells;
            var timeSlots = problem.TimeSlots;
            var userTypes = problem.UserTypes;
            // end optimization block;
            int sum;
            int j;
            if ((solution.Count * 43) >> (8 % 2) != 0) // * 43 >> 8 == / 6
            {
                sum = costs.costMatrix[
                          ((solution[0] * cells + solution[1]) * timeSlots + solution[2]) * userTypes +
                          solution[3]] * solution[4];
                j = 6;
            }
            else
            {
                sum = 0;
                j = 0;
            }
            var times = solution.Count >> 1; // >> 1 == / 2
            for (var i = solution.Count; (i -= 6) >= times; j += 6)
                if (i == j)
                    sum = sum +
                          costs.costMatrix[
                              ((solution[i] * cells + solution[i + 1]) * timeSlots + solution[i + 2]) * userTypes +
                              solution[i + 3]] * solution[i + 4];
                else
                    sum = sum +
                          costs.costMatrix[
                              ((solution[i] * cells + solution[i + 1]) * timeSlots + solution[i + 2]) * userTypes +
                              solution[i + 3]] * solution[i + 4] +
                          costs.costMatrix[
                              ((solution[j] * cells + solution[j + 1]) * timeSlots + solution[j + 2]) * userTypes +
                              solution[j + 3]] * solution[j + 4];
            return sum;
        }

        private static int[] OptimizeSolving(int tasks, IReadOnlyList<bool> usable)
        {
            // Optimization block (not really required, just more readability ed enforced inling)
            var tasksPerUser = problem.TasksPerUser;
            var userTypes = problem.UserTypes;
            // end optimization block
            var d = new int[userTypes];
            for (var i = userTypes; i-- > 0;)
                d[i] = tasksPerUser[i].Tasks;

            var user = 0;
            var c = new int[tasks + 1];
            var s = new int[tasks + 1];

            for (var p = 1; p <= tasks; p++)
            {
                var min = int.MaxValue;
                var overBooking = int.MinValue;
                for (var j = 0; j < d.Length; j++)
                    if (d[j] - d[0] < p && usable[j])
                    {
                        int tempMin;
                        int tempUser;
                        if (p - d[j] < 0)
                        {
                            tempMin = 1;
                            tempUser = tasksPerUser[j].UserType;
                        }

                        else if (1 + c[p - d[j]] < min)
                        {
                            tempMin = 1 + c[p - d[j]];
                            tempUser = tasksPerUser[j].UserType;
                        }

                        else
                        {
                            break;
                        }
                        var neededUsers = UsersNeeded(p, s);
                        var tempOverBooking = p;
                        for (var z = userTypes; z-- > 0;)
                            tempOverBooking -= neededUsers[z] * tasksPerUser[z].Tasks;
                        if (tempOverBooking < overBooking) continue;
                        min = tempMin;
                        user = tempUser;
                        overBooking = tempOverBooking;
                    }
                c[p] = min;
                s[p] = user;
            }
            return UsersNeeded(tasks, s);
        }

        private static int[] UsersNeeded(int tasks, IReadOnlyList<int> s)
        {
            var tasksPerUser = problem.TasksPerUser; // just optimization
            var returns = new int[problem.UserTypes];
            for (var i = tasks; i > 0;)
            {
                returns[s[i]]++;
                i -= tasksPerUser[s[i]].Tasks;
            }
            return returns;
        }

        public static List<int> VNS(List<int> movings, int percentage)
        {
            var numTuples = (movings.Count * 43) >> 8; // * 43 >> 8 == / 6
            var counter = (numTuples * percentage * 41) >> 12; // * 41 >> 12 == / 100
            var toBeRecomputed = new HashSet<int>();
            var toBeDropped = new bool[numTuples];
            for (var i = counter; i-- > 0;)
            {
                int droppedIndex;
                do
                {
                    droppedIndex = generator.Next(numTuples);
                } while (toBeDropped[droppedIndex]);
                toBeDropped[droppedIndex] = true;
                droppedIndex *= 6;
                problem.Availability[
                    (movings[droppedIndex] * problem.TimeSlots + movings[droppedIndex + 2]) * problem.UserTypes +
                    movings[droppedIndex + 3]] += movings[droppedIndex + 4];
                problem.Users += movings[droppedIndex + 4];
                toBeRecomputed.Add(movings[droppedIndex + 1]);
            }
            var tempList = new List<int>(movings.Capacity);
            for (var i = 0; i < movings.Count; i += 6)
            {
                if (toBeDropped[(i * 43) >> 8]) continue; // * 43 >> 8 == / 6
                tempList.Add(movings[i]);
                tempList.Add(movings[i + 1]);
                tempList.Add(movings[i + 2]);
                tempList.Add(movings[i + 3]);
                tempList.Add(movings[i + 4]);
                tempList.Add(movings[i + 5]);
            }
            movings = tempList;
            using (var enumerator = toBeRecomputed.GetEnumerator())
            {
                while (enumerator.MoveNext())
                    SolveTasks(movings, enumerator.Current);
            }
            return movings;
        }

        /// <summary>
        /// </summary>
        /// <param name="requiredUsers">
        ///     Key -> destination
        ///     Value :
        ///     0. type
        ///     1. number
        /// </param>
        /// <returns></returns>
        public static List<int> GRASP(Dictionary<int, Dictionary<int, int>> requiredUsers)
        {
            // Optimization block (not really required, just more readability ed enforced inling)
            var costs = problem.Matrix;
            var availability = problem.Availability;
            var cells = problem.Cells;
            var userTypes = problem.UserTypes;
            var timeSlots = problem.TimeSlots;
            // end optimization block
            var movings = new List<int>(600);
            var totCell = cells;
            var visited = new bool[totCell];
            var allVisited = false;
            for (var i = totCell; --i >= 0;)
            {
                int cell;
                do
                {
                    cell = generator.Next(0, totCell);
                    if (problem.Tasks[cell] != 0) continue;
                    visited[cell] = true;
                    allVisited = visited.All(v => v);
                } while (visited[cell] && !allVisited);
                if (allVisited) break;
                visited[cell] = true;
                // Clone in array
                var required = new Dictionary<int, int>(requiredUsers[cell]);
                // End clone
                var tasks = problem.Tasks[cell];
                for (var userType = userTypes; userType-- > 0;)
                    while (required.ContainsKey(userType) && required[userType] != 0)
                    {
                        if (problem.Users == 0)
                            throw new NoUserLeft();
                        var minimum = costs.GetMin(cell, userType);
                        var available =
                            availability[(minimum[0] * timeSlots + minimum[1]) * userTypes + userType];
                        if (available >= required[userType])
                        {
                            availability[(minimum[0] * timeSlots + minimum[1]) * userTypes + userType] -=
                                required[userType];
                            problem.Users -= required[userType];
                            movings.Add(minimum[0]); //start
                            movings.Add(cell); //destination
                            movings.Add(minimum[1]); //timeslot
                            movings.Add(userType); //usertype
                            movings.Add(required[userType]); //used users
                            movings.Add(tasks); //performed tasks
                            required[userType] = 0;
                        }
                        else
                        {
                            availability[(minimum[0] * timeSlots + minimum[1]) * userTypes + userType] -=
                                available;
                            problem.Users -= available;
                            tasks -= available * problem.TasksPerUser[userType].Tasks;
                            required[userType] -= available;
                            movings.Add(minimum[0]); //start
                            movings.Add(cell); //destination
                            movings.Add(minimum[1]); //timeslot
                            movings.Add(userType); //usertype
                            movings.Add(available); //used users
                            movings.Add(available * problem.TasksPerUser[userType].Tasks); //performed tasks
                        }
                    }
            }
            return movings;
        }

        public static List<int> Compactizator(List<int> movings)
        {
            var returns = new List<int>(movings.Capacity);
            var size = movings.Count;
            returns.Add(movings[size - 6]);
            returns.Add(movings[size - 5]);
            returns.Add(movings[size - 4]);
            returns.Add(movings[size - 3]);
            returns.Add(movings[size - 2]);
            returns.Add(movings[size - 1]);
            for (var i = size - 6; (i -= 6) >= 0;)
            {
                var contained = false;
                for (var j = returns.Count; (j -= 6) >= 0;)
                    if (returns[j] == movings[i] && returns[j + 1] == movings[i + 1] &&
                        returns[j + 2] == movings[i + 2] && returns[j + 3] == movings[i + 3])
                    {
                        returns[j + 4] += movings[i + 4];
                        returns[j + 5] += movings[i + 5];
                        contained = true;
                        var overBooking = returns[j + 4] * problem.TasksPerUser[returns[j + 3]].Tasks - returns[j + 5];
                        if (overBooking >= problem.TasksPerUser[returns[j + 3]].Tasks)
                        {
                            var toBeRemoved = overBooking / problem.TasksPerUser[returns[j + 3]].Tasks;
                            returns[j + 4] -= toBeRemoved;
                            problem.Availability[
                                (returns[j] * problem.TimeSlots + returns[j + 2]) * problem.UserTypes +
                                returns[j + 3]] += toBeRemoved;
                            problem.Users += toBeRemoved;
                        }
                        break;
                    }
                if (contained) continue;
                returns.Add(movings[i]);
                returns.Add(movings[i + 1]);
                returns.Add(movings[i + 2]);
                returns.Add(movings[i + 3]);
                returns.Add(movings[i + 4]);
                returns.Add(movings[i + 5]);
            }
            return returns;
        }
    }
}