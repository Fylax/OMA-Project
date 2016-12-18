using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using OMA_Project.Extensions;

namespace OMA_Project
{
    /// <summary>
    ///     Class containing all data concerning the problem (number of cells, number of timeslots,
    ///     user types, etc.)
    /// </summary>
    public class Problem
    {
        /// <summary>
        ///     Initializes a new (and unique) instance of the <see cref="Problem" />.
        /// </summary>
        /// <param name="cells">Number of cells.</param>
        /// <param name="timeSlots">Number of time slots.</param>
        /// <param name="users">Number of users types.</param>
        private Problem(int cells, int timeSlots, int users)
        {
            Cells = cells;
            TimeSlots = timeSlots;
            UserTypes = users;
        }

        /// <summary>
        ///     An immutable copy of original availability informations.
        /// </summary>
        /// <inheritdoc cref="Availability" />
        public int[] ImmutableAvailability { get; private set; }

        /// <summary>
        ///     An immutable copy of the original total users counter
        ///     (regardless of the type of the user).
        /// </summary>
        /// <inheritdoc cref="Users" />
        public int ImmutableUsers { get; private set; }

        /// <summary>
        ///     Allows to retrieve the matrix of the costs.
        /// </summary>
        /// <value>
        ///     Matrix of all costs, containing method to perform
        ///     computations on it.
        /// </value>
        public Costs Matrix { get; private set; }

        /// <summary>
        ///     Gets the number of the cells.
        /// </summary>
        /// <value>
        ///     Number of cells.
        /// </value>
        public int Cells { get; }

        /// <summary>
        ///     Gets the number of time slots.
        /// </summary>
        /// <value>
        ///     Number of time slots.
        /// </value>
        public int TimeSlots { get; }

        /// <summary>
        ///     Gets the number of user types.
        /// </summary>
        /// <value>
        ///     Number of user types.
        /// </value>
        public int UserTypes { get; }

        /// <summary>
        ///     Gets the tasks tasks that must be accomplished
        ///     on each cell.
        /// </summary>
        /// <value>
        ///     Tasks to be performed on each cell.
        /// </value>
        /// <remarks>
        ///     This is an immutable property, number of tasks
        ///     must <b>never</b> be changed.
        /// </remarks>
        public int[] Tasks { get; private set; }

        /// <summary>
        ///     Gets the tasks each user type can perform
        ///     in ascending ordering (both user type and
        ///     performable tasks are provided).
        /// </summary>
        /// <value>
        ///     Number of tasks each user type can perform
        /// </value>
        /// <remarks>
        ///     This is an immutable property, number of tasks
        ///     must <b>never</b> be changed.
        /// </remarks>
        public TaskPerUser[] TasksPerUser { get; private set; }

        /// <summary>
        ///     Flattened array containing available users on each cell
        ///     of each type on each time slot.
        /// </summary>
        /// <value>
        ///     As this is the result of the flattening of a 3D array,
        ///     in order to compute the 3D indices, following formula
        ///     must be used:
        ///     <c>
        ///         (CurrentCell * NumberOfTimeSlots + CurrentTimeSlot) *
        ///         NumberOfUserTypes + CurrentUserType
        ///     </c>
        /// </value>
        public int[] Availability { get; set; }

        /// <summary>
        ///     Gets the total number of users still available.
        /// </summary>
        /// <value>
        ///     The users.
        /// </value>
        /// <remarks>
        ///     This property comes in hand for trowing <see cref="NoUserLeft" />.
        /// </remarks>
        public int Users { get; set; }

        /// <summary>
        ///     Computes the number of users still available for each type.
        /// </summary>
        /// <returns>Users still available for each type.</returns>
        /// <remarks>
        ///     This method is mainly used in <see cref="Solver.SolvePreciseTasks" />.
        /// </remarks>
        public int[] TotalUsers()
        {
            var users = new int[UserTypes];
            for (var i = Cells; i-- > 0;)
            for (var j = TimeSlots; j-- > 0;)
            for (var k = UserTypes; k-- > 0;)
                users[k] += Availability[(i * TimeSlots + j) * UserTypes + k];
            return users;
        }

