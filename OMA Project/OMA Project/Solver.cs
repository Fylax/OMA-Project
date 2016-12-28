using System.Collections.Generic;
using System.Linq;
using OMA_Project.Extensions;
using static OMA_Project.Program;

namespace OMA_Project
{
    /// <summary>
    ///     Static class that exposes all the methods to profide feasible solution (if any)
    ///     and try to obtain the optimal one.
    /// </summary>
    public static class Solver
    {
        /// <summary>
        ///     Computes the initial solution, generated
        ///     through the Change Making Problem.
        /// </summary>
        /// <returns>
        ///     Initial solution in a flattened array,
        ///     each tuple is 6-element long, with:
        ///     <list type="number">
        ///         <item>Start</item>
        ///         <item>Destination</item>
        ///         <item>Time slot</item>
        ///         <item>User type</item>
        ///         <item>Required users</item>
        ///         <item>Performed tasks</item>
        ///     </list>
        /// </returns>
        public static List<int> InitialSolution()
        {
            var solution = new List<int>(600);
            try
            {
                var orderedTask = problem.Tasks.Select((t, c) => new {cell = c, task = t})
                    .Where(t => t.task != 0).OrderBy(t => t.task).ToArray();
                var totalUsers = problem.TotalUsers();
                for (var i = orderedTask.Length; i-- > 0;)
                    SolvePreciseTasks(solution, totalUsers, orderedTask[i].cell, orderedTask[i].task);
            }
            catch (NoUserLeft)
            {
                problem.Availability = problem.ImmutableAvailability.DeepClone();
                solution.Clear();
                problem.Users = problem.ImmutableUsers;
                var orderedTask = problem.Tasks.Select((t, c) => new { cell = c, task = t })
                    .Where(t => t.task != 0).OrderByDescending(t => t.task).ToArray();
                var totalUsers = problem.TotalUsers();
                for (var i = orderedTask.Length; i-- > 0;)
                    SolvePreciseTasks(solution, totalUsers, orderedTask[i].cell, orderedTask[i].task);
            }
            return solution;
        }

        /// <summary>
        ///     Solves tasks in a deterministic way (through the Change Making Problem)
        /// </summary>
        /// <param name="movings">Current solution which will be updated</param>
        /// <param name="totUsers">Total of available user for each type</param>
        /// <param name="destination">Requested destination</param>
        /// <param name="tasks">Tasks that must to be accomplished</param>
        private static void SolvePreciseTasks(List<int> movings, int[] totUsers, int destination, int tasks)
        {
            // Optimization block (not really required, just more readability ed enforced inling)
            var costs = problem.Matrix;
            var tasksPerUser = problem.TasksPerUser;
            var availability = problem.Availability;
            var timeSlots = problem.TimeSlots;
            var userTypes = problem.UserTypes;
            var baseAv = timeSlots * userTypes;
            // end optimization block;
            var usable = new bool[userTypes];
            for (var i = totUsers.Length; i-- > 0;)
                if (totUsers[i] != 0)
                    usable[i] = true;
            var partitioned = OptimizeSolving(tasks, usable);
            for (var i = partitioned.Length; i-- > 0;)
                while (partitioned[i] != 0)
                {
                    if (problem.Users == 0)
                        throw new NoUserLeft();
                    var minimum = costs.GetMin(destination, i);
                    var avIndex = minimum[0] * baseAv + minimum[1] * userTypes + i;
                    var available = availability[avIndex];
                    if (available >= partitioned[i])
                    {
                        var doneTasks = tasks < tasksPerUser[i].Tasks * partitioned[i]
                            ? tasks
                            : partitioned[i] * tasksPerUser[i].Tasks;
                        tasks -= partitioned[i] * tasksPerUser[i].Tasks;
                        availability[avIndex] -= partitioned[i];
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
                        availability[avIndex] -= available;
                        problem.Users -= available;
                        totUsers[i] -= available;
                        movings.Add(minimum[0]); //start
                        movings.Add(destination); //destination
                        movings.Add(minimum[1]); //timeslot
                        movings.Add(i); //usertype
                        movings.Add(available); //usernumber
                        movings.Add(doneTasks); //perfomed tasks
                    }
                    if (totUsers[i] != 0) continue;
                    usable[i] = false;
                    partitioned = OptimizeSolving(tasks, usable);
                    i = partitioned.Length - 1;
                }
        }

        /// <summary>
        ///     Implementation of Change Making Problem (modified version).
        ///     <para />
        ///     Differently from the original problem it both minimizes required users
        ///     and eventual overbooking (difference between performable task by an user
        ///     and actually performed tasks).
        ///     <para />
        ///     Futhermore it takes into account availability of users, skipping the ones that
        ///     aren't available.
        /// </summary>
        /// <param name="tasks">Tasks that must to be splitted between users</param>
        /// <param name="usable">
        ///     Wheter users of each type are usable or not (which
        ///     can be traslated in "there are user left of this type)
        /// </param>
        /// <returns>Array containing required users for each type.</returns>
        private static int[] OptimizeSolving(int tasks, bool[] usable)
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

