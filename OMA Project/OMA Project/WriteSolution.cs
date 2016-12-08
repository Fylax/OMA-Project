﻿using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace OMA_Project
{
    public static class WriteSolution
    {
        public static void Write(string filename, IEnumerable<int[]> solution, int fitness, long elapsedTime,
            string instance)
        {
            var name = Path.GetFileName(instance)?.Split('.')[0];
            var u1 = 0;
            var u2 = 0;
            var u3 = 0;
            using (var writer = new StreamWriter(filename, true))
            {
                foreach (var sol in solution)
                    if (sol[3] == 0)
                        u1 += sol[4];
                    else if (sol[3] == 1)
                        u2 += sol[4];
                    else if (sol[3] == 2)
                        u3 += sol[4];
                writer.WriteLine('"' + name + "\";" +
                                 (elapsedTime/1000d).ToString(CultureInfo.InvariantCulture) + ';' +
                                 fitness + ';' + u1 + ';' + u2 + ';' + u3);
            }
        }
    }
}