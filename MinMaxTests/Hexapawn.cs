using System.Data;
using System.Diagnostics;
using MiniMax;
using static MinMaxTests.PawnColor;

namespace MinMaxTests
{
    internal enum PawnColor
    {
        Empty, 
        Black,
        White
    }

    internal enum MoveType
    {
        Advance,
        CapLeft,
        CapRight
    }

    [TestClass]
    public class TestHexapawn
    {
        [TestMethod]
        public void TestMove()
        {
            var rnd = new Random(0);
            var board = new Hexapawn(3, 3);
            var (_, value) = board.EvaluateTree(0, rnd);
            Assert.AreEqual(double.MinValue, value);
        }
    }
    internal class Hexapawn : TwoPlayerAdversarialBoard
    {
        private readonly int _maxPlies;
        private const int PawnValue = 10;
        private const int HighestPawnValue = 7;
        internal readonly PawnColor[,] Board;
        internal readonly int Width;
        internal readonly int Height;
        private static readonly PawnColor[] Players = { White, Black };
        private readonly PawnColor _player;
        private readonly PawnColor _mover;
        private int _highestWhite;
        private int _highestBlack;
        private readonly List<Pawn> _whitePawns;
        private readonly List<Pawn> _blackPawns;
        private int CWhitePawns => _whitePawns.Count;
        private int CBlackPawns => _blackPawns.Count;
        private List<Pawn> MoverPawns => _mover == White ? _whitePawns : _blackPawns;
        private List<Pawn> FoePawns => MoverFoe == White ? _whitePawns : _blackPawns;
        private PawnColor MoverFoe => _mover == White ? Black : White;

        public Hexapawn(int width, int height, int maxPlies = int.MaxValue)
        {
            _player = Players[CurrentPlayer];
            _mover = Players[VantagePoint];
            _maxPlies = maxPlies;
            Board = new PawnColor[height, width];
            Width = width;
            Height = height;
            _whitePawns = new List<Pawn>();
            _blackPawns = new List<Pawn>();

            // White moves first and is at the bottom (lower indices)
            for (var iCol = 0; iCol < Width; iCol++)
            {
                Board[0, iCol] = White;
                _whitePawns.Add(new Pawn(White, 0, iCol));
                Board[Height - 1, iCol] = Black;
                _blackPawns.Add(new Pawn(Black, Height - 1, iCol));
            }
        }

        private Hexapawn(Hexapawn hexapawn, Move move)
        {
            Board = (PawnColor[,])hexapawn.Board.Clone();
            _mover = move.Pawn.Color;
            int iMovedPawn = 0;
            _whitePawns = ClonePawns(hexapawn._whitePawns, move.Pawn.Row, move.Pawn.Col, ref iMovedPawn);
            _blackPawns = ClonePawns(hexapawn._blackPawns, move.Pawn.Row, move.Pawn.Col, ref iMovedPawn);
            var movedPawn = MoverPawns[iMovedPawn];
            _player = hexapawn._player;
            _maxPlies = hexapawn._maxPlies;
            VantagePoint = 1 - hexapawn.VantagePoint;
            _highestBlack = hexapawn._highestBlack;
            _highestWhite = hexapawn._highestWhite;


            Width = hexapawn.Width;
            Height = hexapawn.Height;
            Board[move.Pawn.Row, move.Pawn.Col] = Empty;
            bool fCapture = false;
            switch (move.MoveType)
            {
                case MoveType.Advance:
                    MoverPawns[iMovedPawn].Advance();
                    break;

                case MoveType.CapLeft:
                    MoverPawns[iMovedPawn].Capture(true);
                    fCapture = true;
                    break;

                case MoveType.CapRight:
                    MoverPawns[iMovedPawn].Capture(false);
                    fCapture = true;
                    break;

                default:
                    throw new InvalidConstraintException();
            }

            if (fCapture)
            {
                // Remove pawn from the board
                var (index, capturedPawn) = 
                    FindPawn(movedPawn.Row, movedPawn.Col, movedPawn.Color == White ? _blackPawns : _whitePawns);

                FoePawns.RemoveAt(index);

                // See if it's removal affects highest row achieved
                if (capturedPawn.RowsFromStart(this) == (capturedPawn.Color == White
                        ? _highestWhite
                        : _highestBlack))
                {
                    RecalcHighest(capturedPawn.Color);
                }
            }
            Board[movedPawn.Row, movedPawn.Col] = movedPawn.Color;
            _mover = MoverFoe;

            if (movedPawn.Color == White)
            {
                _highestWhite = Math.Max(movedPawn.RowsFromStart(this), _highestWhite);
            }
            else
            {
                _highestBlack = Math.Max(movedPawn.RowsFromStart(this), _highestBlack);
            }
        }

        private void RecalcHighest(PawnColor color)
        {
            if (color == White)
            {
                if (_whitePawns.Count == 0)
                {
                    _highestWhite =  -1;
                    return;
                }
                _highestWhite = _whitePawns.Select(p => p.RowsFromStart(this)).Max();
            }
            else
            {
                if (_blackPawns.Count == 0)
                {
                    _highestBlack = -1;
                    return;
                }
                _highestBlack = _blackPawns.Select(p => p.RowsFromStart(this)).Max();
            }
        }

