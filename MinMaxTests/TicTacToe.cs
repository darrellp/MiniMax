using System.Diagnostics;
using MiniMax;


namespace MinMaxTests
{
    internal enum CellState
    {
        Unoccupied,
        PlayerX,
        PlayerO
    }

    internal enum Symmetry
    {
        None,
        RotCW1,
        RotCW2,
        RotCW3,
        FlipV,
        FlipRotCW1,
        FlipRotCW2,
        FlipRotCW3
    }

    internal class TicTacToe : Board
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Player doing the evaluating. </summary>
        ///
        /// <value> The player. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private CellState Player => CurrentPlayer == 0 ? CellState.PlayerX : CellState.PlayerO;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Vantage Point we're doing the evaluating from. </summary>
        /// 
        /// <remarks>   During a tree evaluation the Player value remains the same during all recursiong but
        ///             the VantagePlayer alternates between players.  </remarks>
        ///
        /// <value> The vantage point. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private CellState VantagePlayer => VantagePoint == 0 ? CellState.PlayerX : CellState.PlayerO;
        private CellState[,] _board = new CellState[3, 3];
        private int _hashCode = int.MaxValue;
        private Symmetry _symmetryToCanonical = Symmetry.None;

        public TicTacToe()
        {
            for (var iRow = 0; iRow < 3; iRow++)
            {
                for (var iCol = 0; iCol < 3; iCol++)
                {
                    _board[iRow, iCol] = CellState.Unoccupied;
                }
            }
            CurrentPlayer = VantagePoint = 0;
        }

        protected override Board ApplyMove(IMove imove)
        {
            var move = (TttMove)imove;
            var boardRet = new TicTacToe
            {
                _board = (CellState[,])_board.Clone()
            };
            boardRet._board[move.Row, move.Col] = VantagePlayer;
            boardRet.VantagePoint = 1 - VantagePoint;
            return boardRet;
        }

