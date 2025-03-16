using Framework.Core;
namespace Framework.Interfaces;

/* Defines an interface for different piece types used in varying boardgames. */

public interface IPiece
{
    // common properties all pieces must have
    object Value { get; } // base object type allows any piece value type for extensibility
    Player Owner { get; internal set; }
}



