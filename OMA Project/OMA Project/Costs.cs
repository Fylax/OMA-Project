﻿using System.Threading.Tasks;
using static OMA_Project.Program;

namespace OMA_Project
{
    /// <summary>
    ///     Class which stores the whole cost's matrix in an efficient way,
    ///     exposing methods to retrieve the minimum cost to move
    ///     users from a starting cell to a destination one.
    /// </summary>
    public class Costs
    {
        /// <summary>
        ///     Costs Matrix. Flattened 4 dimensions matrix with, in order:
        ///     <list type="number">
        ///         <item>Start cell</item>
        ///         <item>Destination cell</item>
        ///         <item>Time slot</item>
        ///         <item>User type</item>
        ///     </list>
        /// </summary>
        /// <remarks>
        ///     As the array has been flattened, in order to compute the 4D indices, following
        ///     formula must be used:
        ///     <c>
        ///         ((Start * NumberOfCells + Destination) * NumberOfTimeSlots + CurrentTimeSlot) *
        ///         NumberOfUserTypes + CurrentUserType
        ///     </c>
        ///     .
        ///     <para />
        ///     This is an immutable property, number of tasks
        ///     must <b>never</b> be changed.
        /// </remarks>
        public readonly int[] costMatrix;

        /// <summary>
        ///     Internal object for multi-threading locking.
        /// </summary>
        private readonly object sync = new object();

        /// <summary>
        ///     As provided instances all have either 30, 100 or 300 cells,
        ///     it is safe to  check if the number of cells is even,
        ///     this allows the <see cref="GetMin(int, int)" /> method to perform
        ///     a loop unrolling.
        /// </summary>
        /// <remarks>
        ///     This would allow loop unrolling either on <see cref="GetMin(int)" />,
        ///     but would make the code too hard to mantain.
        ///     <para />
        ///     It is may be possible to provide a different unrollable
        ///     condition (like divisible by 5), but again, would make
        ///     code too hard to mantain.
        /// </remarks>
        private readonly bool unrollableCells;

        /// <summary>
        ///     As provided instances all have three user types, it is safe to
        ///     check if the number of users is three, this allows
        ///     the <see cref="GetMin(int)" /> method to perform a
        ///     loop unrolling.
        /// </summary>
        private readonly bool unrollableUser;

        /// <summary>
        /// Precomputed offset for start cell.
        /// </summary>
        private readonly int baseStart;

        /// <summary>
        ///     Initializes a new <see cref="Costs" /> matrix (performing checks on
        ///     unrollability conditions).
        ///     <para />
        ///     See <see cref="unrollableCells" /> and <see cref="unrollableUser" /> for informations
        ///     about loop unrolling and how it is managed.
        /// </summary>
        /// <param name="numCells">Number of cells</param>
        /// <param name="timeSlots">Number of time slots</param>
        /// <param name="userTypes">Number of user types</param>
        public Costs(int numCells, int timeSlots, int userTypes)
        {
            costMatrix = new int[numCells * numCells * timeSlots * userTypes];
            unrollableUser = userTypes == 3;
            unrollableCells = numCells % 2 == 0;
            baseStart = numCells * timeSlots * userTypes;
        }

        /// <summary>
        ///     Adds a cost matrix to the total cost matrix
        /// </summary>
        /// <param name="matrix">Matrix to be added</param>
        /// <param name="timeSlot">Current time slot</param>
        /// <param name="userType">Current user type</param>
        /// <param name="cells">Number of cells</param>
        /// <param name="timeSlots">Number of time slots</param>
        /// <param name="userTypes">Number of user types</param>
        public void AddMatrix(int[][] matrix, int timeSlot, int userType, int cells, int timeSlots, int userTypes)
        {
            for (var start = 0; start < cells; ++start)
                for (var dest = 0; dest < cells; ++dest)
                    costMatrix[((start * cells + dest) * timeSlots + timeSlot) * userTypes + userType] = matrix[start][dest];
        }

