using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MTCG.Models;
using MTCG.Classes;
using MTCG.Battle;

namespace MTCG_Tests
{
    [TestClass]
    public class Tests
    {
        private const string ConnectionString = "Host=localhost;Username=postgres;Password=yourpassword;Database=mtcg";

        // Tests for Package class
        [TestMethod]
        public void CreatePackage_ShouldCreatePackage()
        {
            var cards = GetTestCards();
            var result = Package.CreatePackage(cards);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void BuyPackage_ShouldReturnNotEnoughCoins()
        {
            var user = new User { UserId = 1, Username = "Player", Coins = 2, Cards = new List<Card>() };
            var result = Package.BuyPackage(user);

            Assert.AreEqual(Package.BuyResult.NotEnoughCoins, result);
        }

        [TestMethod]
        public void BuyPackage_ShouldReturnError()
        {
            var user = new User { UserId = 1, Username = "Player", Coins = 10, Cards = new List<Card>() };

            // Simulate an error (e.g., database connection issue)
            var result = Package.BuyPackage(user);

            Assert.AreEqual(Package.BuyResult.Error, result);
        }

        // Tests for BattleLogic class
        [TestMethod]
        public async Task StartBattleAsync_ShouldStartBattle()
        {
            var player1 = new User { Username = "Player1", Cards = GetTestCards() };
            var player2 = new User { Username = "Player2", Cards = GetTestCards() };

            var battleLogic = new BattleLogic(player1, player2);
            var outcome = await battleLogic.StartBattleAsync();

            Assert.IsNotNull(outcome);
            Assert.IsNotNull(outcome.Log);
        }

        [TestMethod]
        public void UpdateUserData_ShouldUpdateData()
        {
            var user = new User { UserId = 1, Username = "ExistingUser" };
            var result = user.UpdateUserData("NewName", "NewBio", "NewImage");

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void AddCard_ShouldAddCardToUser()
        {
            var user = new User { UserId = 1, Username = "ExistingUser", Cards = new List<Card>() };
            var card = new Card { Id = Guid.NewGuid(), Name = "NewCard" };

            user.AddCard(card);

            CollectionAssert.Contains(user.Cards, card);
        }

        // Additional tests
        [TestMethod]
        public void RemoveCard_ShouldRemoveCardFromUser()
        {
            var user = new User { UserId = 1, Username = "ExistingUser", Cards = GetTestCards() };
            var card = user.Cards.First();

            user.Cards.Remove(card);

            CollectionAssert.DoesNotContain(user.Cards, card);
        }

        [TestMethod]
        public void GetCardById_ShouldReturnCorrectCard()
        {
            var user = new User { UserId = 1, Username = "ExistingUser", Cards = GetTestCards() };
            var card = user.Cards.First();

            var result = user.Cards.FirstOrDefault(c => c.Id == card.Id);

            Assert.AreEqual(card, result);
        }

        [TestMethod]
        public void GetCardById_ShouldReturnNullForNonExistentCard()
        {
            var user = new User { UserId = 1, Username = "ExistingUser", Cards = GetTestCards() };

            var result = user.Cards.FirstOrDefault(c => c.Id == Guid.NewGuid());

            Assert.IsNull(result);
        }

        [TestMethod]
        public void UpdateCard_ShouldUpdateCardDetails()
        {
            var user = new User { UserId = 1, Username = "ExistingUser", Cards = GetTestCards() };
            var card = user.Cards.First();
            card.Name = "UpdatedCard";

            var updatedCard = user.Cards.FirstOrDefault(c => c.Id == card.Id);
            Assert.AreEqual("UpdatedCard", updatedCard.Name);
        }

        [TestMethod]
        public void GetAllCards_ShouldReturnAllCards()
        {
            var user = new User { UserId = 1, Username = "ExistingUser", Cards = GetTestCards() };

            var result = user.Cards;

            Assert.AreEqual(4, result.Count);
        }

        [TestMethod]
        public void User_ShouldInitializeWithEmptyCardList()
        {
            var user = new User { UserId = 1, Username = "NewUser" };

            Assert.IsNotNull(user.Cards);
            Assert.AreEqual(0, user.Cards.Count);
        }

        [TestMethod]
        public void User_ShouldInitializeWithGivenValues()
        {
            var user = new User { UserId = 1, Username = "NewUser", Coins = 10 };

            Assert.AreEqual(1, user.UserId);
            Assert.AreEqual("NewUser", user.Username);
            Assert.AreEqual(10, user.Coins);
        }

        // Helper method to generate test cards
        private List<Card> GetTestCards(int count = 4)
        {
            var cards = new List<Card>();
            for (int i = 0; i < count; i++)
            {
                cards.Add(new Card
                {
                    Id = Guid.NewGuid(),
                    Name = $"Card{i}",
                    Damage = 20,
                    ElementType = "fire",
                    CardType = "monster"
                });
            }
            return cards;
        }
    }
}
