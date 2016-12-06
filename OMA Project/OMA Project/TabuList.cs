using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OMA_Project
{
    class TabuList
    {
        private Queue<int[]> tabuList;
        private int maxLenght;
        
        public TabuList()
        {
            maxLenght = 5;
            tabuList = new Queue<int[]>();
        }

        public void Add(int[] moving)
        {
            tabuList.Enqueue(moving);
            if(tabuList.Count == maxLenght)
            {
                tabuList.Dequeue();
            }
        }

        public bool checkList(int[] moving) => tabuList.Contains(moving);

    }
}
