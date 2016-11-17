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
            Problem x = new Problem(@"C:\Users\Fylax\Desktop\Material_assignment\input\Co_30_20_NT_0.txt");
            x.Matrix.GetMin(13, x.TaskPerUser);
            var z = Stopwatch.StartNew();
            x.GreedySolution();
            var q = z.ElapsedMilliseconds;
            //Problem x = new Problem(@"C: \Users\vergo\Google Drive\PoliTO - Magistrale\• 1.1 Optimization Methods and Algorithms\Assignement\Materiale\material_assignment_v2\Material_assignment\input\Co_300_20\Co_300_20_T_19.txt");            
            int y = 0;
        }
    }
}
