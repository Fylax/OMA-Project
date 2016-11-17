using System;
using System.Linq;

namespace OMA_Project
{
    public class Costs
    {
        private readonly int[][][][] matrix;

        public Costs(int timeSlots, int userTypes)
        {
            matrix = new int[timeSlots][][][];
            for (int i = 0; i < timeSlots; ++i)
            {
                matrix[i] = new int[userTypes][][];
            }
        }

        public void AddMatrix(int timeSlot, int userType, int[][] matrix)
        {
            this.matrix[timeSlot][userType] = matrix;
        }

        public int GetCost(int timeslot, int userType, int start, int destination) => matrix[timeslot][userType][start][destination];

        public void GetMin(int destination, int[] taskPerUser)
        {
            int maxTask = taskPerUser.Max();
            int minStart = 0;
            int minUserType = 0;
            int minTimeSlot = 0;
            int min = int.MaxValue;
            for (int t = 0; t < matrix.Length; ++t)
            {
                for (int u = 0; u < matrix[t].Length; ++u)
                {
                    for (int i = 0; i < matrix[t][u].Length; ++i)
                    {
                        if (i != destination)
                        {
                            int cost = matrix[t][u][i][destination] * maxTask / taskPerUser[u];
                            if (cost < min)
                            {
                                min = cost;
                                minStart = i;
                                minUserType = u;
                                minTimeSlot = t;
                            }
                        }
                    }
                }
            }
        }
    }
}