        /// <summary>   A list of the coordinates of all winning triples. 
        ///             Would actually be easier to accomplish this with
        ///             the HashCode</summary>
        private static readonly (int Row, int Col)[][] WinningCombos = new (int, int)[][]
        {
            // Diagonals
            new (int, int)[3]{(0, 0), (1, 1), (2, 2)},
            new (int, int)[3]{(0, 2), (1, 1), (2, 0)},
            // Rows
            new (int, int)[3]{(0, 0), (0, 1), (0, 2)},
            new (int, int)[3]{(1, 0), (1, 1), (1, 2)},
            new (int, int)[3]{(2, 0), (2, 1), (2, 2)},
            // Columns
            new (int, int)[3]{(0, 0), (1, 0), (2, 0)},
            new (int, int)[3]{(0, 1), (1, 1), (2, 1)},
            new (int, int)[3]{(0, 2), (1, 2), (2, 2)}
        };

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Returns the player who wins or CellState.Unoccupied if no winner. </summary>
        ///
        /// <remarks>   darrellp, 8/31/2023. </remarks>
        ///
        /// <param name="combo">    The combo. </param>
        ///
        /// <returns>   A CellState. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private CellState ComboWins(IList<(int Row, int Col)> combo)
        {
            var firstCell = _board[combo[0].Row, combo[0].Col];
            if (firstCell != CellState.Unoccupied &&
                firstCell == _board[combo[1].Row, combo[1].Col] &&
                firstCell == _board[combo[2].Row, combo[2].Col])
            {
                return firstCell;
            }
            return CellState.Unoccupied;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Returns true if either player has a win on this board. </summary>
        ///
        /// <remarks>   darrellp, 8/31/2023. </remarks>
        ///
        /// <returns>   True if there is a win, false if not. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private bool AnyWin()
        {
            return WinningCombos.Any(combo => ComboWins(combo) != CellState.Unoccupied);
        }

        private bool BoardIsFull()
        {
            return _board.Cast<CellState>().All(cell => cell != CellState.Unoccupied);
        }

        protected override bool ContinueEvaluatingTree(int plies)
        {
            // We continue to evaluate if NO wins and if the board is not full yet
            return !AnyWin() && !BoardIsFull();
        }

        protected override double HeuristicScore()
        {
            foreach (var combo in WinningCombos)
            {
                var winner = ComboWins(combo);
                if (winner != CellState.Unoccupied)
                {
                    // Always return score from Player's perspective
                    return Player == winner ? double.MaxValue : double.MinValue;
                }
            }

            return 0;
        }

        protected override IEnumerable<IMove> Moves()
        {
            List<IMove> moves = new();
            for (var iRow = 0; iRow < 3; iRow++)
            {
                for (var iCol = 0; iCol < 3; iCol++)
                {
                    if (_board[iRow, iCol] == CellState.Unoccupied)
                    {
                        moves.Add(new TttMove(iRow, iCol));
                    }
                }
            }
            return moves as ICollection<IMove>;
        }

        public override string? ToString()
        {
            List<char> chars = new();
            for (var iRow = 0; iRow < 3; iRow++)
            {
                for (var iCol = 0; iCol < 3; iCol++)
                {
                    switch (_board[iRow, iCol])
                    {
                        case CellState.Unoccupied:
                            chars.Add('.');
                            break;

                        case CellState.PlayerX:
                            chars.Add('X');
                            break;

                        case CellState.PlayerO:
                            chars.Add('O');
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                if (iRow != 2)
                {
                    chars.Add('/');
                }
            }
            return new string(chars.ToArray());
        }

        public override int GetHashCode()
        {
            if (_hashCode == int.MaxValue)
            {
                (_symmetryToCanonical, _hashCode) =  GetHashCodeRaw();
            }
            return _hashCode;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets hash code for canonical board. </summary>
        ///
        /// <remarks>   This does a zobrist hashcode for all symmetries of the board and uses
        ///             the smallest one which is the "canonical" symmetry for the board.  It returns
        ///             both the hashcode for the canonical board and the symmetry for how to get to
        ///             that canonical board.
        ///             Every two bits, starting from the 1s bit, represents a position on the board in
        ///             rowwise order. For each 2 bits, 00 is unoccupied, 01 is X and 10 is O.
        ///             darrellp, 9/13/2023. </remarks>
        ///
        /// <exception cref="ArgumentOutOfRangeException">  Thrown when one or more arguments are outside
        ///                                                 the required range. </exception>
        ///
        /// <returns>   The hash code. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public (Symmetry sym, int hash) GetHashCodeRaw()
        {
            var shiftPos = 0;
            int finalHash = int.MaxValue;
            Symmetry finalSym = Symmetry.None;

            foreach (Symmetry sym in Enum.GetValues(typeof(Symmetry)))
            {
                var hash = 0;
                for (int iRow = 0; iRow < 3; ++iRow)
                {
                    for (var iCol = 0; iCol < 3; iCol++)
                    {
                        switch (IndexAfterSymmetry(iRow, iCol, sym))
                        {
                            case CellState.Unoccupied:
                                break;
                            case CellState.PlayerX:
                                hash |= 1 << shiftPos;
                                break;
                            case CellState.PlayerO:
                                hash |= 2 << shiftPos;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        shiftPos += 2;
                    }
                }
                if (hash < finalHash)
                {
                    finalSym = sym;
                    finalHash = hash;
                }
            }
            return (finalSym, finalHash);
        }

        // TODO: Double Check this
        CellState IndexAfterSymmetry(int iRow, int iCol, Symmetry sym)
        {
            (int iNewRow, int iNewCol) = sym switch
            {
                Symmetry.None => (iRow, iCol),
                Symmetry.FlipV => (iRow, 2 - iCol),
                Symmetry.RotCW1 => (iCol, 2 - iRow),
                Symmetry.RotCW2 => (2 - iRow, 2 - iCol),
                Symmetry.RotCW3 => (2 - iCol, iRow),
                Symmetry.FlipRotCW1 => (2 - iCol, 2 - iRow),
                Symmetry.FlipRotCW2 => (2 - iRow, iCol),
                Symmetry.FlipRotCW3 => (iCol, iRow),
                _ => throw new ArgumentOutOfRangeException()
            };

            return _board[iNewRow, iNewCol];
        }

        // TODO: Double Check this
        CellState IndexBeforeSymmetry(int iRow, int iCol, Symmetry sym)
        {
            (int iNewRow, int iNewCol) = sym switch
            {
                Symmetry.None => (iRow, iCol),
                Symmetry.FlipV => (iRow, 2 - iCol),
                Symmetry.RotCW1 => (2 - iCol, iRow),
                Symmetry.RotCW2 => (2 - iRow, 2 - iCol),
                Symmetry.RotCW3 => (iRow, 2 - iCol),
                Symmetry.FlipRotCW1 => (2 - iRow, 2 - iCol),
                Symmetry.FlipRotCW2 => (2 - iRow, iCol),
                Symmetry.FlipRotCW3 => (iCol, iRow),
                _ => throw new ArgumentOutOfRangeException()
            };

            return _board[iNewRow, iNewCol];
        }

    }

    internal struct TttMove : IMove
    {
        internal readonly int Row;
        internal readonly int Col;

        internal TttMove(int row, int col)
        {
            Row = row;
            Col = col;
        }

        public override string ToString()
        {
            return $"({Row}, {Col})";
        }
    }
}
