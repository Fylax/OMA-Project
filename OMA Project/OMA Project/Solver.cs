

namespace OMA_Project
{
    public static class Solver
    {
        public static void GreedySolution(Problem problem)
        {
            int objFunct = 0;
            int[] tasks = new int[problem.Tasks.Length];
            problem.Tasks.CopyTo(tasks, 0);
            Availabilities av = problem.Availabilty.Clone();
            for (int cell = problem.Matrix.Cells; cell-- > 0;)
            {
                if (tasks[cell] != 0)
                {
                    int[] minimum = problem.Matrix.GetMin(cell, problem.TaskPerUser, av);
                    int available = av.GetUserNumber(minimum[0], minimum[1], minimum[2]);
                    if () 
                }
            }
        }
    }
}