        /// <summary>
        ///     Computes the user required to partition a task.
        /// </summary>
        /// <param name="tasks">Tasks to be splitted</param>
        /// <param name="s">s vector of Change Making Problem</param>
        /// <returns>Number of users required for each type</returns>
        private static int[] UsersNeeded(int tasks, int[] s)
        {
            var tasksPerUser = problem.TasksPerUser; // just optimization
            var returns = new int[problem.UserTypes];
            while (tasks > 0)
            {
                returns[s[tasks]]++;
                tasks -= tasksPerUser[s[tasks]].Tasks;
            }
            return returns;
        }

        /// <summary>
        ///     Solves tasks in a greedy way (always take the minimum cost bestween the
        ///     available users).
        /// </summary>
        /// <param name="movings">Current solution that will be updated</param>
        /// <param name="destination">Requested destination</param>
        /// <exception cref="NoUserLeft">
        ///     Underlying greedy used all available users
        ///     while it need more of them.
        /// </exception>
        private static void SolveTasks(List<int> movings, int destination)
        {
            // Optimization block (not really required, just more readability ed enforced inling)
            var costs = problem.Matrix;
            var tasksPerUser = problem.TasksPerUser;
            var availability = problem.Availability;
            var userTypes = problem.UserTypes;
            var baseAv = problem.AvailabilityBaseIndex;
            // end optimization block;
            var tasks = problem.Tasks[destination];
            var lookup = new HashSet<int>();
            var droppable = new List<int>();
            int j;
            if (movings.Count / 6 % 2 != 0)
            {
                j = 6;
                if (movings[1] == destination)
                {
                    tasks -= movings[5];
                    lookup.Add(0);
                }
            }
            else
            {
                j = 0;
            }
            var times = movings.Count / 2;
            for (var i = movings.Count; (i -= 6) >= times; j += 6)
                if (i == j)
                {
                    if (movings[i + 1] == destination)
                    {
                        tasks -= movings[i + 5];
                        lookup.Add(i);
                    }
                }
                else
                {
                    if (movings[i + 1] == destination)
                    {
                        tasks -= movings[i + 5];
                        lookup.Add(i);
                    }
                    if (movings[j + 1] == destination)
                    {
                        tasks -= movings[j + 5];
                        lookup.Add(j);
                    }
                }
            while (tasks != 0)
            {
                if (problem.Users == 0)
                    throw new NoUserLeft();
                var minimum = costs.GetMin(destination);
                var avIndex = minimum[0] * baseAv + minimum[1] * userTypes + minimum[2];
                var available = availability[avIndex];
                int used;
                int performedTasks;
                if (available * tasksPerUser[minimum[2]].Tasks >= tasks)
                {
                    // shift based ceiling function (way faster than Math.Ceiling)
                    used = 32768 - (int) (32768d - tasks / (double) tasksPerUser[minimum[2]].Tasks);
                    performedTasks = tasks;
                    tasks = 0;
                }
                else
                {
                    used = available;
                    tasks -= used * tasksPerUser[minimum[2]].Tasks;
                    performedTasks = used * tasksPerUser[minimum[2]].Tasks;
                }
                availability[avIndex] -= used;
                problem.Users -= used;
                var currentPtr = -1;
                //compactizator
                using (var enumerator = lookup.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        var ptr = enumerator.Current;
                        if (movings[ptr] == minimum[0] && movings[ptr + 2] == minimum[1] &&
                            movings[ptr + 3] == minimum[2])
                        {
                            movings[ptr + 4] += used;
                            movings[ptr + 5] += performedTasks;
                            currentPtr = ptr;
                            var overbooking = movings[ptr + 4] * tasksPerUser[movings[ptr + 3]].Tasks -
                                              movings[ptr + 5];
                            if (overbooking >= tasksPerUser[movings[ptr + 3]].Tasks)
                            {
                                var toBeRemoved = overbooking / tasksPerUser[movings[ptr + 3]].Tasks;
                                movings[ptr + 4] -= toBeRemoved;
                                if (movings[ptr + 4] == 0)
                                    droppable.Add(movings[ptr]);
                                availability[movings[ptr] * baseAv + movings[ptr + 2] * userTypes + movings[ptr + 3]] +=
                                    toBeRemoved;
                                problem.Users += toBeRemoved;
                            }
                            break;
                        }
                    }
                }
                if (currentPtr == -1)
                {
                    currentPtr = movings.Count;
                    movings.Add(minimum[0]);
                    movings.Add(destination);
                    movings.Add(minimum[1]);
                    movings.Add(minimum[2]);
                    movings.Add(used);
                    movings.Add(performedTasks);
                }

                // sweeper!
                var overBooking = used * tasksPerUser[minimum[2]].Tasks - movings[currentPtr + 5];
                for (var i = userTypes; i-- > 0 && overBooking != 0;)
                    using (var enumerator = lookup.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            var ptr = enumerator.Current;
                            if (ptr == currentPtr) continue;
                            if (overBooking == 0) break;
                            if (movings[ptr + 3] != i) continue;
                            var lastPerformed = tasksPerUser[i].Tasks * (1 - movings[ptr + 4]) + movings[ptr + 5];
                            if (lastPerformed > overBooking) continue;
                            overBooking -= lastPerformed;
                            movings[ptr + 4]--;
                            movings[currentPtr + 5] += lastPerformed;
                            availability[movings[ptr] * baseAv + movings[ptr + 2] * userTypes + movings[ptr + 3]]++;
                            problem.Users++;
                            if (movings[ptr + 4] != 0)
                                movings[ptr + 5] -= lastPerformed;
                            else
                                droppable.Add(ptr);
                        }
                    }
                if (droppable.Count == 0) continue;
                foreach (var ptr in droppable.OrderByDescending(s => s))
                    movings.RemoveRange(ptr, 6);
            }
        }

        /// <summary>
        ///     Computes the objective function.
        /// </summary>
        /// <param name="solution"></param>
        /// <returns>Value of objective function</returns>
        public static int ObjectiveFunction(List<int> solution)
        {
            // Optimization block (not really required, just more readability ed enforced inling)
            var costs = problem.Matrix;
            var userTypes = problem.UserTypes;
            var baseDest = problem.TimeSlots * userTypes;
            var baseStart = baseDest * problem.Cells;
            // end optimization block;
            int sum;
            int j;
            if (solution.Count / 6 % 2 != 0)
            {
                sum =
                    costs.costMatrix[
                        solution[0] * baseStart + solution[1] * baseDest + solution[2] * userTypes + solution[3]] *
                    solution[4];
                j = 6;
            }
            else
            {
                sum = 0;
                j = 0;
            }
            var times = solution.Count / 2;
            for (var i = solution.Count; (i -= 6) >= times; j += 6)
                if (i == j)
                    sum = sum +
                          costs.costMatrix[
                              solution[i] * baseStart + solution[i + 1] * baseDest + solution[i + 2] * userTypes +
                              solution[i + 3]] * solution[i + 4];
                else
                    sum = sum +
                          costs.costMatrix[
                              solution[i] * baseStart + solution[i + 1] * baseDest + solution[i + 2] * userTypes +
                              solution[i + 3]] * solution[i + 4] +
                          costs.costMatrix[
                              solution[j] * baseStart + solution[j + 1] * baseDest + solution[j + 2] * userTypes +
                              solution[j + 3]] * solution[j + 4];
            return sum;
        }

        /// <summary>
        ///     Computes the Variable Neighborhood Search Metaheuristic.
        ///     <para />
        ///     Neighborhood is generated by dropping some random tuples from the
        ///     solution table and then recompute them through a greedy.
        ///     <seealso cref="SolveTasks" />
        /// </summary>
        /// <param name="movings">Current solution from which tuples will be dropped.</param>
        /// <param name="percentage">Percentage of tuples to be dropped (as integer). This means that 1 is 1%.</param>
        /// <returns>
        ///     Solution processed by the VNS.
        /// </returns>
        public static List<int> VNS(List<int> movings, int percentage)
        {
            // Optimization block (not really required, just more readability ed enforced inling)
            var userTypes = problem.UserTypes;
            var baseAv = problem.TimeSlots * userTypes;
            // end optimization block
            var numTuples = movings.Count / 6;
            var counter = numTuples * percentage / 100;
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
                        movings[droppedIndex] * baseAv + movings[droppedIndex + 2] * userTypes + movings[droppedIndex + 3]]
                    += movings[droppedIndex + 4];
                problem.Users += movings[droppedIndex + 4];
                toBeRecomputed.Add(movings[droppedIndex + 1]);
            }
            var tempList = new List<int>(movings.Capacity);
            for (var i = 0; i < numTuples; i++)
            {
                if (toBeDropped[i]) continue;
                var offset = i * 6;
                tempList.Add(movings[offset]);
                tempList.Add(movings[offset + 1]);
                tempList.Add(movings[offset + 2]);
                tempList.Add(movings[offset + 3]);
                tempList.Add(movings[offset + 4]);
                tempList.Add(movings[offset + 5]);
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
        ///     Computes the Greedy Randomized Adaptive Search Procedure Methaheuristic.
        ///     <para />
        ///     Whereas the number of required users for each type and each cell is computed
        ///     in a deterministic way (through Change Making Problem), order of cells from
        ///     where compute the greedy is taken randomly
        /// </summary>
        /// <param name="requiredUsers">
        ///     Tree of required users:
        ///     <list type="bullet">
        ///         <item>Key: Destination cell</item>
        ///         <item>
        ///             <list type="bullet">
        ///                 <item>Key: User type</item>
        ///                 <item>Value: Number of required users</item>
        ///             </list>
        ///         </item>
        ///     </list>
        /// </param>
        /// <returns>Solution (if any) computed through greedy</returns>
        /// <exception cref="NoUserLeft">
        ///     Current GRASP used more user of
        ///     the available ones.
        /// </exception>
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
    }
}