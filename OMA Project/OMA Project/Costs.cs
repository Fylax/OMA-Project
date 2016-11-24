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

        public int[] GetMin(int destination, int[] taskPerUser, Availabilities availableUsers)
        {
            int minValue = int.MaxValue;
            int minUser = 0;
            int minTime = 0;
            int minStart = 0;
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
                            if (minValue > weightedCost && availableUsers.HasUsers(start, timeSlot, userType))
                            {
                                minValue = weightedCost;
                                minStart = start;
                                minTime = timeSlot;
                                minUser = userType;
                            }
                        }
                    }
                }
            }
            return new[] { minStart, minTime, minUser };
        }
    }
}
