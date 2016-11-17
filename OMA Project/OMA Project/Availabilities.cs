namespace OMA_Project
{
    public class Availabilities
    {
        int[][][] Values;

        private Availabilities()
        {

        }

        public Availabilities(int cells, int timeSlots, int userTypes)
        {
            Values = new int[cells][][];
            for (int i = 0; i < cells; ++i)
            {
                Values[i] = new int[timeSlots][];
                for (int j = 0; j < timeSlots; ++j)
                {
                    Values[i][j] = new int[userTypes];
                }
            }
        }

        public void Add(int cell, int timeSlot, int userType, int userNumber)
        {
            Values[cell][timeSlot][userType] = userNumber;
        }

        public int GetUserNumber(int cell, int timeSlot, int userType) => Values[cell][timeSlot][userType];

        public void AddPair(int timeSlot, int userType, int[] userNumber)
        {
            for (int i = 0; i < userNumber.Length; ++i)
            {
                Values[i][timeSlot][userType] = userNumber[i];
            }
        }
        
        public void Use(int cell, int timeSlot, int userType, int used)
        {
            Values[cell][timeSlot][userType] -= used;
        }

        public Availabilities Clone()
        {
            var av = new Availabilities()
            {
                Values = (int[][][])Values.Clone()
            };
            return av;
        }
    }
}
