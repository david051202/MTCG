using System;

namespace MTCG.Classes
{
    public class Card
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public double Damage { get; set; }
        public string ElementType { get; set; }
        public string CardType { get; set; }

        public Deck Deck
        {
            get => default;
            set
            {
            }
        }

        public Package Package
        {
            get => default;
            set
            {
            }
        }
    }
}
