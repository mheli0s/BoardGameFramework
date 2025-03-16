using System;
using System.Linq;
using Framework.Core;
using Framework.Enums;
using Framework.Interfaces;
namespace Framework.NumTicTacToe;

/* Implements the IGameRules interface to define gam rules and logic specific to a
   Numerical TicTacToe game. */

// primary constructor syntax
public class NumTicTacToeRules(GameState gameState, IBoard board) : IGameRules
{
    // fields
    private readonly GameState _gameState = gameState;
    private readonly IBoard _board = board;


    /* Validates all game rules and logic during for each move during gameplay. Uses a tuple
       to also return the error message produced by any invalid input to the console.  */
    public (bool isValid, string? errorMessage) IsValidMove(IBoard board, IMove move)
    {
        // square occupied
        if (!IsSquareAvailable(board, move.Row, move.Col))
        {
            return (false, "\nInvalid move - square is already occupied.");
        }

        // piece value was outside each player's allowed piece set
        if (!IsValidPieceRangeForPlayer((int)move.Value, move.Owner))
        {
            return
            (false, $"\nInvalid move - value: {move.Value} is not in the valid piece set for {move.Owner.Name}");
        }

        // Check if the piece has already been placed on the board
        if (board.GetSquares()
                .Any(s => s.IsOccupied && s.Piece != null && (int)s.Piece.Value == (int)move.Value))
        {
            return (false, "\nThis piece has already been used on the board.");
        }

        return (true, string.Empty); // passed our validity check above, return valid piece
    }

    /* Returns true if move inputs are within board grid bounds and structured correctly else displays
       an error message displaying what the move format issue was. */
    public bool IsValidMoveFormat(string[] parts, out string? errorMessage)
    {
        // check move input has correct format structure and number of parts needed for the format
        if (parts.Length != 4 || parts[0] != "m")
        {
            errorMessage = "\nInvalid move format. Use: 'm row col value'. Eg. m 1 2 7";
            return false;
        }

        // move input incorrectly starts with a number
        if (!char.IsLetter(parts[0][0]))
        {
            errorMessage = "\nInvalid command: '{input}'. Enter 'm' for a move, 'u' for undo, etc.";
            Console.WriteLine();
            return false;
        }

        // save each matching part to out variables and return true if inputs were within specified range
        if (!int.TryParse(parts[1], out int row) ||
            !int.TryParse(parts[2], out int col) ||
            !int.TryParse(parts[3], out int value))
        {
            errorMessage = "\nInvalid move format - incorrect positional structure or missing parts.";
            return false;
        }

        // move out of bounds for position or piece values
        if (!IsWithinBounds(row, col, value))
        {
            errorMessage = "\nInvalid move format - input out of bounds. Use: Row/Col: 1-3, Value: 1-9";
            return false;
        }

        // move input is in correct format
        errorMessage = null;
        return true;
    }

    // utility to check inputs are within the limits of the grid size (3 x 3) and allowed pieces (1 - 9)
    private static bool IsWithinBounds(int row, int col, int value)
    {
        return row >= 1 && row <= 3 &&
                col >= 1 && col <= 3 &&
                value >= 1 && value <= 9;
    }

    // utility to check if a square is available for a move (no piece value on it)
    private static bool IsSquareAvailable(IBoard board, int row, int col)
    {
        var square = board.GetSquare(row, col);
        return !square.IsOccupied;
    }

    // utility to check if move value is in the allocated sets of each player i.e. odds or evens
    private static bool IsValidPieceRangeForPlayer(int value, Player player)
    {
        // for player 1 (Id:1), a modulus of 1 means an odd number, and a modulus of 0 means an 
        // even number for player 2 (Id:2)
        bool isOdd = value % 2 == 1;
        //return player.Id == 1 && isOdd || player.Id == 2 && !isOdd;
        bool isValid = (player.Id == 1 && isOdd) || (player.Id == 2 && !isOdd);

        return isValid;
    }

    // utility to check if in game over state (won or draw)
    public bool IsGameOver()
    {
        return _gameState.GameStatus == GameStatus.WON || _gameState.GameStatus == GameStatus.DRAW;
    }

    // utility to check if game is a draw
    public bool IsStalemate()
    {

        bool allSquaresFilled = _board.GetSquares().All(s => s.IsOccupied);
        return allSquaresFilled && !IsGameWon();
    }

    // utility to check if a game is won
    public bool IsGameWon()
    {
        return GetWinner(_board) != null;
    }

    // returns the player ID of the winner
    public int? GetWinner(IBoard board)
    {
        int winningSum = 15; // Numerical TicTacToe specific winning condition rule
        {
            // check all rows and cols for a winning sum
            for (int i = 0; i < board.Size; i++)
            {
                if (SumRow(board, i) == winningSum) return board.GetSquare(i, 0).Piece?.Owner?.Id;
                if (SumCol(board, i) == winningSum) return board.GetSquare(0, i).Piece?.Owner?.Id;
            }
            // check both diagonals for a winning sum (main starts at col 0, opposite at last grid col)
            if (SumMainDiagonal(board) == winningSum) return board.GetSquare(0, 0).Piece?.Owner?.Id;
            if (SumOppDiagonal(board) == winningSum) return board.GetSquare(0, board.Size - 1).Piece?.Owner?.Id;

            return null; // reaching here indicates no winner yet
        }
    }

    /* Sum a grid row - 3 possible rows - grid positions: row1: {(0,0) (0,1) (0,2)} row2: {(1,0) (1,1) (1,2)}
       row3: {(2,0) (2,1) (2,2)} */
    private static int SumRow(IBoard board, int row)
    {
        int sum = 0;

        for (int col = 0; col < board.Size; col++)
        {
            sum += (int?)board.GetSquare(row, col).Piece?.Value ?? 0; // default to 0 if any square empty
        }

        return sum;
    }

    /* Sum a grid col - 3 possible cols - grid positions: col1: {(0,0) (1,0) (2,0)} col2: {(0,1) (1,1) (2,1)}
       col3: {(0,2) (1,2) (2,2)} */
    private static int SumCol(IBoard board, int col)
    {
        int sum = 0;

        for (int row = 0; row < board.Size; row++)
        {
            sum += (int?)board.GetSquare(row, col).Piece?.Value ?? 0; // default to 0 if square empty
        }
        return sum;
    }

    // Sum the main diagonal - grid positions: (0,0) (1,1) (2,2)
    private static int SumMainDiagonal(IBoard board)
    {
        int sum = 0;

        for (int i = 0; i < board.Size; i++)
        {
            sum += (int?)board.GetSquare(i, i).Piece?.Value ?? 0; // default to 0 if any square empty
        }

        return sum;
    }

    // Sum the opposite diagonal - grid positions: (0,2) (1,1) (2,0)
    private static int SumOppDiagonal(IBoard board)
    {
        int sum = 0;

        for (int row = 0; row < board.Size; row++)
        {
            int col = board.Size - 1 - row;
            sum += (int?)board.GetSquare(row, col).Piece?.Value ?? 0; // default to 0 if any square empty
        }

        return sum;
    }
}
