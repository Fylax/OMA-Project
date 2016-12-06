using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OMA_Project
{
    public class TabuList
    {
        public Queue<int[]> List
        {
            get;
        }
            
        private int maxLenght;
        
        public TabuList()        {
            maxLenght = 5;
            List = new Queue<int[]>();
        }

        public void Add(int[] moving)
        {
            List.Enqueue(moving);
            if(List.Count == maxLenght)
            {
                List.Dequeue();
            }
        }

        public bool checkList(int[] moving) => List.Contains(moving);

    }
}
