namespace OMA_Project
{
    public class TaskPerUser
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TaskPerUser"/> class.
        /// </summary>
        /// <param name="userType">Type of the user.</param>
        /// <param name="tasks">Tasks it can perform.</param>
        public TaskPerUser(int userType, int tasks)
        {
            UserType = userType;
            Tasks = tasks;
        }

        /// <summary>
        /// Gets the type of the user.
        /// </summary>
        /// <value>
        /// The type of the user.
        /// </value>
        public int UserType { get; }


        /// <summary>
        /// Gets the tasks the user can perfomr.
        /// </summary>
        /// <value>
        /// The tasks the user can perform.
        /// </value>
        public int Tasks { get; }
    }
}