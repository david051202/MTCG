using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG.Models
{
    public class PackageCards
    {
        public int PackageID { get; set; }
        public int CardID { get; set; }
        public PackageCards(int id, int packageID, int cardID)
        {
            PackageID = packageID;
            CardID = cardID;
        }
        public override string ToString()
        {
            return $"PackageID: {PackageID}, CardID: {CardID}";
        }
    }
}
