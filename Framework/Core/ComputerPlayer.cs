using System;
using Framework.Enums;
using Framework.Interfaces;
namespace Framework.Core;

/* Extends base Player class to define a computer player */

public class ComputerPlayer : Player
{
    // constructor to create a computer player - delegates setting common fields to the base class constructor
    public ComputerPlayer(string name, int id, IMoveStrategy strategy) : base(name, id)
    {
        AIStrategy = strategy ?? throw new ArgumentNullException(nameof(strategy),
                                                "Strategy cannot be null for computer player");
        Type = PlayerType.COMPUTER;
    }

    // Generate a move for a computer player using a move strategy
    internal override IMove GetMove(IBoard board)
    {
        if (AIStrategy == null) throw new ArgumentNullException("Computer player has no move strategy assigned.");

        return AIStrategy.GenerateMove(board, this);
    }


    // utility to set a new move strategy for use by the computer player
    internal void SetStrategy(IMoveStrategy strategy)
    {
        AIStrategy = strategy ?? throw new ArgumentNullException("New strategy is null.");
    }
}