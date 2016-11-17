using System.Collections.Generic;
using System.Linq;

namespace OMA_Project
{
    public class Costs
    {
        private readonly Dictionary<CostIndex, int[][]> matrix;

        public Costs(int timeSlots, int userTypes)
        {
            matrix = new Dictionary<CostIndex, int[][]>(timeSlots * userTypes);
        }

        public void AddMatrix(int timeSlot, int userType, int[][] matrix)
        {
            this.matrix.Add(new CostIndex(timeSlot, userType), matrix);
        }

        public int GetCost(int timeSlot, int userType, int start, int destination) => matrix[new CostIndex(timeSlot, userType)][start][destination];

        public System.Tuple<CostIndex, int> GetMin(int destination, int[] taskPerUser)
        {
            int maxTask = taskPerUser.Max();
            int minStart = 0;
            CostIndex minUserTime = new CostIndex();
            int min = int.MaxValue;
            foreach (var relativeCosts in matrix)
            {
                for (int i = 0; i < relativeCosts.Value.Length; ++i)
                {
                    if (i != destination)
                    {
                        int cost = relativeCosts.Value[i][destination] * maxTask / taskPerUser[relativeCosts.Key.UserType];
                        if (cost < min)
                        {
                            min = cost;
                            minStart = i;
                            minUserTime = relativeCosts.Key;
                        }
                    }
                }
            }
            return System.Tuple.Create(minUserTime, minStart);
        }
    }
}
