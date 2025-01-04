using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG.Models
{
    public class Packages
    {
        public int ID { get; set; }
        public int Price { get; set; }

        public Packages(int id, int price)
        {
            ID = id;
            Price = price;
        }

        public override string ToString()
        {
            return $"ID: {ID}, Price: {Price}";
        }
    }
}
