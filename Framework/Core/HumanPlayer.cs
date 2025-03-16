using System;
using Framework.Enums;
using Framework.Interfaces;
namespace Framework.Core;

/* Extends base class Player to define human-specific player functionality */

public class HumanPlayer : Player
{
    //constructor - delegates setting common fields to the base constructor 
    public HumanPlayer(string name, int id) : base(name, id)
    {
        Type = PlayerType.HUMAN;
    }

    // returns a move from a human player with the piece data input via an array for different move formats
    internal override IMove GetMove(string[] moveParts, Player currentPlayer)
    {
        // check that the currentPlayer argument matches the current player reference
        if (currentPlayer != this) throw new ArgumentException("Current player mismatch, no move returned.");
        return BoardGameFramework.GetFactory().CreateMove(moveParts, this);
    }
}
