using System;
using System.IO;
using System.Linq;
using OMA_Project.Extensions;

// ReSharper disable PossibleNullReferenceException

namespace OMA_Project
{
    public class Problem
    {
        /// <summary>
        /// c_{ij}^{tm}
        /// Dizionario (lista chiave-Valore):
        /// * chiave = coppia ordinata (Tipo Utente, Time Slot)
        /// * valore = matrice dei costi corrispondente
        /// </summary>
        public Costs Matrix
        {
            get;
            private set;
        }
        
        /// <summary>
        /// Lista con il numero di task per ogni cella
        /// </summary>
        public int[] Tasks
        {
            get; private set;
        }

        /// <summary>
        /// Numero di task che ogni tipo di utente può eseguire.
        /// </summary>
        public TaskPerUser[] TasksPerUser
        {
            get; private set;
        }

        /// <summary>
        /// Dizionario (lista chiave-Valore):
        /// <list type="bullet">
        ///     <item>chiave = coppia ordinata (Tipo Utente, Time Slot)</item>
        ///     <item>valore = utenti disponibili per cella</item>
        /// </list>
        /// </summary>
        public int[][][] Availability
        {
            get;
            set;
        }

        public int Users
        {
            get
            {
                int returns = 0;
                int[] total = TotalUsers();
                for (int i = 0; i < total.Length;++i)
                {
                    returns += total[i];
                }
                return returns;
            }
        }

        public int[] TotalUsers()
        {
            int[] users = new int[TasksPerUser.Length];
            for (int i = Availability.Length; i-- > 0;)
            {
                for (int j = Availability[0].Length; j-- > 0;)
                {
                    for (int k = Availability[0][0].Length; k-- > 0;)
                    {
                        users[k] += Availability[i][j][k];
                    }
                }
            }
            return users;
        }

        public int[][][] immutableAvailability;

        private Problem()
        {

        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public static Problem ReadFromFile(string inputFile)
        {
            Problem prob = new Problem();
            using (FileStream stream = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan))
            using (StreamReader file = new StreamReader(stream, System.Text.Encoding.UTF8, true, 4096))
            {
                // Reads first row (# cell, # time slots, # user types)
                string line = file.ReadLine();
                string[] parts = line.Split(' ');
                int cells = int.Parse(parts[0]);
                int timings = int.Parse(parts[1]);
                int userTypes = int.Parse(parts[2]);

                // Read third row (# Tasks per user)
                file.ReadLine();
                line = file.ReadLine();
                int[] tasksPerUser = Array.ConvertAll(line.Trim().Split(' '), int.Parse);
                var orderedTaskPerUser = tasksPerUser.Select((t, u) => new {task = t, user = u})
                    .OrderBy (t => t.task).ToList();
                prob.TasksPerUser = new TaskPerUser[tasksPerUser.Length];
                for (int i = 0; i < orderedTaskPerUser.Count; ++i)
                {
                    prob.TasksPerUser[i] = new TaskPerUser(orderedTaskPerUser[i].user, orderedTaskPerUser[i].task);
                }

                // Reads and stores matrix of Matrix
                int iterations = unchecked(userTypes * timings);
                prob.Matrix = new Costs(cells, timings, userTypes);
                file.ReadLine();
                for (int i = 0; i < iterations; ++i)
                {
                    line = file.ReadLine();
                    parts = line.Split(' ');
                    int currentUserType = int.Parse(parts[0]);
                    int currentTimeSlot = int.Parse(parts[1]);
                    int[][] matrix = new int[cells][];
                    for (int j = 0; j < cells; ++j)
                    {
                        // legge linea matrice considerando il punto (.) come separatore decimale
                        // direttamente troncato (non arrotondato)
                        line = file.ReadLine();
                        matrix[j] = Array.ConvertAll(line.Trim().Split(' '), cost => (int)float.Parse(cost,
                            System.Globalization.NumberStyles.AllowDecimalPoint,
                            System.Globalization.NumberFormatInfo.InvariantInfo));
                    }
                    prob.Matrix.AddMatrix(currentTimeSlot, currentUserType, matrix);
                }

                // Reads and stores Tasks to be performed on each cell
                file.ReadLine();
                line = file.ReadLine();
                prob.Tasks = Array.ConvertAll(line.Trim().Split(' '), int.Parse);

                // Reads and stores different user availability on each cell, at different timings
                prob.Availability = new int[cells][][];
                for (int i = 0; i < cells; ++i)
                {
                    prob.Availability[i] = new int[timings][];
                    for (int j = 0; j < timings; ++j)
                    {
                        prob.Availability[i][j] = new int[userTypes];
                    }
                }
                file.ReadLine();
                for (int i = 0; i < iterations; ++i)
                {
                    line = file.ReadLine();
                    parts = line.Split(' ');
                    int currentUserType = int.Parse(parts[0]);
                    int currentTimeSlot = int.Parse(parts[1]);
                    line = file.ReadLine();
                    int[] userNumber = Array.ConvertAll(line.Trim().Split(' '), int.Parse);
                    for (int j = 0; j < userNumber.Length; ++j)
                    {
                        prob.Availability[j][currentTimeSlot][currentUserType] = userNumber[j];
                    }
                }
            }
            prob.immutableAvailability = prob.Availability.DeepClone();
            return prob;
        }
    }
}
