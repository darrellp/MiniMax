using MiniMax;

namespace MinMaxTests
{
    [TestClass]
    public class TestAlphaBeta
    {
        [TestMethod]
        public void TestMove()
        {
            var board = GetGame();
            var (_, value) = board.EvaluateTree(0);
            Assert.AreEqual(6, value);

        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Converts shorthand input to actual value. </summary>
        ///
        /// <remarks>   Assumes format rccc...-sss... where r is row, c is column and s is score.  The "-"
        ///             and the score are optional.  The only restriction is fewer than 10 rows which is
        ///             not normally gonna cause problems - 10 rows is a LOT!
        ///             darrellp, 9/12/2023. </remarks>
        ///
        /// <param name="shortName">    Name of the short. </param>
        ///
        /// <returns>   Value to be used for the node. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private static string Convert(string shortName)
        {
            var szRow = shortName[0..1];
            var szCol = shortName[1..];
            var szScore = string.Empty;
            var dashPos = shortName.IndexOf('-');
            var isLeaf = dashPos >= 0;
            if (isLeaf)
            {
                szCol = shortName[1..dashPos];
                szScore = shortName[(dashPos + 1)..];
            }

            return isLeaf
                ? $"({szRow}, {szCol}) - {szScore}"
                : $"({szRow}, {szCol})";
        }

        private static GeneralizedGame GetGame()
        {
            // Specify the root
            var ggm = new GeneralizedGameMaker("11", Convert);

            // Specify the game tree
            // Recreates the tree pictured at:
            // https://en.wikipedia.org/wiki/Alpha%E2%80%93beta_pruning#/media/File:AB_pruning.svg
            ggm.AddChildren("11", "21", "22", "23");
            ggm.AddChildren("21", "31", "32");
            ggm.AddChildren("22", "33", "34");
            ggm.AddChildren("23", "35", "36");
            ggm.AddChildren("31", "41", "42");
            ggm.AddChildren("32", "43");
            ggm.AddChildren("33", "44", "45");
            ggm.AddChildren("34", "46");
            ggm.AddChildren("35", "47");
            ggm.AddChildren("36", "48", "49");
            ggm.AddChildren("41", "51-5", "52-6");
            ggm.AddChildren("42", "53-7", "54-4", "55-5");
            ggm.AddChildren("43", "56-3");
            ggm.AddChildren("44", "57-6");
            ggm.AddChildren("45", "58-6", "59-9");
            ggm.AddChildren("46", "510-7");
            ggm.AddChildren("47", "511-5");
            ggm.AddChildren("48", "512-9", "512-8");
            ggm.AddChildren("49", "514-6");

            return ggm.Game;
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   A generalized game maker. </summary>
    ///
    /// <remarks>   darrellp, 9/11/2023. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    internal class GeneralizedGameMaker
    {
        private readonly Dictionary<string, GeneralizedGameNode> _dict = new();
        private readonly GeneralizedGameNode _root;
        internal GeneralizedGame Game => new(_root);

        internal delegate string DoConvert(string shortName);
        private readonly DoConvert _convert;

        public GeneralizedGameMaker(string root, DoConvert convert)
        {
            _convert = convert;
            var rootValue = _convert(root);
            _root = new GeneralizedGameNode(rootValue);
            _dict[root] = _root;
        }

        public void AddChildren(string parent, params string[] children)
        {
            var parentNode = _dict[parent];

            foreach (var child in children)
            {
                var childNode = new GeneralizedGameNode(_convert(child));
                parentNode.AddChild(childNode);
                _dict[child] = childNode;
            }
        }
    }
}
