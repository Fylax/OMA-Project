namespace OMA_Project
{
    public struct TaskPerUser
    {
        public int UserType { get; }
        public int Tasks { get; }

        public TaskPerUser(int userType, int tasks)
        {
            UserType = userType;
            Tasks = tasks;
        }
    }
}
