using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace OMA_Project
{
    public static class WriteSolution
    {
        public static void Write(string filename, List<int> solution, int fitness, long elapsedTime,
            string instance)
        {
            var name = Path.GetFileName(instance)?.Split('.')[0];
            var u1 = 0;
            var u2 = 0;
            var u3 = 0;
            using (var writer = new StreamWriter(filename, true))
            {
                for (var i = 0; i < solution.Count; i+=6)
                    switch (solution[i+3])
                    {
                        case 0:
                            u1 += solution[i+4];
                            break;
                        case 1:
                            u2 += solution[i+4];
                            break;
                        case 2:
                            u3 += solution[i+4];
                            break;
                    }
                writer.WriteLine('"' + name + "\";" +
                                 (elapsedTime/1000d).ToString(CultureInfo.InvariantCulture) + ';' +
                                 fitness + ';' + u1 + ';' + u2 + ';' + u3);
            }
        }
    }
}