using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTCG.Classes;
using System;
using System.Collections.Generic;

namespace MTCG_Tests
{
    [TestClass]
    public class DeckTests
    {
        [TestMethod]
        public void GetDeckByUserId_ShouldReturnDeck()
        {
            var userId = 1; // Assume a user with this ID exists
            var deck = Deck.GetDeckByUserId(userId);

            Assert.IsNotNull(deck);
            Assert.AreEqual(userId, deck.UserId);
        }

        [TestMethod]
        public void AddCardToDeck_ShouldAddCard()
        {
            var userId = 1; // Assume a user with this ID exists
            var deck = Deck.GetDeckByUserId(userId);
            var card = new Card { Id = Guid.NewGuid(), Name = "TestCard", Damage = 50, ElementType = "fire", CardType = "monster" };

            deck.Cards.Add(card);

            CollectionAssert.Contains(deck.Cards, card);
        }

        [TestMethod]
        public void RemoveCardFromDeck_ShouldRemoveCard()
        {
            var userId = 1; // Assume a user with this ID exists
            var deck = Deck.GetDeckByUserId(userId);
            var card = new Card { Id = Guid.NewGuid(), Name = "TestCard", Damage = 50, ElementType = "fire", CardType = "monster" };

            deck.Cards.Add(card);
            deck.Cards.Remove(card);

            CollectionAssert.DoesNotContain(deck.Cards, card);
        }
    }
}
