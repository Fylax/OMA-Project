using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace OMA_Project
{
    public class Costs
    {
        private readonly int[][][][] costMatrix;

        public int Cells => costMatrix.Length;

        public Costs(int numCells, int timeSlots, int userTypes)
        {
            costMatrix = new int[numCells][][][];
            for (int i = 0; i < numCells; ++i)
            {
                costMatrix[i] = new int[numCells][][];
                for (int j = 0; j < numCells; ++j)
                {
                    costMatrix[i][j] = new int[timeSlots][];
                    for (int k = 0; k < timeSlots; ++k)
                    {
                        costMatrix[i][j][k] = new int[userTypes];
                    }
                }
            }
        }

        public void AddMatrix(int timeSlot, int userType, int[][] matrix)
        {
            for (int i = 0; i < matrix.Length; ++i)
            {
                for (int j = 0; j < matrix[i].Length; ++j)
                {
                    costMatrix[i][j][timeSlot][userType] = matrix[j][i];
                }
            }
        }

        public int GetCost(int timeSlot, int userType, int start, int destination) => costMatrix[destination][start][timeSlot][userType];

        public int[] GetRandomMin(int destination, int[] taskPerUser, Availabilities availableUsers)
        {
            LinkedList<int[]> minima = new LinkedList<int[]>(); ;
            int maxTasks = 0;
            for (int i = taskPerUser.Length; i-- > 0;)
            {
                if (maxTasks < taskPerUser[i])
                {
                    maxTasks = taskPerUser[i];
                }
            }
            for (int start = costMatrix[0].Length; start-- > 0;)
            {
                if (start != destination)
                {
                    for (int timeSlot = costMatrix[0][0].Length; timeSlot-- > 0;)
                    {
                        for (int userType = costMatrix[0][0][0].Length;
                            userType-- > 0;)
                        {

                            int weightedCost = unchecked(
                                costMatrix[destination][start][timeSlot][userType] * maxTasks /
                                               taskPerUser[userType]
                            );
                            if (minima.Count == 0 && availableUsers.HasUsers(start, timeSlot, userType))
                            {
                                minima.AddFirst(new[] { weightedCost, start, timeSlot, userType });
                            }
                            else if (minima.Count != 0 && weightedCost < minima.Last()[0] && availableUsers.HasUsers(start, timeSlot, userType))
                            {
                                if (minima.Count == 4)
                                {
                                    minima.RemoveLast();
                                }
                                minima.AddFirst(new[] { weightedCost, start, timeSlot, userType });
                            }
                        }
                    }
                }
            }
            Random r = new Random();
            return minima.Select(m => new[] { m[1], m[2], m[3] }).ElementAt(r.Next(0, minima.Count));
        }
    }
}
