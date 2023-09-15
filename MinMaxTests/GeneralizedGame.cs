using System.Diagnostics;
using MiniMax;

namespace MinMaxTests
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   A generalized game node. </summary>
    ///
    /// <remarks>   Makes a game tree by simply creating the tree directly for testing purposes.
    ///             darrellp, 9/11/2023. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    internal class GeneralizedGameNode : Node<String>, IMove
    {
        internal bool LeafNode => Children == null || Children.Count == 0;
        internal double Evaluation => (double)(Value![^1] - '0');

        internal GeneralizedGameNode(string value)
        {
            Value = value;
        }

        public override string ToString()
        {
            return Value!;
        }
    }

    internal class GeneralizedGame : TwoPlayerAdversarialBoard
    {
        private GeneralizedGameNode GameNode;

        internal GeneralizedGame(GeneralizedGameNode node)
        {
            GameNode = node;
        }

        protected override double HeuristicScore()
        {
            return (GameNode.Evaluation);
        }

        protected override IEnumerable<IMove> Moves()
        {
            return (GameNode.Children != null ? GameNode.Children.Select(n => (IMove)n) : new List<IMove>())!;
        }

        protected override Board ApplyMove(IMove move)
        {
            var node = move as GeneralizedGameNode;
            var board = new GeneralizedGame(node!);
            Debug.Assert(board != null, nameof(board) + " != null");
            board.VantagePoint = 1 - VantagePoint;
            return board;
        }

        protected override bool ContinueEvaluatingTree(int cPlies)
        {
            return !GameNode.LeafNode;
        }
    }
}
