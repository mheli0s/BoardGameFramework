namespace Framework.Interfaces;

/* Interface for defining common game rules for any boardgame */

public interface IGameRules
{
    (bool isValid, string? errorMessage) IsValidMove(IBoard board, IMove move);
    bool IsValidMoveFormat(string[] moveParts, out string? errorMessage);
    int? GetWinner(IBoard board);
    bool IsGameOver();
    bool IsGameWon();
    bool IsStalemate();
}