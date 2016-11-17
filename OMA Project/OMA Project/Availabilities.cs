namespace OMA_Project
{
    public class Availabilities
    {
        /// <summary>
        /// Matrice delle disponibilità ordinata per cella, time slot e tipo utente
        /// </summary>
        private readonly int[][][] values;

        private Availabilities(Availabilities av)
        {
            values = (int[][][])av.values.Clone();
        }

        /// <summary>
        /// Crea una nuova istanza partendo da:
        /// </summary>
        /// <param name="cells">Numero di cella</param>
        /// <param name="timeSlots">Time slot corrente</param>
        /// <param name="userTypes">Tipo utente corrente</param>
        public Availabilities(int cells, int timeSlots, int userTypes)
        {
            values = new int[cells][][];
            for (int i = 0; i < cells; ++i)
            {
                values[i] = new int[timeSlots][];
                for (int j = 0; j < timeSlots; ++j)
                {
                    values[i][j] = new int[userTypes];
                }
            }
        }

        /// <summary>
        /// Aggiunge un costo.
        /// </summary>
        /// <param name="cell">Cella di interesse</param>
        /// <param name="timeSlot">Time slot di interesse</param>
        /// <param name="userType">Tipo di utente coinvolto</param>
        /// <param name="userNumber">Costo di spostamento</param>
        public void Add(int cell, int timeSlot, int userType, int userNumber)
        {
            values[cell][timeSlot][userType] = userNumber;
        }

        /// <summary>
        /// Ritorna il numero di utenti disponibili
        /// </summary>
        /// <param name="cell">Cella di interesse</param>
        /// <param name="timeSlot">Time slot di interesse</param>
        /// <param name="userType">Tipo di utente di interesse</param>
        /// <returns>Numero di utenti di uno specifico tipo in una data cella ad un determinato time slot</returns>
        public int GetUserNumber(int cell, int timeSlot, int userType) => values[cell][timeSlot][userType];

        /// <summary>
        /// Data una coppia tipo utente - time slot, aggiunge le disponibilità
        /// relative per ogni cella
        /// </summary>
        /// <param name="timeSlot">Time slot di interesse</param>
        /// <param name="userType">Tipo di utente di interesse</param>
        /// <param name="userNumber">Utenti disponibili per ogni cella</param>
        public void AddPair(int timeSlot, int userType, int[] userNumber)
        {
            for (int i = 0; i < userNumber.Length; ++i)
            {
                values[i][timeSlot][userType] = userNumber[i];
            }
        }
        
        /// <summary>
        /// Utilizza utenti attualmente in una cella
        /// </summary>
        /// <param name="cell">Cella nella quale si trova l'utente</param>
        /// <param name="timeSlot">Time slot di riferimento</param>
        /// <param name="userType">Tipo di utente</param>
        /// <param name="used">Numero di utenti utilizzati</param>
        public void DecreaseUser(int cell, int timeSlot, int userType, int used)
        {
           values[cell][timeSlot][userType] -= used;
        }

        /// <summary>
        /// Crea una versione clonata delle disponibilità
        /// </summary>
        /// <returns>Copia delle disponibilità</returns>
        public Availabilities Clone() => new Availabilities(this);
    }
}
