using Framework.Core;
using Framework.Interfaces;
namespace Framework.NumTicTacToe;

/* Implementation of IMove for a Numerical TicTacToe specific move type */

public class NumTicTacToeMove : IMove
{
    // properties
    public int Row { get; }
    public int Col { get; }
    public int Value { get; } // NumTicTacToe-specific property
    object IMove.Value => Value; // explicit IMove implementation by casting game-specific Value to object type
    public Player Owner { get; }
    public int MoveNumber { get; set; } // for history/state tracking and undo/redo

    // constructor - create a NumTicTacToe move with specified row/col/value/player
    public NumTicTacToeMove(int row, int col, int value, Player owner)
    {
        Row = row;
        Col = col;
        Value = value;
        Owner = owner;
    }

    // Convert a board position (1-9) to corresponding zero-based index grid row, col values
    public static (int row, int col) PositionToRowColValues(int position, int boardsize = 3)
    {
        int row = (position - 1) % boardsize;
        int col = (position - 1) % boardsize;

        return (row, col);
    }

    // Convert zero-index based grid row, col values to corresponding board position (1-9) 
    public static int RowColValuesToPosition(int row, int col, int boardsize = 3)
    {
        int position = row * boardsize + col + 1; // add one to convert back to 1-based index grid
        return position;
    }

    // Return position on the board a move is attempting to be placed on
    public int GetBoardPosition(int boardsize = 3)
    {
        return RowColValuesToPosition(Row, Col, boardsize);
    }

    // Create a deep copy of the current move for adding it to the history
    public IMove Clone()
    {
        var clonedMove = new NumTicTacToeMove(Row, Col, Value, Owner);
        clonedMove.MoveNumber = this.MoveNumber;

        return clonedMove;
    }

    // Return a string representation of a move
    public override string ToString()
    {
        return $"Move #{MoveNumber}: {Value} placed at position ({Row}, {Col}) by {Owner.Name}";
    }
}
