using System.Collections.Generic;
using Framework.Core;
namespace Framework.Interfaces;

/* Interface defining any two player boardgame in the framework. Inherits IGameSerializer
   to provide a common interface for serialization operations of a board during a game. */

public interface IBoardGame : IGameSerializer
{
    // common properties all games must have
    IBoard Board { get; internal set; }
    List<Player> Players { get; }
    IGameRules Rules { get; }
    MoveHistory MoveHistory { get; }

    // method signatures
    bool MakeMove(IMove move);
}
