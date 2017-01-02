using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace OMA_Project
{
    /// <summary>
    ///     Writes the result on a file
    /// </summary>
    public static class WriteSolution
    {
        /// <summary>
        ///     Writes a summary of the solution on file.
        /// </summary>
        /// <param name="filename">Output file.</param>
        /// <param name="solution">Solution to be summarized.</param>
        /// <param name="fitness">Fitness for this solution.</param>
        /// <param name="elapsedTime">Elapsed time.</param>
        /// <param name="instance">Instance name.</param>
        public static void Write(string filename, List<int> solution, int fitness, double elapsedTime,
            string instance)
        {
            var name = Path.GetFileName(instance)?.Split('.')[0];
            var u1 = 0;
            var u2 = 0;
            var u3 = 0;
            using (var writer = new StreamWriter(filename, true))
            {
                for (var i = 0; i < solution.Count; i += 6)
                    switch (Program.problem.TasksPerUser[solution[i + 3]].UserType)
                    {
                        case 0:
                            u1 += solution[i + 4];
                            break;
                        case 1:
                            u2 += solution[i + 4];
                            break;
                        case 2:
                            u3 += solution[i + 4];
                            break;
                    }
                writer.WriteLine('"' + name + "\";" +
                                 elapsedTime.ToString(CultureInfo.InvariantCulture) + ';' +
                                 fitness + ';' + u1 + ';' + u2 + ';' + u3);
            }
        }
    }
}