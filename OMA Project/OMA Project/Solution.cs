using System;
using System.Collections.Generic;

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

        /// <summary>
        /// Indice di lookup con in cui per ogni cella di destinazione
        /// sono indicati gli indici dei vari spostamenti
        /// </summary>
        private readonly HashSet<int>[] index;

        public Solution(int numCell)
        {
            Movings = new List<int[]>();
            maxMovingsDestination = -1;
            index = new HashSet<int>[numCell];
            movingsPerDestination = new int[numCell];
            for (int i = numCell; i-- > 0;)
            {
                index[i] = new HashSet<int>();
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
            index[moving[1]].Add(Movings.Count - 1);
            movingsPerDestination[moving[1]]++;
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
            index[moving[1]].Remove(Movings.IndexOf(moving));
            movingsPerDestination[moving[1]]--;
            Movings.Remove(moving);
            if (maxMovingsDestination == moving[1])
            {
                for (int i = index.Length; i-- > 0;)
                {
                    if (movingsPerDestination[i] > maxMovingsDestination)
                    {
                        maxMovingsDestination = i;
                    }
                }
            }
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

        public int[][] MovingsToRandomCell()
        {
            Random generator = new Random();
            int cell;
            do
            {
                cell = generator.Next(index.Length);
            } while (index[cell].Count == 0);
            int[][] returns = new int[index[cell].Count][];
            using (HashSet<int>.Enumerator enumerator = index[cell].GetEnumerator())
            {
                for (int i = index[cell].Count; i-- > 0;)
                {
                    enumerator.MoveNext();
                    returns[i] = Movings[enumerator.Current];
                }
            }
            return returns;
        }

        public void RemoveCell(int cell)
        {
            Movings.RemoveAll(s => s[1] == cell);
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
        public int[][] MovementsMaxDestination()
        {
            int total = index[maxMovingsDestination].Count;
            int[][] returns = new int[total][];
            using (HashSet<int>.Enumerator enumerator = index[maxMovingsDestination].GetEnumerator())
            {
                for (int i = total; i-- > 0;)
                {
                    enumerator.MoveNext();
                    returns[i] = Movings[enumerator.Current];
                }
            }
            return returns;
        }

        public void RemoveMax()
        {
            RemoveCell(maxMovingsDestination);
        }

        public Solution Clone()
        {
            Solution solution = new Solution(index.Length);
            for (int i = Movings.Count; i-- > 0;)
            {
                solution.Add(Movings[i]);
            }
            return solution;
        }
    }
}
