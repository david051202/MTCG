using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;
using MTCG.Classes;
using System;

namespace MTCG_Tests
{
    [TestClass]
    public class DatabaseHelperTests
    {
        [TestMethod]
        public void GetOpenConnection_ShouldReturnOpenConnection()
        {
            try
            {
                using (var conn = DatabaseHelper.GetOpenConnection())
                {
                    Assert.AreEqual(System.Data.ConnectionState.Open, conn.State);
                }
            }
            catch (Exception ex)
            {
                Assert.Fail($"Failed to open connection: {ex.Message}");
            }
        }
    }
}
