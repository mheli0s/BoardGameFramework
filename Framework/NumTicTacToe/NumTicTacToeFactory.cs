using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Core;
using Framework.Enums;
using Framework.Interfaces;
using NumTPiece = Framework.NumTicTacToe.NumTicTacToePiece; // shorter type alias
namespace Framework.NumTicTacToe;

/* Concrete implementation of IGameFactory. This is a game factory which forms part of 
 * the abstract factory design pattern as a concrete factory creator. It uses a number of
 * methods (factory methods pattern) to create the main components of a Numerical TicTacToe game. 
 */

// primary constructor syntax
public class NumTicTacToeFactory : IGameFactory
{
    // constants for player IDs
    private const int PLAYER1_ID = 1;
    private const int PLAYER2_ID = 2;

    // factory method for creating a concrete NumTicTacToeBoard implementation of IBoard
    public IBoard CreateBoard()
    {
        return new NumTicTacToeBoard();
    }

    // factory method for creating a concrete NumTicTacToeRules implementation of IGameRules
    public IGameRules CreateRules(GameState gameState, IBoard board)
    {
        return new NumTicTacToeRules(gameState, board);
    }

    // factory method for creating a concrete NumTicTacToeMove object after validating piece availability
    public IMove CreateMove(string[] moveParts, Player owner)
    {
        // parse move parts to check game specific format validation
        if (!int.TryParse(moveParts[1], out int row) ||
            !int.TryParse(moveParts[2], out int col) ||
            !int.TryParse(moveParts[3], out int value))
        {
            throw new ArgumentException("Invalid move format.");
        }

        // adjust row and col inputs since we use 0-based indexing of grid position values internally
        row--;
        col--;

        // create the piece and check if player still has piece available in remaining pieces
        CreatePiece(value, owner);

        if (!owner.HasPiece(value))
        {
            ConsoleUI.DisplayErrorMessage($"\n{owner.Name} doesn't have piece {value}");
            Console.WriteLine();
            return null!;
        }

        return new NumTicTacToeMove(row, col, value, owner);
    }

    /* Factory method to create the concrete AI move strategy dynamicaly based on a chosen difficulty
       level using a strategy design pattern */
    public IMoveStrategy CreateMoveStrategy(GameDifficulty level)
    {
        var strategy = level switch
        {
            GameDifficulty.EASY => new RandomMoveStrategy(),
            // ...other future EASY/MEDIUM/HARD strategies...
            _ => new RandomMoveStrategy()  // default
        };

        return strategy ?? throw new InvalidOperationException("Failed to create move strategy.");
    }

    // factory method for creating concrete NumericalTicTacToePiece objects
    public IPiece CreatePiece(int value, Player owner)
    {
        if (owner == null)
        {
            throw new ArgumentNullException(nameof(owner), "Piece Owner cannot be null");
        }

        return new NumTPiece(value, owner);
    }

    // overloaded factory method to create human players
    public Player CreatePlayer(string name, int id, bool isHuman)
    {
        if (!isHuman)
        {
            throw new ArgumentException("Use the CreatePlayer method overload with strategy arg for computer.");
        }
        return CreateEachPlayer(name, id, isHuman);
    }

    // overloaded factory method for computer player creation with strategy
    public Player CreatePlayer(string name, int id, IMoveStrategy strategy)
    {
        return strategy == null ? throw new ArgumentNullException(nameof(strategy))
                                : CreateEachPlayer(name, id, false, strategy);
    }

    // Private factory method that handles the actual player creation
    private Player CreateEachPlayer(string name, int id, bool isHuman = true, IMoveStrategy? strategy = null)
    {
        if (!isHuman && strategy == null)
        {
            throw new ArgumentNullException(nameof(strategy), "Strategy required for computer player");
        }

        var values = id == PLAYER1_ID ? new[] { 1, 3, 5, 7, 9 } : new[] { 2, 4, 6, 8 };

        // Create appropriate player type with validation
        Player player;
        if (isHuman)
        {
            player = new HumanPlayer(name, id);
        }
        else
        {
            player = new ComputerPlayer(name, id, strategy!);
        }

        // Create and assign pieces
        var pieces = values.Select(v => CreatePiece(v, player)).ToList();
        player._remainingPieces = pieces;

        return player;
    }

    // factory method that creates both players in a game dynamically, based on chosen GameMode, into a list
    private List<Player> CreatePlayers(bool isHumanVsComputer)
    {
        var players = new List<Player>
        {
            // player 1 (ID:1) is always human and uses odd piece numbers: (1,3,5,7,9)
            CreatePlayer("Human1", PLAYER1_ID, isHuman: true)
        };

        // player 2 (ID:2) is either a computer player or human and uses even numbers: (2,4,6,8)
        if (isHumanVsComputer)
        {
            // creata a computer player and sets its move strategy via the straegy pattern
            IMoveStrategy strategy = CreateMoveStrategy(GameDifficulty.EASY) ??
                            throw new InvalidOperationException("Failed to create AI move strategy.");
            players.Add(CreatePlayer("Computer", PLAYER2_ID, strategy));
        }
        else // a second human player
        {
            players.Add(CreatePlayer("Human2", PLAYER2_ID, isHuman: true));
        }

        return players;
    }

    /* Factory method that creates a concrete NumTicTacToeGame implementation of IBoardGame. 
       Uses composition to create the main components of a boardgame together. */

    public IBoardGame CreateBoardGame(bool isHumanVsComputer, GameState gameState)
    {
        // create game components and return fully constructed game with null checks
        if (gameState == null)
            throw new ArgumentNullException(nameof(gameState), "Game state cannot be null");

        // Create the players first to ensure they're properly initialized
        var players = CreatePlayers(isHumanVsComputer);
        if (players == null || !players.Any())
            throw new InvalidOperationException("Failed to create players");

        // Create the board
        var board = CreateBoard();
        if (board == null)
            throw new InvalidOperationException("Failed to create game board");

        // Create the rules
        var rules = CreateRules(gameState, board);
        if (rules == null)
            throw new InvalidOperationException("Failed to create game rules");

        // Verify that all critical components are created before constructing the game
        var game = new NumTicTacToeGame(
            board,
            players,
            rules,
            gameState
        );

        return game;
    }

    // concrete factory method showing game rules specific to Numerical Tic-Tac-Toe 
    public string CreateGameRulesInfo()
    {
        return
        """

        Numerical Tic-Tac-Toe Rules:
        ----------------------------
        -This game is a variant of Tic-Tac-Toe using numbers between 1-9 as pieces on a 3x3 board.
        -Player 1 uses odds (1,3,5,7,9) and Player 2 evens (2,4,6,8). 
        -Odds make the first turn. Each piece can only be used once.
        -The goal is to be the first to have a row, column, or diagonal's pieces to sum to 15. 
        -That player wins!
        """;
    }
}