        /// <summary>
        ///     Retrieves the minimum cost for a given couple of fixed destination cell and user type.
        /// </summary>
        /// <param name="destination">Requested destination cell</param>
        /// <param name="userType">Requested user type</param>
        /// <returns>
        ///     Minimum cost for a given values in a 2 element array,
        ///     where indicex has following meanings:
        ///     <list type="number">
        ///         <item>Starting cell</item>
        ///         <item>Time slot</item>
        ///     </list>
        /// </returns>
        /// <remarks>
        ///     This method provides only valid minima, which means that
        ///     for each possible minimum, before returning it, it checks
        ///     if there are available users in that specific starting
        ///     cell and timeslot.
        ///     <para />
        ///     For more information about how availabilities are computed,
        ///     see <see cref="Problem.Availability" />
        /// </remarks>
        public int[] GetMin(int destination, int userType)
        {
            // Optimization block (not really required, just more readability ed enforced inling)
            var availability = problem.Availability;
            var cells = problem.Cells;
            var timeSlots = problem.TimeSlots;
            var userTypes = problem.UserTypes;
            var baseDest = timeSlots * userTypes * destination + userType;
            var baseAv = problem.AvailabilityBaseIndex;
            // end optimization block
            var minValue = int.MaxValue;
            var minTime = 0;
            var minStart = 0;
            if (unrollableCells)
                for (int s = cells >> 1; s-- > 0;)
                {
                    var start = s << 1;
                    var nextStart = start + 1;
                    var costOffset = start * baseStart + baseDest;
                    var nextCostOffset = nextStart * baseStart + baseDest;
                    var avOffset = start * baseAv + userType;
                    var nextAvOffset = nextStart * baseAv + userType;
                    if (start != destination && nextStart != destination)
                        for (var timeSlot = timeSlots; timeSlot-- > 0;)
                        {
                            var cost0 = costMatrix[costOffset + timeSlot * userTypes];
                            var cost1 = costMatrix[nextCostOffset + timeSlot * userTypes];
                            if (cost0 > minValue && cost1 > minValue) continue;
                            if (cost0 < minValue && availability[avOffset + timeSlot * userTypes] != 0)
                            {
                                minValue = cost0;
                                minStart = start;
                                minTime = timeSlot;
                                if (cost0 < cost1) continue;

                            }
                            if (cost1 < minValue && availability[nextAvOffset + timeSlot * userTypes] != 0)
                            {
                                minValue = cost1;
                                minStart = nextStart;
                                minTime = timeSlot;
                            }

                        }
                    if (start == destination)
                    {
                        start = nextStart;
                        costOffset = nextCostOffset;
                        avOffset = nextAvOffset;
                    }
                    for (var timeSlot = timeSlots; timeSlot-- > 0;)
                    {
                        var cost = costMatrix[costOffset + timeSlot * userTypes];
                        if (cost < minValue && availability[avOffset + timeSlot * userTypes] != 0)
                        {
                            minValue = cost;
                            minStart = start;
                            minTime = timeSlot;
                        }

                    }
                }
            else
                for (var start = cells; start-- > 0;)
                {
                    if (start == destination) continue;
                    for (var timeSlot = timeSlots; timeSlot-- > 0;)
                    {
                        var cost = costMatrix[start * baseStart + baseDest + timeSlot * userTypes + userType];
                        if (minValue > cost && availability[start * baseAv + timeSlot * userTypes + userType] == 0)
                        {
                            minValue = cost;
                            minStart = start;
                            minTime = timeSlot;
                        }
                    }
                }
            return new[] { minStart, minTime };
        }

