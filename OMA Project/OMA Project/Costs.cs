﻿namespace OMA_Project
{
    public class Costs
    {
        private readonly int[][][][] costMatrix;

        public Costs(int numCells, int timeSlots, int userTypes)
        {
            costMatrix = new int[numCells][][][];
            for (var i = 0; i < numCells; ++i)
            {
                costMatrix[i] = new int[numCells][][];
                for (var j = 0; j < numCells; ++j)
                {
                    costMatrix[i][j] = new int[timeSlots][];
                    for (var k = 0; k < timeSlots; ++k)
                        costMatrix[i][j][k] = new int[userTypes];
                }
            }
        }

        public void AddMatrix(int timeSlot, int userType, int[][] matrix)
        {
            for (var i = 0; i < matrix.Length; ++i)
                for (var j = 0; j < matrix[i].Length; ++j)
                    costMatrix[i][j][timeSlot][userType] = matrix[j][i];
        }

        public int GetCost(int timeSlot, int userType, int start, int destination)
            => costMatrix[destination][start][timeSlot][userType];

        public int[] GetMin(int destination, int[][][] availableUsers, int userType)
        {
            var minValue = int.MaxValue;
            var minTime = 0;
            var minStart = 0;
            for (var start = costMatrix[0].Length; start-- > 0;)
                if (start != destination)
                    for (var timeSlot = costMatrix[0][0].Length; timeSlot-- > 0;)
                    {
                        var cost = costMatrix[destination][start][timeSlot][userType];
                        if ((minValue > cost) && (availableUsers[start][timeSlot][userType] != 0))
                        {
                            if (cost == 1)
                                return new[] {start, timeSlot};
                            minValue = cost;
                            minStart = start;
                            minTime = timeSlot;
                        }
                    }
            return new[] {minStart, minTime};
        }

        public int[] GetMin(int destination, Problem problem)
        {
            var minValue = double.MaxValue;
            var minUser = 0;
            var minTime = 0;
            var minStart = 0;
            for (var start = problem.Cells; start-- > 0;)
                if (start != destination)
                    for (var timeSlot = problem.TimeSlots; timeSlot-- > 0;)
                        for (var userType = problem.UserTypes; userType-- > 0;)
                        {
                            var weightedCost = costMatrix[destination][start][timeSlot][userType]*
                                               problem.TasksPerUser[problem.TasksPerUser.Length - 1].Tasks/
                                               (double) problem.TasksPerUser[userType].Tasks;
                            if ((minValue > weightedCost) && (problem.Availability[start][timeSlot][userType] != 0))
                            {
                                minValue = weightedCost;
                                minStart = start;
                                minTime = timeSlot;
                                minUser = userType;
                            }
                        }
            return new[] {minStart, minTime, minUser};
        }
    }
}