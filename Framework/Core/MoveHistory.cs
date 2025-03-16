using System.Collections.Generic;
using System.Linq;
using Framework.Interfaces;
namespace Framework.Core;

/* Tracks the sequence of moves throughout a game, which provides for undo/redo functionality */

// primary constructor syntax
public class MoveHistory(GameState gameState)
{
    /* The log of moves is a List of (move, state) tuple pairs - tuple usage allows simpler 
     * and clearer access for operating on each tuple element (Move and State). Implementing
     * the move history using a List structure also offers benefits over a Stack since its
     * direct indexing allows direct access to any element making undo/redo operations simpler
     * to implement as well.
     */
    private readonly List<(IMove Move, GameState State)> _moveLog = [];
    private readonly GameState _initialGameState = gameState.Clone(); // used if undoing back to initial empty state
    private int _currentMoveIndex = -1; // Initial value of -1 indicates no moves in history log yet


    // Add the current move to the move history log after filtering out data due to any undo operations
    internal void AddMoveToHistory(IMove move, GameState updatedState)
    {
        /* Before adding a move to the log, check if we're at the end of the list. 
         * If not, this means undo operations have occurred. Therefore, truncate 
         * the list by removing all future moves ahead of it to eliminate stale 
         * move state corrupting the history. 
         */
        if (_currentMoveIndex < _moveLog.Count - 1)
        {
            _moveLog.RemoveRange(_currentMoveIndex + 1, _moveLog.Count - _currentMoveIndex - 1);
        }

        move.MoveNumber = _moveLog.Count + 1; // current number of moves in 1-based indexing
        _moveLog.Add((move, updatedState.Clone())); // add the new Move/State tuple to the history
        _currentMoveIndex = _moveLog.Count - 1; // update move index via 0-based indexing
    }

    // Undo a move by returning to previous move's state as far back as the initial state before any moves
    internal GameState UndoMove()
    {
        // no moves made yet
        if (_currentMoveIndex < 0)
        {
            ConsoleUI.DisplayErrorMessage("No moves in history.");
            return _initialGameState;
        }

        // undone back to first move - set initial state again 
        if (_currentMoveIndex == 0)
        {
            ConsoleUI.DisplayErrorMessage("No further moves in history to undo.");
            _currentMoveIndex = -1; // resets back to default initial value
            return _initialGameState;
        }
        // still moves in history, so return previous move's state
        _currentMoveIndex--;
        return _moveLog[_currentMoveIndex].State.Clone(); // return a copy to preserve original state

    }

    // reapplies a move previously undone until reaching the last move in history (all undone moves reapplied)
    internal GameState RedoMove()
    {
        if (_currentMoveIndex < _moveLog.Count - 1)
        {
            _currentMoveIndex++;
            return _moveLog[_currentMoveIndex].State.Clone();
        }
        ConsoleUI.DisplayErrorMessage("No further moves in history to redo.");
        return _initialGameState;
    }

    // utility to return a list of all moves made in a current game
    internal List<IMove> GetAllMoves()
    {
        return _moveLog.Select(m => m.Move).ToList();
    }

    // utility to retrieve the index of the current move
    internal int GetCurrentMoveIndex()
    {
        return _currentMoveIndex;
    }

    // utility to return the current Move object instance
    internal IMove GetCurrentMove()
    {
        _currentMoveIndex = GetCurrentMoveIndex();
        return _moveLog[_currentMoveIndex].Move;
    }

    // utility to return the total current moves made
    internal int GetMoveCount()
    {
        return _moveLog.Count;
    }

    // utility to clear the move history back to an empty, initial state
    internal void ClearHistory()
    {
        _moveLog.Clear();
        _currentMoveIndex = -1;
    }
}