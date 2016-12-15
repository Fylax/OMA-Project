using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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
        public readonly int[] costMatrix;
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
        ///     Creates a new cost matrix (setting to zero all values)
        /// </summary>
        /// <param name="numCells">Number of cells</param>
        /// <param name="timeSlots">Number of time slots</param>
        /// <param name="userTypes">Number of user types</param>
        public Costs(int numCells, int timeSlots, int userTypes)
        {
            costMatrix = new int[numCells*numCells*timeSlots*userTypes];
            unrollableUser = userTypes == 3;
            unrollableCells = numCells%2 == 0;
        }

        /// <summary>
        ///     Adds a cost matrix to the total cost matrix
        /// </summary>
        /// <param name="matrix">Matrix to be added</param>
        /// <param name="timeSlot">Current time slot</param>
        /// <param name="userType">Current user type</param>
        /// <param name="cells"></param>
        /// <param name="timeSlots"></param>
        /// <param name="userTypes"></param>
        public void AddMatrix(int[][] matrix, int timeSlot, int userType, int cells, int timeSlots, int userTypes)
        {
            for (var start = 0; start < cells; ++start)
                for (var dest = 0; dest < cells; ++dest)
                    costMatrix[((start*cells + dest)*timeSlots + timeSlot)*userTypes + userType] = matrix[start][dest];
        }

        /// <summary>
        ///     Retrieves the cost of a movement.
        /// </summary>
        /// <param name="start">Starting cell</param>
        /// <param name="destination">Destination cell</param>
        /// <param name="timeSlot">Current time slot</param>
        /// <param name="userType">Current user type</param>
        /// <returns>Cost for given movement.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetCost(int start, int destination, int timeSlot, int userType) =>
            costMatrix[((start*problem.Cells + destination)*problem.TimeSlots + timeSlot)*problem.UserTypes + userType];

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
        public int[] GetMin(int destination, int userType)
        {
            // Optimization block (not really required, just more readability ed enforced inling)
            var availability = problem.Availability;
            var cells = problem.Cells;
            var timeSlots = problem.TimeSlots;
            var userTypes = problem.UserTypes;
            // end optimization block
            var minValue = int.MaxValue;
            var minTime = 0;
            var minStart = 0;
            if (unrollableCells)
                for (var start = cells; start-- > 0;)
                {
                    if (start != destination)
                        for (var timeSlot = timeSlots; timeSlot-- > 0;)
                        {
                            var cost =
                                costMatrix[
                                    ((start*cells + destination)*timeSlots + timeSlot)*userTypes +
                                    userType];
                            if ((minValue <= cost) ||
                                (availability[(start*timeSlots + timeSlot)*userTypes + userType] == 0))
                                continue;
                            minValue = cost;
                            minStart = start;
                            minTime = timeSlot;
                        }
                    if (--start == destination) continue;
                    for (var timeSlot = timeSlots; timeSlot-- > 0;)
                    {
                        var cost =
                            costMatrix[
                                ((start*cells + destination)*timeSlots + timeSlot)*userTypes +
                                userType];
                        if ((minValue <= cost) ||
                            (availability[(start*timeSlots + timeSlot)*userTypes + userType] == 0))
                            continue;
                        minValue = cost;
                        minStart = start;
                        minTime = timeSlot;
                    }
                }
            else
                for (var start = cells; start-- > 0;)
                {
                    if (start == destination) continue;
                    for (var timeSlot = timeSlots; timeSlot-- > 0;)
                    {
                        var cost =
                            costMatrix[
                                ((start*cells + destination)*timeSlots + timeSlot)*userTypes +
                                userType];
                        if ((minValue <= cost) ||
                            (availability[(start*timeSlots + timeSlot)*userTypes + userType] == 0))
                            continue;
                        minValue = cost;
                        minStart = start;
                        minTime = timeSlot;
                    }
                }
            return new[] {minStart, minTime};
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
        public int[] GetMin(int destination)
        {
            // Optimization block (not really required, just more readability ed enforced inling)
            var taskPerUser = problem.TasksPerUser;
            var cells = problem.Cells;
            var userTypes = problem.UserTypes;
            var timeSlots = problem.TimeSlots;
            var availability = problem.Availability;
            // end optmization block
            var minValue = double.MaxValue;
            var minUser = 0;
            var minTime = 0;
            var minStart = 0;
            Parallel.For(0, cells, start =>
            {
                if (start == destination) return;
                for (var timeSlot = timeSlots; timeSlot-- > 0;)
                    if (unrollableUser)
                    {
                        var weightedCost0 = costMatrix[((start*cells + destination)*
                                                        timeSlots + timeSlot)*userTypes]*
                                            taskPerUser[userTypes - 1].Tasks/(double) taskPerUser[0].Tasks;
                        var weightedCost1 = costMatrix[((start*cells + destination)*
                                                        timeSlots + timeSlot)*userTypes + 1]*
                                            taskPerUser[userTypes - 1].Tasks/(double) taskPerUser[1].Tasks;
                        double weightedCost2 = costMatrix[((start*cells + destination)*
                                                           timeSlots + timeSlot)*userTypes + 2];
                        lock (sync)
                        {
                            if (availability[(start*timeSlots + timeSlot)*userTypes] != 0)
                                if (weightedCost0 < minValue)
                                {
                                    minValue = weightedCost0;
                                    minStart = start;
                                    minTime = timeSlot;
                                    minUser = 0;
                                    if ((weightedCost0 < weightedCost1) && (weightedCost0 < weightedCost2)) continue;
                                }
                            if (availability[(start*timeSlots + timeSlot)*userTypes + 1] != 0)
                                if (weightedCost1 < minValue)
                                {
                                    minValue = weightedCost1;
                                    minStart = start;
                                    minTime = timeSlot;
                                    minUser = 1;
                                    if ((weightedCost1 < weightedCost0) && (weightedCost1 < weightedCost2)) continue;
                                }
                            if (availability[(start*timeSlots + timeSlot)*userTypes + 2] == 0) continue;
                            if (weightedCost2 >= minValue) continue;
                            minValue = weightedCost2;
                            minStart = start;
                            minTime = timeSlot;
                            minUser = 2;
                        }
                    }
                    else
                    {
                        for (var userType = userTypes; userType-- > 0;)
                            if (availability[(start*timeSlots + timeSlot)*userTypes + userType] != 0)
                            {
                                var weightedCost = costMatrix[((start*cells + destination)*
                                                               problem.TimeSlots + timeSlot)*userTypes + userType]*
                                                   taskPerUser[userTypes - 1].Tasks/(double) taskPerUser[userType].Tasks;
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
            return new[] {minStart, minTime, minUser};
        }
    }
}