        /// <summary>
        ///     Reads an instance file.
        /// </summary>
        /// <param name="inputFile">Path of the input file.</param>
        /// <returns>Parsed problem.</returns>
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public static Problem ReadFromFile(string inputFile)
        {
            Problem prob;
            using (
                var stream = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096,
                    FileOptions.SequentialScan))
            {
                using (var file = new StreamReader(stream, Encoding.UTF8, true, 4096))
                {
                    // Reads first row (# cell, # time slots, # user types)
                    var line = file.ReadLine();
                    var parts = line.Split(' ');
                    var cells = int.Parse(parts[0]);
                    var timings = int.Parse(parts[1]);
                    var userTypes = int.Parse(parts[2]);

                    prob = new Problem(cells, timings, userTypes);

                    // Read third row (# Tasks per user)
                    file.ReadLine();
                    line = file.ReadLine();
                    var tasksPerUser = Array.ConvertAll(line.Trim().Split(' '), int.Parse);
                    var orderedTaskPerUser = tasksPerUser.Select((t, u) => new {task = t, user = u})
                        .OrderBy(t => t.task).ToList();
                    prob.TasksPerUser = new TaskPerUser[userTypes];
                    for (var i = 0; i < orderedTaskPerUser.Count; ++i)
                        prob.TasksPerUser[i] = new TaskPerUser(orderedTaskPerUser[i].user, orderedTaskPerUser[i].task);

                    // Reads and stores matrix of Matrix
                    var iterations = unchecked(userTypes * timings);
                    prob.Matrix = new Costs(cells, timings, userTypes);
                    file.ReadLine();
                    for (var i = 0; i < iterations; ++i)
                    {
                        line = file.ReadLine();
                        parts = line.Split(' ');
                        var currentUserType = int.Parse(parts[0]);
                        var currentTimeSlot = int.Parse(parts[1]);
                        var matrix = new int[cells][];
                        for (var j = 0; j < cells; ++j)
                        {
                            // legge linea matrice considerando il punto (.) come separatore decimale
                            // direttamente troncato (non arrotondato)
                            line = file.ReadLine();
                            matrix[j] = Array.ConvertAll(line.Trim().Split(' '), cost => (int) float.Parse(cost,
                                NumberStyles.AllowDecimalPoint,
                                NumberFormatInfo.InvariantInfo));
                        }
                        prob.Matrix.AddMatrix(matrix, currentTimeSlot, currentUserType, cells, timings, userTypes);
                    }

                    // Reads and stores Tasks to be performed on each cell
                    file.ReadLine();
                    line = file.ReadLine();
                    prob.Tasks = Array.ConvertAll(line.Trim().Split(' '), int.Parse);

                    // Reads and stores different user availability on each cell, at different timings
                    prob.Availability = new int[cells * timings * userTypes];
                    file.ReadLine();
                    for (var i = 0; i < iterations; ++i)
                    {
                        line = file.ReadLine();
                        parts = line.Split(' ');
                        var currentUserType = int.Parse(parts[0]);
                        var currentTimeSlot = int.Parse(parts[1]);
                        line = file.ReadLine();
                        var userNumber = Array.ConvertAll(line.Trim().Split(' '), int.Parse);
                        for (var cell = 0; cell < cells; ++cell)
                        {
                            prob.Availability[cell * timings * userTypes + currentTimeSlot * userTypes + currentUserType
                                ] =
                                userNumber[cell];
                            prob.Users += userNumber[cell];
                        }
                    }
                }
            }
            prob.ImmutableAvailability = prob.Availability.DeepClone();
            prob.ImmutableUsers = prob.Users;
            return prob;
        }
    }
}