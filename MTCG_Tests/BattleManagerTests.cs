using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTCG.Models;
using MTCG.Battle;
using MTCG.Classes; // Add this using directive
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MTCG_Tests
{
    [TestClass]
    public class BattleManagerTests
    {
        [TestMethod]
        public void AddUserToLobby_ShouldAddUser()
        {
            var user = new User { UserId = 1, Username = "Player" };
            BattleManager.Instance.AddUserToLobby(user);

            var result = BattleManager.Instance.GetBattleResult(user.UserId);
            Assert.IsNull(result); // No battle result yet
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
