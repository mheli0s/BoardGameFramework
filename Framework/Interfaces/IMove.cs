using Framework.Core;
namespace Framework.Interfaces;

/* Base interface defining a basic move in a game */

public interface IMove
{
    // common properties all moves must have
    int Row { get; }
    int Col { get; }
    object Value { get; } // base object type allows any move value type for extensibility
    Player Owner { get; }
    int MoveNumber { get; set; } // tracking move order for undo/redo, move history etc.
}