namespace Framework.Interfaces;

/* Defines a grid cell on a game board */

public interface IBoardSquare
{
    int Row { get; }
    int Col { get; }
    bool IsOccupied { get; }
    IPiece? Piece { get; }
    void SetPiece(IPiece piece);
    IPiece? RemovePiece();
    void ResetSquare();
}