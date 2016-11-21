using System;
using System.Runtime.InteropServices.ComTypes;
using System.Timers;

namespace OMA_Project
{
    internal static class Program
    {
        public static void Main()
        {
            Problem x = Problem.ReadFromFile(@"C:\Users\Fylax\Desktop\Material_assignment\input\Co_30_1_NT_0.txt");
            using (Timer r = new Timer(5000))
            {
                r.Elapsed += Callback;
                r.Enabled = true;
                var m = x.Availabilty.Clone();
                var p = Solver.GreedySolution(x, m);
                int q = Solver.ObjectiveFunction(p, x);
                int bestUntilNow = q;
                while (r.Enabled)
                {
                    while (!Solver.GenerateNeighborhood(p, m, x.TaskPerUser)) { }
                    int partial = Solver.ObjectiveFunction(p, x);
                    if (partial < bestUntilNow)
                    {
                        bestUntilNow = partial;
                    }
                }
                Console.WriteLine(bestUntilNow);
                Console.Read();
            }
        }

        private static void Callback(object sender, ElapsedEventArgs e)
        {
            ((Timer)sender).Enabled = false;
        }
    }
}
