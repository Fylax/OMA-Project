namespace OMA_Project
{
    public class TaskPerUser
    {
        public TaskPerUser(int userType, int tasks)
        {
            UserType = userType;
            Tasks = tasks;
        }

        public int UserType { get; }
        public int Tasks { get; }
    }
}