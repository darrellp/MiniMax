namespace MiniMax
{
    public enum TieStrategy
    {
        LastTieWins,
        RandomlySelectFromTies
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   A board. </summary>
    ///
    /// <remarks>   
    ///     Representation of a board.  Includes player count and who is the current player.  Generally,
    ///     this code does very little with manipulating the actual board, determining players and 
    ///     deciding who is opposing who.  Board manipulation and determining the next player should happen
    ///     in an overridden version of ApplyMove.  Deciding who opposes who is done in ChooseHighestScore
    ///     which defaults to everyone opposed to everyone else but can be overridden.
    ///     
    ///     The CURRENT player is to be distinguished from the player whose vantage point we are currently
    ///     taking. All scores are expected to be from the current player's perspective. Depending on the 
    ///     result of ChooseHighestScore we'll either choose the highest score or the lowest. This allows
    ///     for more variations in who is "opposed" to who.  This is the one assumption made by this code -
    ///     higher scores are good for the current player.  Actually, you could make a game where lowest
    ///     score is best for current player as long as ChooseHighestScore were reversed also.  Why you'd
    ///     do that, I don't know, but it would work.
    ///     
    ///     For instance, a puzzle is a zero player game in which there is no opposition. A cooperative
    ///     game is an n player game with no opposition.  Other games may have a secret "opponent" while
    ///     everybody else plays cooperatively.  This even allows for temporary "alliances" which change
    ///     during gameplay.
    ///     
    ///     darrellp, 8/29/2023. </remarks>
    ///
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    public abstract class Board
    {
        private double? _baseValuation = null;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the currently evaluating player. </summary>
        /// 
        /// <remarks>   The CURRENT player is to be distinguished from the player whose vantage point
        ///             we are currently taking. All scores are expected to be from the current player's
        ///             perspective. Depending on the result of ChooseHighestScore we'll either choose
        ///             the highest score or the lowest. This allows for more variations in who is "opposed" to who.
        ///             For instance, a puzzle is a zero player game in which there is no opposition.
        ///             A cooperative game is an n player game with no opposition.  Other games may have a
        ///             secret "opponent" while everybody else plays cooperatively.  This even allows
        ///             for temporary "alliances" which change during gameplay.
        ///             
        ///             Setting both the VantagePoint and Maximize is entirely up to the subclass in ApplyMove().
        ///             darrellp, 8/29/2023. </remarks>
        ///
        /// <value> The current player. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        protected int CurrentPlayer { get; init; } = 0;

        protected internal int VantagePoint { get; set; } = 0;

        protected static TieStrategy Ties => TieStrategy.LastTieWins;

        private double Value()
        {
            _baseValuation ??= HeuristicScore();
            return _baseValuation.Value;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary> Gets a value indicating whether we should choose highest score or lowest. </summary>
        /// 
        /// <remarks> This allows for differing definitions of who opposes who. Default is each 
        ///           person for themselves with no cooperation but it can be overridden. It is the
        ///           subclass's responsibility to set this correctly in any board returned from
        ///           ApplyMove().
        ///           darrellp, 8/31/2023. </remarks>
        ///
        /// <value> True if we should choose highest score, false if not. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        protected internal virtual bool Maximize => CurrentPlayer == VantagePoint;
            

            ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Enumerates moves on this board. </summary>
        ///
        /// <remarks>   darrellp, 8/31/2023. </remarks>
        ///
        /// <returns>
        /// An enumerator that allows foreach to be used to process moves in this collection.
        /// </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        protected abstract IEnumerable<IMove> Moves();

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the evaluation of the board using heuristics - no lookahead. </summary>
        ///
        /// <remarks>   darrellp, 8/29/2023. </remarks>
        ///
        /// <returns>   The valuation of the board for CurrentPlayer. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        protected abstract double HeuristicScore();

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Applies a move to the current board and returns a new board with the move made. </summary>
        ///
        /// <remarks>   We rely on ApplyMove() to determine which player goes next after the move
        ///             and set CurrentPlayer appropriately.
        ///             darrellp, 8/29/2023. </remarks>
        ///
        /// <param name="move"> The move. </param>
        ///
        /// <returns>   A Board. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        protected abstract Board ApplyMove(IMove move);

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Whether or not to continue evaluating tree. </summary>
        ///
        /// <remarks>   For instance, in Tic Tac Toe a win for either side would eliminate
        ///             the need to continue as would a filled board (resulting in a tie).
        ///             darrellp, 8/31/2023. </remarks>
        ///
        /// <param name="cPlies">   The ply count starting at 0 for the original call. </param>
        ///
        /// <returns>   True if it we should continue, false if we should stop. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        protected abstract bool ContinueEvaluatingTree(int cPlies);

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Evaluate tree. </summary>
        ///
        /// <remarks>   
        ///             We assume that positive is always good for the EVALUATING player - not necessarily
        ///             the player whose vantage point we are taking.  This allows for 0 player puzzles and
        ///             cooperative games and other situations.
        ///             darrellp, 8/29/2023. </remarks>
        ///
        /// <param name="cPlies">   The ply count starting at 0 for the original call. </param>
        /// <param name="rnd">      (Optional) Random number generator for ties.  Dflt is new rnd each call. </param>
        ///
        /// <returns>   The best move (if one is available) and evaluation of the tree. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public virtual (IMove?, double) EvaluateTree(int cPlies, Random? rnd = null)
        {
            // TODO: How important is it to use the same RNG each time we recurse and select between ties?
            // If we make this call several times in a row is it important that tied moves be selected the
            // same each time?  I don't really think so.
            Random rndCur = rnd ?? new Random();

#if VERBOSE
            var padding = new string(' ', 4 * cPlies);
            Console.WriteLine($"{padding}Evaluating {this.ToString()} for player {VantagePoint}, Choose Highest = {ChooseHighestScore}");
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
            var maxValue = Maximize ? double.MinValue : double.MaxValue;

            foreach (var move in Moves())
            {
#if VERBOSE
                Console.WriteLine($"{padding}Player {VantagePoint} trying move {move.ToString()}");
#endif
                var curBoard = ApplyMove(move);

                // The move in response to THIS move may change when we change plies so don't
                // bother trying to cache it
                var (_, curValue) = curBoard.EvaluateTree(cPlies + 1);

                // We allow the implementation to tell us whether this player wants the evaluating
                // player to have the highest score (is allied with) or the lowest (is opposed).

                // ReSharper disable once InvertIf
                if (Maximize ? curValue >= maxValue : curValue <= maxValue)
                {
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (Ties == TieStrategy.RandomlySelectFromTies && curValue == maxValue)
                    {
                        tieMoves.Add(move);
                    }
                    else
                    {
                        tieMoves.Clear();
                        tieMoves.Add(move);
                        finalMove = move;
                        maxValue = curValue;
                    }
                }
            }

            if (tieMoves.Count == 0)
            {
                maxValue = HeuristicScore();
            }
            else if (Ties == TieStrategy.RandomlySelectFromTies)
            {
                finalMove = tieMoves[rndCur.Next(tieMoves.Count)];
            }
#if VERBOSE
            Console.WriteLine($"{padding}Final value: {maxValue}");
#endif
            return (finalMove, maxValue);
        }
    }
}