using Framework.Core;
using Framework.Interfaces;
namespace Framework.NumTicTacToe;

/* Concrete implementation of IPiece for Numerical TicTacToe.  */

// primary constructor syntax
public class NumTicTacToePiece(int value, Player owner) : IPiece
{
    // properties
    public int Value { get; } = value; // the specific Numerical TicTacToe piece's integer value
    object IPiece.Value => Value; // explicit Value implementation (IPiece.Value)
    public Player Owner { get; private set; } = owner;
}
