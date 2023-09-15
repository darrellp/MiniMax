//#define VERBOSE
using System.Diagnostics;

namespace MiniMax
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   A two player adversarial board. </summary>
    ///
    /// <remarks>   This is a specialization of Board for the common case of two players
    ///             playing in turns and opposing each other.
    ///             darrellp, 9/9/2023. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    public abstract class TwoPlayerAdversarialBoard : Board
    {
        protected abstract override Board ApplyMove(IMove move);

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Whether or not to continue evaluating tree. </summary>
        ///
        /// <remarks>
        ///             For instance, in Tic Tac Toe a win for either side would eliminate the need to continue as
        ///             would a filled board (resulting in a tie). 
        ///             darrellp, 8/31/2023. </remarks>
        ///
        /// <param name="cPlies">   The ply count starting at 0 for the original call. </param>
        ///
        /// <returns>   True if we should continue, false if we should stop. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        protected abstract override bool ContinueEvaluatingTree(int cPlies);

        protected abstract override double HeuristicScore();

        protected abstract override IEnumerable<IMove> Moves();

        public override (IMove?, double) EvaluateTree(int cPlies, Random? rnd = null)
        {
            return EvaluateTreeAlphaBeta(cPlies, double.MinValue, double.MaxValue, rnd);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Evaluate tree using alpha beta pruning. </summary>
        ///
        /// <remarks>   The max value will be the MINIMUM that a previous move by an ancestor minimize node
        ///             can hold us to.  If we are maximizers and we produce larger than max then that
        ///             earlier minimizer will discard us so there's no use in continuing to investigate
        ///             further moves out of this game board.  Since we are a maximizer the only thing
        ///             other moves could do would be to raise our value still further.  Likewise, the
        ///             min value will be the MAXIMUM that a previous move by an ancestor maximize node
        ///             can hold us to.
        ///             darrellp, 9/9/2023. </remarks>
        ///
        /// <param name="cPlies">   The plies deep we are. </param>
        /// <param name="min">      The minimum value we'll return. </param>
        /// <param name="max">      The maximum value we'll return. </param>
        /// <param name="rnd">      (Optional) RNG. </param>
        ///
        /// <returns>   A Tuple. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public (IMove?, double) EvaluateTreeAlphaBeta(int cPlies, double min /* alpha */, double max /* beta */, Random? rnd = null)
        {
            var rndCur = rnd ?? new Random();

#if VERBOSE
            var padding = new string(' ', 4 * cPlies);
            var choose = Maximize ? "max" : "min";
            Console.WriteLine($"{padding}Evaluating {this.ToString()} for player {VantagePoint}, {choose}");
            Console.WriteLine($"{padding}\t(min, max) = ({min}, {max})");
#endif
            if (!ContinueEvaluatingTree(cPlies))
            {
                var evaluation = HeuristicScore();
#if VERBOSE
                Console.WriteLine($"{padding}Value: {evaluation}");
#endif
                return (null, evaluation);
            }

            var tieMoves = new List<IMove>();

            IMove? finalMove = null;
            var bestValue = Maximize ? double.MinValue : double.MaxValue;
            var prevBest = bestValue;
            var opponent = 1 - VantagePoint;
            bool fFoundMove = false;

            foreach (var move in Moves())
            {
                fFoundMove = true;
#if VERBOSE
                Console.WriteLine($"{padding}Player {VantagePoint} trying move {move.ToString()}");
#endif
                if (ApplyMove(move) is not TwoPlayerAdversarialBoard curBoard)
                {
                    throw new InvalidOperationException(
                        "TwoPlayerAdversarial boards must return the same from ApplyMove");
                }
                
                // The opponent set by ApplyMove had better be the single other player and they better
                // be in opposition to us.  We could set this ourselves easily enough but then the 
                // practice of setting the next player used in Board would be inconsistent here so I'm
                // leaving it up to the implementor for consistency's sake.
                if (curBoard.VantagePoint != opponent)
                {
                    throw new InvalidOperationException("Adversarial opponent wasn't setup correctly in ApplyMove");
                }
                if (curBoard.Maximize != !Maximize)
                {
                    throw new InvalidOperationException(
                        "AdversarialBoard requires the two players to be in opposition");
                }

                var (_, value) = curBoard.EvaluateTreeAlphaBeta(cPlies + 1, min, max, rnd);

                if (Maximize)
                {
                    bestValue = Math.Max(value, bestValue);
                    if (bestValue >= max)
                    {
#if VERBOSE
                        Console.WriteLine($"{padding}Pruned move : {bestValue} >= {max}");
#endif
                        break;
                    }

                    min = Math.Max(value, min);
                }
                else
                {
                    bestValue = Math.Min(value, bestValue);
                    if (bestValue <= min)
                    {
#if VERBOSE
                        Console.WriteLine($"{padding}Pruned move : {bestValue} <= {min}");
#endif
                        break;
                    }

                    max = Math.Min(value, max);
                }
                // ReSharper disable once InvertIf
                if (cPlies == 0)
                {
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (value == bestValue)
                    {
                        // We only worry about tie moves where we actually care about moves at all - at level 0
                        if (Ties == TieStrategy.RandomlySelectFromTies)
                        {
                            tieMoves.Add(move);
                        }
                        else
                        {
                            finalMove = move;
                            bestValue = value;
                        }
                    }
                    else if (Ties == TieStrategy.RandomlySelectFromTies)
                    {
                        tieMoves.Clear();
                        tieMoves.Add(move);
                    }
                }
            }

            if (!fFoundMove)
            {
                bestValue = HeuristicScore();
            }
            else if (Ties == TieStrategy.RandomlySelectFromTies && cPlies == 0)
            {
                finalMove = tieMoves[rndCur.Next(tieMoves.Count)];
            }
#if VERBOSE
            Console.WriteLine($"{padding}Final value: {bestValue}");
#endif
            return (finalMove, bestValue);
        }
    }
}
