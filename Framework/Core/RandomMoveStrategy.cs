using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Interfaces;
namespace Framework.Core;

/* Implements IMoveStrategy to define a simple System.Random() move generation strategy
 * with no complex logic to improve the AI's move choice. 
 * Note: this is part of a strategy design pattern used by the game factory for creating
 * different strategies based on difficulty levels. 
 */

internal class RandomMoveStrategy : IMoveStrategy
{
    private readonly Random _random = new(); // RNG to produce a random number for move 

    /* Keeps an up-to-date list of vacant squares and pieces still available for making a move and 
     * uses the RNG to randomly generate a move from that square/piece pool to return for making 
     *  the computer's next move. 
     */

    public IMove GenerateMove(IBoard board, Player player)
    {
        if (board == null)
            throw new ArgumentNullException(nameof(board), "Board cannot be null");

        if (player == null)
            throw new ArgumentNullException(nameof(player), "Player cannot be null");

        List<IBoardSquare> availableSquares = board.GetSquares()
            .Where(s => !s.IsOccupied)
            .ToList();

        // create a HashSet of already used piece values
        var usedPieces = board.GetSquares()
            .Where(s => s.IsOccupied && s.Piece != null && s.Piece.Value != null)
            .Select(s => (int)s.Piece!.Value!)
            .ToHashSet();

        // ensure AI only selects from remaining pieces that are NOT on the board
        var availablePieces = player._remainingPieces
            .Select(p => (int)p.Value)
            .Where(value => !usedPieces.Contains(value)) // exclude pieces already on board
            .ToList();

        if (!availablePieces.Any())
        {
            throw new InvalidOperationException("No valid pieces left to generate moves.");
        }

        var selectedSquare = availableSquares[_random.Next(availableSquares.Count)];
        var selectedValue = availablePieces[_random.Next(availablePieces.Count)];
        var selectedPiece = BoardGameFramework.GetFactory().CreatePiece(selectedValue, player);

        if (selectedPiece == null)
        {
            throw new InvalidOperationException($"Failed to create a piece for value {selectedValue}.");
        }

        string[] moveParts =
        [
            "m",
        (selectedSquare.Row + 1).ToString(),
        (selectedSquare.Col + 1).ToString(),
        selectedPiece?.Value?.ToString() ?? "0"
        ];

        IMove move = BoardGameFramework.GetFactory().CreateMove(moveParts, player);

        return move;
    }
}
