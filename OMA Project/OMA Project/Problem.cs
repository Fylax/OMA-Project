using System;
using System.Collections.Generic;
using System.IO;

namespace OMA_Project
{
    public struct CostIdentifier
    {
        public int UserType
        {
            get;
            private set;
        }

        public int TimeSlot
        {
            get;
            private set;
        }

        public CostIdentifier(int userType, int timeSlot)
        {
            UserType = userType;
            TimeSlot = timeSlot;
        }

        public int GetHasCode() => (unchecked(TimeSlot >> UserType));
    }

    public class Problem
    {
        /// <summary>
        /// c_{ij}^{tm}
        /// Dizionario (lista chiave-Valore):
        /// * chiave = coppia ordinata (Tipo Utente, Time Slot)
        /// * valore = matrice dei costi corrispondente
        /// </summary>
        public Dictionary<CostIdentifier, int[][]> Matrix
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
            private set;
        }

        /// <summary>
        /// Dizionario (lista chiave-Valore):
        /// * chiave = coppia ordinata (Tipo Utente, Time Slot)
        /// * valore = utenti disponibili per cella
        /// </summary>
        public Dictionary<CostIdentifier, int[]> Availabilty
        {
            get;
            private set;
        }

        public Problem(string inputFile)
        {
            using (FileStream stream = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan))
            {
                using (StreamReader file = new StreamReader(stream, System.Text.Encoding.UTF8, true, 4096))
                {

                    string line;
                    string[] parts;

                    // Reads first row (# cell, # time slots, # user types)
                    line = file.ReadLine();
                    parts = line.Split(' ');
                    int cells = int.Parse(parts[0]);
                    int timings = int.Parse(parts[1]);
                    int userTypes = int.Parse(parts[2]);

                    // Read third row (# Tasks per user)
                    file.ReadLine();
                    line = file.ReadLine();
                    int[] taskPerUser = Array.ConvertAll(line.Trim().Split(' '), Convert.ToInt32);

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
        }

        private void readMatrix(StreamReader file, int userTypes, int timings, int cells)
        {
            int iterations = unchecked(userTypes * timings);
            Matrix = new Dictionary<CostIdentifier, int[][]>(iterations);
            string line;
            string[] parts;
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
                    line = file.ReadLine();
                    matrix[j] = Array.ConvertAll(line.Trim().Split(' '), cost => (int)Math.Round(float.Parse(cost,
                        System.Globalization.NumberStyles.AllowDecimalPoint,
                        System.Globalization.NumberFormatInfo.InvariantInfo),
                        0, MidpointRounding.AwayFromZero));
                }
                Matrix.Add(new CostIdentifier(currentUserType, currentTimeSlot), matrix);
            }
        }

        private void readAvailabilities(StreamReader file, int userTypes, int timings, int cells)
        {
            Availabilty = new Dictionary<CostIdentifier, int[]>(cells);
            string line;
            string[] parts;
            int iterations = unchecked(userTypes * timings);
            file.ReadLine();
            for (int i = 0; i < iterations; ++i)
            {
                line = file.ReadLine();
                parts = line.Split(' ');
                int currentUserType = int.Parse(parts[0]);
                int currentTimeSlot = int.Parse(parts[1]);
                line = file.ReadLine();
                int[] available = Array.ConvertAll(line.Trim().Split(' '), int.Parse);
                Availabilty.Add(new CostIdentifier(currentUserType, currentTimeSlot), available);
            }
        }
    }
}
