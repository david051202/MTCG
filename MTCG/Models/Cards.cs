namespace MTCG.Classes
{
    public class Cards
    {
        public string Name { get; set; }
        public int Damage { get; set; }
        public string ElementType { get; set; }
        public bool IsSpellCard { get; set; }

        public Cards(string name, int damage, string elementType, bool isSpellCard)
        {
            Name = name;
            Damage = damage;
            ElementType = elementType;
            IsSpellCard = isSpellCard;
        }
    }
}
