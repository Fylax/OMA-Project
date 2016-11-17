using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace OMA_Project
{
    class Program
    {
        static void Main(string[] args)
        {
            Problem x = new Problem(@"C:\Users\Fylax\Desktop\Material_assignment\input\Co_30_1_NT_0.txt");
            var z = Stopwatch.StartNew();
            var h = x.Matrix.GetMin(1, x.TaskPerUser);
            var q = z.ElapsedMilliseconds;
            x.GreedySolution();

            //Problem x = new Problem(@"C: \Users\vergo\Google Drive\PoliTO - Magistrale\• 1.1 Optimization Methods and Algorithms\Assignement\Materiale\material_assignment_v2\Material_assignment\input\Co_300_20\Co_300_20_T_19.txt");            
            Console.WriteLine(q);
            Console.Read();
        }
    }
}
