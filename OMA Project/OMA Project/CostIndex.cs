namespace OMA_Project
{
    public struct CostIndex
    {
        public int TimeSlot
        {
            get;
        }

        public int UserType
        {
            get;
        }

        public CostIndex(int timeSlot, int userType)
        {
            TimeSlot = timeSlot;
            UserType = userType;
        }

        public override bool Equals(object obj)
        {
            if(obj is CostIndex)
            {
                CostIndex temp = (CostIndex)obj;
                return temp.TimeSlot == TimeSlot && temp.UserType == UserType;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (TimeSlot + UserType) * (TimeSlot + UserType + 1) / 2 + TimeSlot;
        }
    }
}
