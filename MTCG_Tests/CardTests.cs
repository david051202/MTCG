using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTCG.Classes;
using System;

namespace MTCG_Tests
{
    [TestClass]
    public class CardTests
    {
        [TestMethod]
        public void Card_ShouldInitializeCorrectly()
        {
            var card = new Card
            {
                Id = Guid.NewGuid(),
                Name = "TestCard",
                Damage = 50,
                ElementType = "fire",
                CardType = "monster"
            };

            Assert.IsNotNull(card.Id);
            Assert.AreEqual("TestCard", card.Name);
            Assert.AreEqual(50, card.Damage);
            Assert.AreEqual("fire", card.ElementType);
            Assert.AreEqual("monster", card.CardType);
        }
    }
}
