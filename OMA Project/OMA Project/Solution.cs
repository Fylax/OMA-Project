using System.Collections.Generic;
using OMA_Project.Extensions;

namespace OMA_Project
{
    public static class Solution
    {
        public static bool IsFeasible(List<int[]> Movings, Problem x)
        {
            var tasks = (int[]) x.Tasks.Clone();
            var availabilities = x.immutableAvailability.DeepClone();

            for (var i = Movings.Count; i-- > 0;)
            {
                if (Movings[i][0] == Movings[i][1]) //Se la partenza è uguale alla destinazione (Non possibile)
                    return false; //Soluzione unfeasible

                tasks[Movings[i][1]] -= Movings[i][5];
                    //Aggiorna i task da fare rimuovendo quelli svolti dal vettore soluzione considerato
                availabilities[Movings[i][0]][Movings[i][2]][Movings[i][3]] -= Movings[i][4];
                    //Aggiorna le disponibilità per la cella di partenza, per un certo timeslot, per un certo tipo utente
            }

            for (var i = tasks.Length; i-- > 0;)
                if (tasks[i] > 0)
                    return false;

            for (var i = availabilities.Length; i-- > 0;)
                for (var j = availabilities[0].Length; j-- > 0;)
                    for (var k = availabilities[0][0].Length; k-- > 0;)
                        if (availabilities[i][j][k] < 0)
                            //Se la disponibilità di utenti in cella i, timeslot j e tipoutente k è negativa
                            return false;
            return true;
        }
    }
}