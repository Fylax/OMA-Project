using System.Collections.Generic;
using OMA_Project.Extensions;

namespace OMA_Project
{
    public static class Solution
    {
        public static bool IsFeasible(List<int> movings)
        {
            var tasks = (int[]) Program.problem.Tasks.Clone();
            var availabilities = Program.problem.ImmutableAvailability.DeepClone();

            for (var i = movings.Count; (i -= 6) >= 0;)
            {
                if (movings[i] == movings[i + 1]) //Se la partenza è uguale alla destinazione (Non possibile)
                    return false; //Soluzione unfeasible

                tasks[movings[i + 1]] -= movings[i + 4] * Program.problem.TasksPerUser[movings[i + 3]].Tasks;
                //Aggiorna i task da fare rimuovendo quelli svolti dal vettore soluzione considerato
                availabilities[
                        (movings[i] * Program.problem.TimeSlots + movings[i + 2]) * Program.problem.UserTypes +
                        movings[i + 3]]
                    -= movings[i + 4];
                //Aggiorna le disponibilità per la cella di partenza, per un certo timeslot, per un certo tipo utente
            }

            for (var i = Program.problem.Cells; i-- > 0;)
                if (tasks[i] > 0)
                    return false;

            for (var i = Program.problem.Cells; i-- > 0;)
            for (var j = Program.problem.TimeSlots; j-- > 0;)
            for (var k = Program.problem.UserTypes; k-- > 0;)
                if (availabilities[(i * Program.problem.TimeSlots + j) * Program.problem.UserTypes + k] < 0)
                    //Se la disponibilità di utenti in cella i, timeslot j e tipoutente k è negativa
                    return false;
            return true;
        }
    }
}