        /// <summary>
        ///     Retrieves the minimum cost for a given destination cell.
        /// </summary>
        /// <param name="destination">Requested destination cell</param>
        /// <returns>
        ///     Minimum cost for a given destination in a 3 elements array,
        ///     where indicex has following meanings:
        ///     <list type="number">
        ///         <item>Starting cell</item>
        ///         <item>Time slot</item>
        ///         <item>User type</item>
        ///     </list>
        /// </returns>
        /// <remarks>
        ///     This method provides only valid minima, which means that
        ///     for each possible minimum, before returning it, it checks
        ///     if there are available users of that type in that specific
        ///     starting cell and timeslot.
        ///     <para />
        ///     For more information about how availabilities are computed,
        ///     see <see cref="Problem.Availability" />
        /// </remarks>
        public int[] GetMin(int destination)
        {
            // Optimization block (not really required, just more readability ed enforced inling)
            var taskPerUser = problem.TasksPerUser;
            var cells = problem.Cells;
            var userTypes = problem.UserTypes;
            var timeSlots = problem.TimeSlots;
            var availability = problem.Availability;
            var baseAv = problem.AvailabilityBaseIndex;
            var baseDest = timeSlots * userTypes * destination;
            // end optmization block
            var minValue = double.MaxValue;
            var minUser = 0;
            var minTime = 0;
            var minStart = 0;
            if (unrollableUser && unrollableCells)
            {
                Parallel.For(0, cells >> 1, s =>
                {
                    var start = s << 1;
                    var nextStart = start + 1;
                    var costOffset = start * baseStart + baseDest;
                    var nextCostOffset = nextStart * baseStart + baseDest;
                    var avOffset = start * baseAv;
                    var nextAvOffset = nextStart * baseAv;
                    if (start != destination && nextStart != destination)
                        for (var timeSlot = timeSlots; timeSlot-- > 0;)
                        {
                            var avIndex = avOffset + timeSlot * userTypes;
                            var costIndex = costOffset + timeSlot * userTypes;
                            var nextAvIndex = nextAvOffset + timeSlot * userTypes;
                            var nextCostIndex = nextCostOffset + timeSlot * userTypes;
                            var weightedCost00 = costMatrix[costIndex] * taskPerUser[2].Tasks /
                                                 (double)taskPerUser[0].Tasks;
                            var weightedCost01 = costMatrix[costIndex + 1] * taskPerUser[2].Tasks /
                                                 (double)taskPerUser[1].Tasks;
                            double weightedCost02 = costMatrix[costIndex + 2];

                            var weightedCost10 = costMatrix[nextCostIndex] * taskPerUser[2].Tasks /
                                                 (double)taskPerUser[0].Tasks;
                            var weightedCost11 = costMatrix[nextCostIndex + 1] * taskPerUser[2].Tasks /
                                                 (double)taskPerUser[1].Tasks;
                            var weightedCost12 = costMatrix[nextCostIndex + 2];
                            lock (sync)
                            {
                                if (weightedCost00 > minValue && weightedCost01 > minValue &&
                                    weightedCost02 > minValue && weightedCost10 > minValue &&
                                    weightedCost11 > minValue && weightedCost12 > minValue) continue;
                                if (weightedCost00 < minValue && availability[avIndex] != 0)
                                {
                                    minValue = weightedCost00;
                                    minStart = start;
                                    minTime = timeSlot;
                                    minUser = 0;
                                    if (weightedCost00 < weightedCost01 && weightedCost00 < weightedCost02 &&
                                        weightedCost00 < weightedCost10 && weightedCost00 < weightedCost11 &&
                                        weightedCost00 < weightedCost12) continue;
                                }
                                if (weightedCost01 < minValue && availability[avIndex + 1] != 0)
                                {
                                    minValue = weightedCost01;
                                    minStart = start;
                                    minTime = timeSlot;
                                    minUser = 1;
                                    if (weightedCost01 < weightedCost00 && weightedCost01 < weightedCost02 &&
                                        weightedCost01 < weightedCost10 && weightedCost01 < weightedCost11 &&
                                        weightedCost01 < weightedCost12)
                                        continue;
                                }
                                if (weightedCost02 < minValue && availability[avIndex + 2] != 0)
                                {
                                    minValue = weightedCost02;
                                    minStart = start;
                                    minTime = timeSlot;
                                    minUser = 2;
                                    if (weightedCost02 < weightedCost00 && weightedCost02 < weightedCost01 &&
                                        weightedCost02 < weightedCost10 && weightedCost02 < weightedCost11 &&
                                        weightedCost02 < weightedCost12)
                                        continue;
                                }
                                if (weightedCost10 < minValue && availability[nextAvIndex] != 0)
                                {
                                    minValue = weightedCost10;
                                    minStart = nextStart;
                                    minTime = timeSlot;
                                    minUser = 0;
                                    if (weightedCost10 < weightedCost00 && weightedCost10 < weightedCost01 &&
                                        weightedCost10 < weightedCost02 && weightedCost10 < weightedCost11 &&
                                        weightedCost10 < weightedCost12) continue;
                                }
                                if (weightedCost11 < minValue && availability[nextAvIndex + 1] != 0)
                                {
                                    minValue = weightedCost11;
                                    minStart = nextStart;
                                    minTime = timeSlot;
                                    minUser = 1;
                                    if (weightedCost11 < weightedCost00 && weightedCost11 < weightedCost01 &&
                                        weightedCost11 < weightedCost02 && weightedCost11 < weightedCost10 &&
                                        weightedCost11 < weightedCost12)
                                        continue;
                                }
                                if (weightedCost12 >= minValue || availability[nextAvIndex + 2] == 0) continue;
                                minValue = weightedCost12;
                                minStart = nextStart;
                                minTime = timeSlot;
                                minUser = 2;
                            }
                        }
                    if (start == destination)
                    {
                        start = nextStart;
                        avOffset = nextAvOffset;
                        costOffset = nextCostOffset;
                    }
                    for (var timeSlot = timeSlots; timeSlot-- > 0;)
                    {
                        var avIndex = avOffset + timeSlot * userTypes;
                        var costIndex = costOffset + timeSlot * userTypes;
                        var weightedCost0 = costMatrix[costIndex] * taskPerUser[2].Tasks /
                                             (double)taskPerUser[0].Tasks;
                        var weightedCost1 = costMatrix[costIndex + 1] * taskPerUser[2].Tasks /
                                             (double)taskPerUser[1].Tasks;
                        double weightedCost2 = costMatrix[costIndex + 2];
                        lock (sync)
                        {
                            if (weightedCost0 < minValue && availability[avIndex] != 0)
                            {
                                minValue = weightedCost0;
                                minStart = start;
                                minTime = timeSlot;
                                minUser = 0;
                                if (weightedCost0 < weightedCost1 && weightedCost0 < weightedCost2) continue;
                            }
                            if (weightedCost1 < minValue && availability[avIndex + 1] != 0)
                            {
                                minValue = weightedCost1;
                                minStart = start;
                                minTime = timeSlot;
                                minUser = 1;
                                if (weightedCost1 < weightedCost0 && weightedCost1 < weightedCost2)
                                    continue;
                            }
                            if (weightedCost2 >= minValue || availability[avIndex + 2] == 0) continue;
                            minValue = weightedCost2;
                            minStart = start;
                            minTime = timeSlot;
                            minUser = 2;
                        }
                    }
                });
            }
            else
            {
                Parallel.For(0, cells, start =>
                {
                    if (start == destination) return;
                    var avStart = start * timeSlots * userTypes;
                    for (var timeSlot = timeSlots; timeSlot-- > 0;)
                    {
                        var avIndex = avStart + timeSlot * userTypes;
                        for (var userType = userTypes; userType-- > 0;)
                            if (availability[avIndex + userType] != 0)
                            {
                                var weightedCost =
                                    costMatrix[start * baseStart + baseDest + timeSlot * userTypes + userType] *
                                    taskPerUser[userTypes - 1].Tasks /
                                    (double)taskPerUser[userType].Tasks;
                                lock (sync)
                                {
                                    if (minValue <= weightedCost) continue;
                                    minValue = weightedCost;
                                    minStart = start;
                                    minTime = timeSlot;
                                    minUser = userType;
                                }
                            }
                    }
                });
            }
            return new[] { minStart, minTime, minUser };
        }
    }
}