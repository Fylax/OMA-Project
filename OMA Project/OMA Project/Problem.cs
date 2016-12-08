using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OMA_Project.Extensions;

// ReSharper disable PossibleNullReferenceException

namespace OMA_Project
{
    public class Problem
    {
        public int[][][] immutableAvailability;

        private Problem(int cells, int timeSlots, int users)
        {
            Cells = cells;
            TimeSlots = timeSlots;
            UserTypes = users;
        }

        /// <summary>
        ///     c_{ij}^{tm}
        ///     Dizionario (lista chiave-Valore):
        ///     * chiave = coppia ordinata (Tipo Utente, Time Slot)
        ///     * valore = matrice dei costi corrispondente
        /// </summary>
        public Costs Matrix { get; private set; }

        public int Cells { get; }
        public int TimeSlots { get; }
        public int UserTypes { get; }

        /// <summary>
        ///     Lista con il numero di task per ogni cella
        /// </summary>
        public int[] Tasks { get; private set; }

        /// <summary>
        ///     Numero di task che ogni tipo di utente può eseguire.
        /// </summary>
        public TaskPerUser[] TasksPerUser { get; private set; }

        /// <summary>
        ///     Dizionario (lista chiave-Valore):
        ///     <list type="bullet">
        ///         <item>chiave = coppia ordinata (Tipo Utente, Time Slot)</item>
        ///         <item>valore = utenti disponibili per cella</item>
        ///     </list>
        /// </summary>
        public int[][][] Availability { get; set; }

        public int Users { get; set; }

        public int TotalUser(int userType)
        {
            var users = 0;
            Parallel.For(0, Cells, i =>
            {
                for (var j = TimeSlots; j-- > 0;)
                    users += Availability[i][j][userType];
            });
            return users;
        }

        public int[] TotalUsers()
        {
            var users = new int[UserTypes];
            Parallel.For(0, Cells, i =>
            {
                for (var j = TimeSlots; j-- > 0;)
                    for (var k = UserTypes; k-- > 0;)
                        users[k] += Availability[i][j][k];
            });
            return users;
        }

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
                    var orderedTaskPerUser = tasksPerUser.Select((t, u) => new { task = t, user = u })
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
                            matrix[j] = Array.ConvertAll(line.Trim().Split(' '), cost => (int)float.Parse(cost,
                                NumberStyles.AllowDecimalPoint,
                                NumberFormatInfo.InvariantInfo));
                        }
                        prob.Matrix.AddMatrix(currentTimeSlot, currentUserType, matrix);
                    }

                    // Reads and stores Tasks to be performed on each cell
                    file.ReadLine();
                    line = file.ReadLine();
                    prob.Tasks = Array.ConvertAll(line.Trim().Split(' '), int.Parse);

                    // Reads and stores different user availability on each cell, at different timings
                    prob.Availability = new int[cells][][];
                    for (var i = 0; i < cells; ++i)
                    {
                        prob.Availability[i] = new int[timings][];
                        for (var j = 0; j < timings; ++j)
                            prob.Availability[i][j] = new int[userTypes];
                    }
                    file.ReadLine();
                    for (var i = 0; i < iterations; ++i)
                    {
                        line = file.ReadLine();
                        parts = line.Split(' ');
                        var currentUserType = int.Parse(parts[0]);
                        var currentTimeSlot = int.Parse(parts[1]);
                        line = file.ReadLine();
                        var userNumber = Array.ConvertAll(line.Trim().Split(' '), int.Parse);
                        for (var j = 0; j < userNumber.Length; ++j)
                        {
                            prob.Availability[j][currentTimeSlot][currentUserType] = userNumber[j];
                            prob.Users += userNumber[j];
                        }
                    }
                }
            }
            prob.immutableAvailability = prob.Availability.DeepClone();
            return prob;
        }
    }
}