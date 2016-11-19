using System;
using System.Collections.Generic;
using System.Linq;

namespace OMA_Project
{
    public static class Solver
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="problem"></param>
        /// <returns>
        /// Array di (in ordine):
        /// 0. Cella di partenza
        /// 1. Cella di arrivo
        /// 2. Time Slot
        /// 3. Tipo utente
        /// 4. Utenti utilizzati
        /// </returns>
        public static HashSet<int[]> GreedySolution(Problem problem)
        {
            int[] tasks = new int[problem.Tasks.Length];
            HashSet<int[]> movings = new HashSet<int[]>();
            problem.Tasks.CopyTo(tasks, 0);
            Availabilities av = problem.Availabilty.Clone();
            for (int cell = problem.Matrix.Cells; cell-- > 0;)
            {
                while (tasks[cell] != 0)
                {
                    int[] minimum = problem.Matrix.GetMin(cell, problem.TaskPerUser, av);
                    int available = av.GetUserNumber(minimum[0], minimum[1], minimum[2]);
                    if (available * problem.TaskPerUser[minimum[2]] >= tasks[cell])
                    {
                        int used = (int) Math.Ceiling(tasks[cell]/(double) problem.TaskPerUser[minimum[2]]);
                        av.DecreaseUser(minimum[0], minimum[1], minimum[2], used);
                        tasks[cell] = 0;
                        movings.Add(new[] {minimum[0], cell, minimum[1], minimum[2], used});
                    }
                    else
                    {
                        av.DecreaseUser(minimum[0], minimum[1], minimum[2], available);
                        tasks[cell] -= available;
                        movings.Add(new[] { minimum[0], cell, minimum[1], minimum[2], available });
                    }
                }
            }
            return movings;
        }

        public static int ObjectiveFunction(HashSet<int[]> solution, Problem prob)
        {
            return solution.Sum(sol => (prob.Matrix.GetCost(sol[2], sol[3], sol[0], sol[1])*sol[4]));
        }
    }
}
