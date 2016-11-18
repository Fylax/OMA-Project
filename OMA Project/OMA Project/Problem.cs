using System;
using System.IO;
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
            get;
        }

        /// <summary>
        /// Numero di task che ogni tipo di utente può eseguire.
        /// </summary>
        public int[] TaskPerUser
        {
            get;
        }

        /// <summary>
        /// Dizionario (lista chiave-Valore):
        /// <list type="bullet">
        ///     <item>chiave = coppia ordinata (Tipo Utente, Time Slot)</item>
        ///     <item>valore = utenti disponibili per cella</item>
        /// </list>
        /// </summary>
        public Availabilities Availabilty
        {
            get;
            private set;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public Problem(string inputFile)
        {
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
                TaskPerUser = Array.ConvertAll(line.Trim().Split(' '), int.Parse);

                // Reads and stores matrix of Matrix
                readMatrix(file, userTypes, timings, cells);

                // Reads and stores Tasks to be performed on each cell
                file.ReadLine();
                line = file.ReadLine();
                Tasks = Array.ConvertAll(line.Trim().Split(' '), int.Parse);

                // Reads and stores different user availability on each cell, at different timings
                readAvailabilities(file, userTypes, timings, cells);
            }
        }

        private void readMatrix(TextReader file, int userTypes, int timings, int cells)
        {
            int iterations = unchecked(userTypes * timings);
            Matrix = new Costs(cells, timings, userTypes);
            file.ReadLine();
            for (int i = 0; i < iterations; ++i)
            {
                string line = file.ReadLine();
                string[] parts = line.Split(' ');
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
                Matrix.AddMatrix(currentTimeSlot, currentUserType, matrix);
            }
        }

        private void readAvailabilities(TextReader file, int userTypes, int timings, int cells)
        {
            Availabilty = new Availabilities(cells, timings, userTypes);
            int iterations = unchecked(userTypes * timings);
            file.ReadLine();
            for (int i = 0; i < iterations; ++i)
            {
                string line = file.ReadLine();
                string[] parts = line.Split(' ');
                int currentUserType = int.Parse(parts[0]);
                int currentTimeSlot = int.Parse(parts[1]);
                line = file.ReadLine();
                Availabilty.AddPair(currentTimeSlot, currentUserType, Array.ConvertAll(line.Trim().Split(' '), int.Parse));
            }
        }
    }
}
