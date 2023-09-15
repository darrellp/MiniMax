using MiniMax;

namespace MinMaxTests
{
    [TestClass]
    public class TestTicTacToe
    {
        [TestMethod]
        public void TestMove()
        {
            var rnd = new Random(0);
            var board = new TicTacToe();
            var (_, value) = board.EvaluateTree(0, rnd);
            Assert.AreEqual(0, value);
        }
    }
}