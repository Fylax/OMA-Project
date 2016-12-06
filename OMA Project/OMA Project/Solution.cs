using System.Collections.Generic;
using OMA_Project.Extensions;

namespace OMA_Project
{
    public class Solution
    {
        /// <summary>
        /// Lista di tutti gli spostamenti facenti parte la soluzione
        /// </summary>
        public List<int[]> Movings { get; }

        public int Count => Movings.Count;

        /// <summary>
        /// Numero della cella per la quale ci sono più spostamenti
        /// </summary>
        private int maxMovingsDestination;

        /// <summary>
        /// Numero di spostamenti per ogni cella
        /// </summary>
        private readonly int[] movingsPerDestination;

        public int[] MovingsFromSource { get; }

        /// <summary>
        /// Indice di lookup con in cui per ogni cella di destinazione
        /// sono indicati gli indici dei vari spostamenti
        /// </summary>
        private readonly HashSet<int>[] destinationLookup;

        private readonly HashSet<int>[] sourceLookup;

        public Solution(int numCell)
        {
            Movings = new List<int[]>();
            maxMovingsDestination = -1;
            destinationLookup = new HashSet<int>[numCell];
            sourceLookup = new HashSet<int>[numCell];
            movingsPerDestination = new int[numCell];
            MovingsFromSource = new int[numCell];
            for (int i = numCell; i-- > 0;)
            {
                destinationLookup[i] = new HashSet<int>();
                sourceLookup[i] = new HashSet<int>();
            }
        }

        /// <summary>
        /// Aggiunge uno spostamento alla soluzione
        /// </summary>
        /// <param name="moving">
        /// Spostamento nella forma:<para />
        /// 0. Sorgente<para />
        /// 1. Destinazione<para />
        /// 2. Time Slot<para />
        /// 3. Tipo utente<para />
        /// 4. Numero utenti coinvolti<para />
        /// 5. Task svolti
        /// </param>
        public void Add(int[] moving)
        {
            Movings.Add(moving);
            destinationLookup[moving[1]].Add(Movings.Count - 1);
            sourceLookup[moving[0]].Add(Movings.Count - 1);
            movingsPerDestination[moving[1]]++;
            MovingsFromSource[moving[0]]++;
            if (maxMovingsDestination == -1 || movingsPerDestination[moving[1]] > movingsPerDestination[maxMovingsDestination])
            {
                maxMovingsDestination = moving[1];
            }
        }

        /// <summary>
        /// Rimuove uno spostamento
        /// </summary>
        /// <param name="moving">
        /// Spostamento nella forma:<para />
        /// 0. Sorgente<para />
        /// 1. Destinazione<para />
        /// 2. Time Slot<para />
        /// 3. Tipo utente<para />
        /// 4. Numero utenti coinvolti<para />
        /// 5. Task svolti
        /// </param>
        public void Remove(int[] moving)
        {
            int moveIndex = Movings.IndexOf(moving);
            destinationLookup[moving[1]].Remove(moveIndex);
            sourceLookup[moving[0]].Remove(moveIndex);
            movingsPerDestination[moving[1]]--;
            MovingsFromSource[moving[0]]--;
            Movings.Remove(moving);
            if (maxMovingsDestination == moving[1])
            {
                for (int i = destinationLookup.Length; i-- > 0;)
                {
                    if (movingsPerDestination[i] > maxMovingsDestination)
                    {
                        maxMovingsDestination = i;
                    }
                }
            }
        }

        public void RemoveAt(int index)
        {
            this.Remove(this.ElementAt(index));
        }

        /// <summary>
        /// Ottiene uno spostamento a un dato indice
        /// </summary>
        /// <param name="position">Indice</param>
        /// <returns>
        /// Spostamento nella forma:<para />
        /// 0. Sorgente<para />
        /// 1. Destinazione<para />
        /// 2. Time Slot<para />
        /// 3. Tipo utente<para />
        /// 4. Numero utenti coinvolti<para />
        /// 5. Task svolti
        /// </returns>
        public int[] ElementAt(int position) => Movings[position];

        public int[][] MovingsFromCell(int cell)
        {
            int[][] returns = new int[MovingsFromSource[cell]][];
            using (HashSet<int>.Enumerator enumerator = sourceLookup[cell].GetEnumerator())
            {
                for (int i = MovingsFromSource[cell]; i-- > 0;)
                {
                    enumerator.MoveNext();
                    returns[i] = Movings[enumerator.Current];
                }
            }
            return returns;
        }

        private int[][] MovingsToCell(int cell)
        {
            int[][] returns = new int[movingsPerDestination[cell]][];
            using (HashSet<int>.Enumerator enumerator = destinationLookup[cell].GetEnumerator())
            {
                for (int i = movingsPerDestination[cell]; i-- > 0;)
                {
                    enumerator.MoveNext();
                    returns[i] = Movings[enumerator.Current];
                }
            }
            return returns;
        }

        public int[][] MovingsToRandomCell()
        {
            int cell;
            do
            {
                cell = Program.generator.Next(destinationLookup.Length);
            } while (destinationLookup[cell].Count == 0);
            return MovingsToCell(cell);
        }

        /// <summary>
        /// Ottiene la lista degli spostamenti per la cella col maggior numero
        /// di spostamenti entranti
        /// </summary>
        /// <returns>
        /// Array di spostamenti nella forma:<para />
        /// 0. Sorgente<para />
        /// 1. Destinazione<para />
        /// 2. Time Slot<para />
        /// 3. Tipo utente<para />
        /// 4. Numero utenti coinvolti<para />
        /// 5. Task svolti
        /// </returns>
        public int[][] MovementsMaxDestination() => MovingsToCell(maxMovingsDestination);

        public void RemoveCell(int cell)
        {
            Movings.RemoveAll(tuple => tuple[1] == cell);
        }

        public void RemoveMax()
        {
            RemoveCell(maxMovingsDestination);
        }

        public Solution Clone()
        {
            Solution solution = new Solution(destinationLookup.Length);
            for (int i = Movings.Count; i-- > 0;)
            {
                solution.Add(Movings[i]);
            }
            return solution;
        }

        public bool isFeasible(Problem x)
        {
            int[] tasks = (int[])x.Tasks.Clone();
            int[][][] availabilities = x.immutableAvailability.DeepClone();

            for (int i = Movings.Count; i-- > 0;)
            {
                if (Movings[i][0] == Movings[i][1])     //Se la partenza è uguale alla destinazione (Non possibile)
                    return false;                       //Soluzione unfeasible

                tasks[Movings[i][1]] -= Movings[i][5];  //Aggiorna i task da fare rimuovendo quelli svolti dal vettore soluzione considerato
                availabilities[Movings[i][0]][Movings[i][2]][Movings[i][3]] -= Movings[i][4]; //Aggiorna le disponibilità per la cella di partenza, per un certo timeslot, per un certo tipo utente
            }

            for (int i = tasks.Length; i-- > 0;)
            {
                if (tasks[i] > 0)
                    return false;
            }

            for (int i = availabilities.Length; i-- > 0;)
            {
                for (int j = availabilities[0].Length; j-- > 0;)
                {
                    for (int k = availabilities[0][0].Length; k-- > 0;)
                    {
                        if (availabilities[i][j][k] < 0)                //Se la disponibilità di utenti in cella i, timeslot j e tipoutente k è negativa
                        {
                            return false;
                        } 
                    }
                }
            }
            return true;
        }
    }
}