        private static (int index, Pawn pawn) FindPawn(int row, int col, List<Pawn> pawns)
        {
            for (var i = 0; i < pawns.Count(); i++)
            {
                if (pawns[i].Row == row && pawns[i].Col == col)
                {
                    return (i, pawns[i]);
                }
            }

            throw new InvalidOperationException("Didn't find pawn");
        }

        private static List<Pawn> ClonePawns(List<Pawn> pawns, int moveRow, int moveCol, ref int movedPawnIndex)
        {
            var ret = new List<Pawn>();

            for (var iPawn = 0; iPawn < pawns.Count; iPawn++)
            {
                var clone = new Pawn(pawns[iPawn]);
               
                if (clone.Row == moveRow && clone.Col == moveCol)
                {
                    movedPawnIndex = iPawn;
                }

                ret.Add(clone);
            }

            return ret;
        }

        protected override double HeuristicScore()
        {
            if (_highestWhite == Height - 1 || _blackPawns.Count == 0)
            {
                return _player == White ? double.MaxValue : double.MinValue;
            }
            if (_highestBlack == Height - 1 || _whitePawns.Count == 0)
            {
                return _player == Black ? double.MaxValue : double.MinValue;
            }

            if (!Moves().Any())
            {
                return _player == _mover ? double.MinValue : double.MaxValue;
            }

            // Assume white and negate at end if wrong
            var value = 0;
            value += (CWhitePawns - CBlackPawns) * PawnValue;
            value += (_highestWhite - _highestBlack) * HighestPawnValue;


            return _player == White ? value : -value;
        }

        protected override IEnumerable<IMove> Moves()
        {
            var pawns = MoverPawns;
            var moves = new List<IMove>();

            foreach (var pawn in pawns)
            {
                if (pawn.CanAdvance(this))
                {
                    moves.Add(new Move(pawn, MoveType.Advance));
                }

                // Put captures before advances as potentially better moves.
                if (pawn.CanCapture(this, true /* fLeft */))
                {
                    moves.Insert(0, new Move(pawn, MoveType.CapLeft));
                }

                if (pawn.CanCapture(this, false))
                {
                    moves.Insert(0, new Move(pawn, MoveType.CapRight));
                }
            }

            return moves;
        }

        protected override Board ApplyMove(IMove move)
        {
            return new Hexapawn(this, (Move)move);
        }

        protected override bool ContinueEvaluatingTree(int cPlies)
        {
            return cPlies < _maxPlies &&
                   _highestBlack != Height - 1 && _highestWhite != Height - 1 &&
                   _whitePawns.Count != 0 && _blackPawns.Count != 0;
        }

        public override string ToString()
        {
            var chars = new char[Width * Height];
            var ichar = 0;
            for (var iRow = 0; iRow < Height; iRow++)
            {
                for (var iCol = 0; iCol < Width; iCol++)
                {
                    chars[ichar++] = ToChar(Board[iRow, iCol]);
                }
            }
            return new string(chars) + $" : {ToChar(_mover)}";
        }

        private static char ToChar(PawnColor color)
        {
            return color switch
            {
                White => 'W',
                Black => 'B',
                Empty => '^'
            };
        }

        private struct Move : IMove
        {
            internal readonly Pawn Pawn;
            internal readonly MoveType MoveType;

            internal Move(Pawn pawn, MoveType moveType)
            {
                Pawn = pawn;
                MoveType = moveType;
            }

            public override string ToString()
            {
                var actionString = MoveType switch
                {
                    MoveType.Advance => "ADVANCE",
                    MoveType.CapLeft => "CAPLEFT",
                    MoveType.CapRight => "CAPRIGHT"
                };
                return $"{ToChar(Pawn.Color)}({Pawn.Row}, {Pawn.Col}) {actionString}";
            }
        }

        private class Pawn
        {
            public int Row;
            public int Col;
            public readonly PawnColor Color;
            private readonly PawnColor Foe;
            public readonly int Direction;

            public Pawn(PawnColor color, int row, int col)
            {
                Row = row; 
                Col = col; 
                Color = color;
                Foe = color == White ? Black : White;
                Direction = Color == White ? 1 : -1;
            }

            public Pawn(Pawn pawn)
            {
                Row = pawn.Row;
                Col = pawn.Col;
                Color = pawn.Color;
                Foe = pawn.Foe;
                Direction = pawn.Direction;
            }

            public int RowsFromStart(Hexapawn board)
            {
                if (Color == White)
                {
                    return Row;
                }
                else
                {
                    return board.Height - 1 - Row;
                }
            }
            public bool CanAdvance(Hexapawn board)
            {
                return board.Board[Row + Direction, Col] == Empty;
            }

            public bool CanCapture(Hexapawn board, bool fLeft)
            {
                if (fLeft && Col == 0 || !fLeft && Col == board.Width - 1)
                {
                    return false;
                }
                return board.Board[Row + Direction, Col + (fLeft ? -1 : 1)] == Foe;
            }

            public void Advance()
            {
                Row += Direction;
            }

            public void Capture(bool fLeft)
            {
                Advance();
                Col += fLeft ? -1 : 1;
            }

            public override string ToString()
            {
                return $"({Row}, {Col}) {ToChar(Color)}";
            }
        }
    }
}
