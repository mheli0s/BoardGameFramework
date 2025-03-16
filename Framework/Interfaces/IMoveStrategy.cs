using Framework.Core;
namespace Framework.Interfaces;

/* Interface for defining AI move generation strategies. Uses the strategy design 
 * pattern for applying the correct strategy algorithm for a chosen context. 
 * - RandomMoveStrategy is a concrete strategy implementation
 * - ComputerPlayer is the context class that uses the strategies
 */

public interface IMoveStrategy
{
    // generate a move using a chosen strategy
    IMove GenerateMove(IBoard board, Player player);
}