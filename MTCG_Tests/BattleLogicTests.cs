using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTCG.Models;
using MTCG.Classes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MTCG_Tests
{
    [TestClass]
    public class BattleLogicTests
    {
        [TestMethod]
        public async Task StartBattleAsync_ShouldReturnOutcome()
        {
            var player1 = new User { Username = "Player1", Cards = GetTestCards() };
            var player2 = new User { Username = "Player2", Cards = GetTestCards() };

            var battleLogic = new BattleLogic(player1, player2);
            var outcome = await battleLogic.StartBattleAsync();

            Assert.IsNotNull(outcome);
            Assert.IsNotNull(outcome.Log);
        }

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
