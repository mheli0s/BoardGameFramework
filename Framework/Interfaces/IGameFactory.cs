using Framework.Core;
using Framework.Enums;
namespace Framework.Interfaces;

/* Interface for defining how to create different families of related 
game components using an abstract factory design pattern. */

public interface IGameFactory
{
    // method signatures
    IBoardGame CreateBoardGame(bool isHumanVsComputer, GameState gameState);
    IBoard CreateBoard();
    Player CreatePlayer(string name, int id, bool isHuman);
    IGameRules CreateRules(GameState gameState, IBoard board);
    IMove CreateMove(string[] moveParts, Player owner);
    IMoveStrategy CreateMoveStrategy(GameDifficulty level);
    IPiece CreatePiece(int selectedValue, Player player);
    string CreateGameRulesInfo();
}