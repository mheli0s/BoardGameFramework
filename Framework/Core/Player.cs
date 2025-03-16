using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Enums;
using Framework.Interfaces;
namespace Framework.Core;

/* Abstract base class defining generic player types and their related behaviours */

// primary constructor syntax
public abstract class Player(string name, int id)
{
    // fields and properties
    public string Name { get; protected set; } = name;
    public int Id { get; protected set; } = id;
    public PlayerType Type { get; protected set; } // player type for turn related logic
    public IMoveStrategy? AIStrategy { get; protected set; } // access to the current computer move strategy
    public List<IPiece> _remainingPieces = []; // track player's remaining playable pieces

    /* default overloaded virtual definitions for retrieving any move format from any player 
       type - can be overriden by subclasses if needed */

    // for computer players
    internal virtual IMove GetMove(IBoard board)
    {
        throw new NotImplementedException("Not supported by this player type.");
    }

    // for human players - allows for flexible move input formats
    internal virtual IMove GetMove(string[] moveParts, Player currentPlayer)
    {
        throw new NotImplementedException("Not supported by this player type.");
    }

    // for a fixed human move input format
    internal virtual IMove GetMove(int row, int col, int value)
    {
        throw new NotImplementedException("Not supported by this player type.");
    }

    public virtual bool UsePiece(int value)
    {
        var pieceToRemove = _remainingPieces.FirstOrDefault(p => Convert.ToInt32(p.Value) == value);

        if (pieceToRemove != null)
        {
            _remainingPieces.Remove(pieceToRemove);
            return true;
        }
        else
        {
            return false;
        }
    }

    // create a deep copy of the current player
    internal Player Clone()
    {
        // create a clone of the current player based on its type
        Player clonedPlayer = GetType() switch
        {
            Type t when t == typeof(HumanPlayer) => new HumanPlayer(Name, Id),
            Type t when t == typeof(ComputerPlayer) => new ComputerPlayer(Name, Id, AIStrategy
                    ?? throw new InvalidOperationException("AIStrategy is null for computer player")),
            _ => throw new InvalidOperationException($"Unknown player type: {GetType().Name}")
        };

        // also clone the remaining pieces needed to restart a saved game with
        clonedPlayer._remainingPieces = _remainingPieces
                                        .Select(p => BoardGameFramework.GetFactory()
                                        .CreatePiece((int)p.Value, clonedPlayer))
                                        .ToList();

        return clonedPlayer;
    }

    // utility to check if potential move piece is already used on the board
    public virtual bool HasPiece(int value)
    {
        return _remainingPieces.Any(p => p.Value.Equals(value));
    }

    // rest pieces to default state for use in playing a new round
    public void ResetPieces()
    {
        // clear any existing pieces first
        _remainingPieces.Clear();

        // get the factory instance
        var factory = BoardGameFramework.GetFactory();

        // for player 1 (odd numbers)
        if (Id == 1)
        {
            // reset available pieces to odd numbers 1-9
            int[] oddValues = [1, 3, 5, 7, 9];
            foreach (int value in oddValues)
            {
                _remainingPieces.Add(factory.CreatePiece(value, this));
            }
        }
        // for player 2 (even numbers)
        else if (Id == 2)
        {
            // reset available pieces to even numbers 2-8
            int[] evenValues = [2, 4, 6, 8];
            foreach (int value in evenValues)
            {
                _remainingPieces.Add(factory.CreatePiece(value, this));
            }
        }
    }
}