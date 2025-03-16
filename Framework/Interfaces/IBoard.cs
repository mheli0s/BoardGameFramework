using System.Collections.Generic;
using Framework.Core;
namespace Framework.Interfaces;

/* Interface to define a game board structure and its operations */

public interface IBoard
{
    int Size { get; }
    void PlacePiece(int row, int col, IPiece piece, Player owner);
    IBoardSquare GetSquare(int row, int col);
    void SetSquare(int row, int col, IBoardSquare square);
    IEnumerable<IBoardSquare> GetSquares();
    void ResetAllSquares();
    IBoard Clone();
}
