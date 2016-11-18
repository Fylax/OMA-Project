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
            int[] h = x.Matrix.GetMin(1, x.TaskPerUser, x.Availabilty);
        }
    }
}
