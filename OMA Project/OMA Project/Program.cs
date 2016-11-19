using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace OMA_Project
{
    internal static class Program
    {
        public static void Main()
        {
            Problem x = new Problem(@"C:\Users\Fylax\Desktop\Material_assignment\input\Co_300_20_NT_0.txt");
            var p = Solver.GreedySolution(x);
            int q = Solver.ObjectiveFunction(p, x);
            //Console.WriteLine(q);
            //Console.Read();
        }
    }
}
