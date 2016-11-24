using OMA_Project.Extensions;
namespace OMA_Project
{
    public class Availabilities
    {
        /// <summary>
        /// Matrice delle disponibilità ordinata per cella, time slot e tipo utente
        /// </summary>
        private readonly int[][][] values;

        public Availabilities() { }
        
        private Availabilities(Availabilities av)
        {
            values = av.values.DeepClone();
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
        /// Indica se sono presenti utenti
        /// </summary>
        /// <param name="cell">Cella di interesse</param>
        /// <param name="timeSlot">Time slot di interesse</param>
        /// <param name="userType">Tipo di utente di interesse</param>
        /// <returns />
        public bool HasUsers(int cell, int timeSlot, int userType) => values[cell][timeSlot][userType] != 0;

        public int TotalUsers
        {
            get
            {
                int tot = 0;
                for(int i = 0; i < values.Length; ++i)
                {
                    for(int j =0; j<values[i].Length;++j)
                    {
                        for(int k = 0; k < values[i][j].Length;++k)
                        {
                            tot += values[i][j][k];
                        }
                    }
                }
                return tot;
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
            int current = values[cell][timeSlot][userType];
            values[cell][timeSlot][userType] = current - used;
        }

        public void IncreaseUser(int cell, int timeSlot, int userType, int used)
        {
            int current = values[cell][timeSlot][userType];
            values[cell][timeSlot][userType] = current + used;
        }


        /// <summary>
        /// Crea una versione clonata delle disponibilità
        /// </summary>
        /// <returns>Copia delle disponibilità</returns>
        public Availabilities Clone() => new Availabilities(this);
    }
}
