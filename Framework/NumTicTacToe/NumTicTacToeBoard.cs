using System.Collections.Generic;
using System.Linq;
using Framework.Core;
using Framework.Interfaces;
using NumTPiece = Framework.NumTicTacToe.NumTicTacToePiece; // shorter alias for generic type
namespace Framework.NumTicTacToe;

/* Concrete implementation of IBoard<TPiece> representing a Numerical TicTacToe board.
   The board is a composition of 9 BoardSquare<NumTPiece> squares inside a 1D List. */

public class NumTicTacToeBoard : IBoard
{
    private readonly List<BoardSquare<NumTPiece>> _boardSquares; // reference for board grid
    public int Size { get; } = 3; // Numerical TicTacToe grid row x col size


    // constructor - initializes board squares only once during construction
    public NumTicTacToeBoard()
    {
        // creates a 1D List of 9 BoardSquare objects
        _boardSquares = new(Size * Size); // pre-allocate list space for 3x3 (9) squares

        for (int row = 0; row < Size; row++)
        {
            for (int col = 0; col < Size; col++)
            {
                _boardSquares.Add(new BoardSquare<NumTPiece>(row, col));
            }
        }
    }

    // place the piece derived from a move onto the board
    public void PlacePiece(int row, int col, IPiece piece, Player owner)
    {
        var square = GetNumTPieceSquare(row, col);

        if (square.IsOccupied)
        {
            ConsoleUI.DisplayErrorMessage("Square already occupied.");
            return;
        }

        // create NumTicTacToePiece piece type with the received data then set the piece on the board
        var numTPiece = (NumTPiece)BoardGameFramework.GetFactory().CreatePiece((int)piece.Value, owner);
        square.SetPiece(numTPiece);
    }

    // Helper method to get a strongly-typed square
    private BoardSquare<NumTPiece> GetNumTPieceSquare(int row, int col)
    {
        return _boardSquares.Find(s => s.Row == row && s.Col == col)
                ?? throw new KeyNotFoundException($"Square at position ({row}, {col}) not found.");
    }

    // return a specific IBoardSquare square
    public IBoardSquare GetSquare(int row, int col)
    {
        return GetNumTPieceSquare(row, col);
    }

    // sets a IBoardSquare square at the specified grid position after reseting and checking square 
    public void SetSquare(int row, int col, IBoardSquare square)
    {
        int index = row * Size + col;

        if (index >= 0 && index < _boardSquares.Count)
        {
            _boardSquares[index].ResetSquare();

            if (square.IsOccupied && square.Piece != null)
            {
                var numTPiece = new NumTPiece((int)square.Piece.Value, square.Piece.Owner);
                _boardSquares[index].SetPiece(numTPiece);
            }
        }
    }

    // returns a deep copy of the current board & piece composie
    public IBoard Clone()
    {
        var clonedBoard = new NumTicTacToeBoard();

        for (int row = 0; row < Size; row++)
        {
            for (int col = 0; col < Size; col++)
            {
                var originalSquare = GetSquare(row, col);
                var clonedSquare = clonedBoard.GetSquare(row, col);

                // if the original square has a piece, clone it and set it on the new square
                if (originalSquare.IsOccupied && originalSquare.Piece != null)
                {
                    var clonedPiece = BoardGameFramework.GetFactory()
                                                        .CreatePiece((int)originalSquare.Piece.Value, originalSquare.Piece.Owner);
                    clonedSquare.SetPiece((NumTPiece)clonedPiece);
                }
            }
        }
        return clonedBoard;
    }

    // returns an enumerable sequnce of IBoardSquares
    public IEnumerable<IBoardSquare> GetSquares()
    {
        return _boardSquares.Cast<IBoardSquare>();
    }

    // resets all squares to an initial state
    public void ResetAllSquares()
    {
        foreach (var square in _boardSquares)
        {
            square.ResetSquare();
        }
    }
}
