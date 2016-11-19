using System;
using System.Runtime.InteropServices.ComTypes;
using System.Timers;

namespace OMA_Project
{
    internal static class Program
    {
        public static void Main()
        {
            Problem x = new Problem(@"C:\Users\Fylax\Desktop\Material_assignment\input\Co_30_1_NT_0.txt");
            int min = int.MaxValue;
            using (Timer r = new Timer(5000))
            {
                r.Elapsed += Callback;
                r.Enabled = true;
                while (r.Enabled)
                {
                    //qui va il codice
                    var p = Solver.GreedySolution(x);
                    int q = Solver.ObjectiveFunction(p, x);
                    if (q < min)
                    {
                        min = q;
                    }
                }
                Console.WriteLine(min);
                Console.Read();
            }
        }

        private static void Callback(object sender, ElapsedEventArgs e)
        {
            ((Timer) sender).Enabled = false;
        }
    }
}
