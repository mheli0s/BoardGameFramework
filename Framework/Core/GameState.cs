using System;
using System.Collections.Generic; // for List<> 
using System.IO;   // for File operations
using System.Linq;  // .Select() queries etc
using System.Threading;
using Framework.Enums;
using Framework.Interfaces;
namespace Framework.Core;

/* Represents and tracks all dynamic point-in-time state data available within a running game.
   Uses the IGameSerialize interface for uniform deserialization of a saved game state snapshot. */

public class GameState
{
    // fields and composition references
    private readonly ConsoleUI _ui;
    private IBoardGame? _game;
    private GameStatus _gameStatus;
    private int _turnNumber; // turn tracking, undo/redo usage, game rule checks
    private GameMode _gameMode;
    private IBoard? _boardSnapshot; // hold point-in-time board state reference
    private List<Player> _players;
    const string saveDirectory = "Game_saves";
    private string _serializedSnapshotFilename;

    // events for Observer pattern based state changes - subscribers can register for event notifications from these
    public event Action<GameStatus>? GameStatusChanged;
    public event Action<int>? TurnChanged;
    public event Action<GameMode>? GameModeChanged;

    //properties
    public int CurrentPlayerIndex { get; internal set; } // index for internal turn and player management
    public string SerializedSnapshotFilename => _serializedSnapshotFilename; // external access to file
    public IBoardGame? Game => _game;
    internal IBoardGame? CurrentGame
    {
        get => _game;
        set => _game = value;
    }

    // properties that can provide event notifications
    public GameStatus GameStatus
    {
        get => _gameStatus;
        set
        {
            if (_gameStatus != value)
            {
                _gameStatus = value;
                GameStatusChanged?.Invoke(_gameStatus); // raises the status change event that subscribers will be notified of
            }
        }
    }
    public GameMode GameMode
    {
        get => _gameMode;
        set
        {
            if (_gameMode != value)
            {
                _gameMode = value;
                GameModeChanged?.Invoke(_gameMode); // raises the status change event that subscribers will be notified of
            }
        }
    }

    public int TurnNumber
    {
        get => _turnNumber;
        internal set
        {
            if (_turnNumber != value)
            {
                _turnNumber = value;
                TurnChanged?.Invoke(_turnNumber); // raises the status change event that subscribers will be notified of
            }
        }
    }


    // construct the GameState instance with required local instance references, default state data etc.
    public GameState(ConsoleUI ui)
    {
        _ui = ui;
        _players = []; // create an initialized, empty list of players

        // create default save path that will be updated when Game is set
        _serializedSnapshotFilename = Path.Combine(saveDirectory, "Snapshot.txt");

    }

    // updates the saved filename to include game type after new game instance fully initialized
    public void UpdateGameSavePath(IBoardGame game)
    {
        CurrentGame = game; // ensure game instance is stored correctly
        _game = Game;

        // create a cross-platform filesystem path format to save the file: '{GameType}-GameSnapshot.txt'
        // prepends the current game type to the default filename
        string savedGameType = game.GetType().Name ?? "UnknownGame";
        _serializedSnapshotFilename = Path.Combine(saveDirectory, $"{savedGameType}-Snapshot.txt");
    }

    // Set all game state elements to default initial value for a new game
    internal void InitializeNewGameState(IBoardGame game)
    {
        if (game == null || game.Board == null)
        {
            throw new ArgumentException("Cannot initialize game state: Game or board is null.");
        }


        _boardSnapshot = game.Board.Clone(); // save initial state
        _players = new List<Player>(game.Players.Count); // set _players to an empty list with space for number of game players

        /* Deep copy of each player in the List to maintain state independence (to ensure no state corruption from shared player references) */
        foreach (var player in game.Players)
        {
            _players.Add(player.Clone());
        }

        UpdateGameStatus(GameStatus.IN_PROGRESS);
        TurnNumber = 0;
        CurrentPlayerIndex = 0;
    }

    // Update game status and clone the latest deep copy state snapshot after each move made (player+board)  
    internal void UpdateStateAfterMove(IBoardGame game)
    {
        _game = game; // updat local reference

        // Check if the game is won
        if (game.Rules.IsGameWon())
        {
            UpdateGameStatus(GameStatus.WON);
        }

        // Check if the game is a draw
        else if (game.Rules.IsStalemate())
        {
            UpdateGameStatus(GameStatus.DRAW);
        }

        else
        {
            TurnNumber++; // raises the OnTurnChanged event and tracks game progression
            //OnTurnChanged(TurnNumber);

            // update the board snapshot with the latest state
            _boardSnapshot = game.Board.Clone();

            for (int i = 0; i < game.Players.Count; i++)
            {
                _players[i] = game.Players[i].Clone();
            }
        }
    }

    // Create a deep copy of all game state elements in a current game
    internal GameState Clone()
    {
        if (_boardSnapshot == null)
        {
            throw new InvalidOperationException("Cannot clone GameState: Board snapshot is null. Ensure game is initialized before loading.");
        }

        // create new GameState instance to save the state so the current state remains independent
        GameState clonedGameState = new(_ui)
        {
            _boardSnapshot = _boardSnapshot.Clone(),
            // LINQ mapping query to clone each player into _players
            _players = _players.Select(p => p.Clone()).ToList(),
            GameStatus = GameStatus,
            CurrentPlayerIndex = CurrentPlayerIndex,
            TurnNumber = TurnNumber,
        };

        // ff the game exists, create a reference to it in the cloned state
        if (_game != null)
        {
            // we need to keep the reference to the same game instance
            clonedGameState._game = _game;
        }

        return clonedGameState;
    }

    internal void Save() // TODO: use factory method pattern to call SaveGameToDisk()/etc?
    {
        // Save the current board state when saving a game (not the full history):
        // When saving to disk, only persist the current GameState
        try
        {
            if (!Directory.Exists(saveDirectory))
            {
                Directory.CreateDirectory(saveDirectory);
            }

            // extract the name of the class instance to prefix the saved filename with for filesystem transparency
            string gameTypeString = _game!.GetType().Name;

            // remove the "Game" portion of the class for the saved name format
            if (gameTypeString.EndsWith("Game"))
            {
                gameTypeString = gameTypeString.Substring(0, gameTypeString.Length - 4);
            }

            // set the saved game filename with a cross-platform format and the game type prepended
            _serializedSnapshotFilename = Path.Combine(saveDirectory, $"{gameTypeString}-Snapshot.txt");

            // use the IGameSerialize interface for uniform serialization
            var serializedSnapshotData = _game?.Serialize();

            // check if the state save failed, created an empty file
            if (string.IsNullOrEmpty(serializedSnapshotData))
            {
                ConsoleUI.DisplayErrorMessage("Failed to serialize game data.");
                return;
            }

            // save file - overwrites contents since we only save one snapshot to disk at a time
            File.WriteAllText(_serializedSnapshotFilename, serializedSnapshotData ?? "");
            ConsoleUI.DisplayInfoMessage($"\nGame saved successfully to {_serializedSnapshotFilename}.");
        }
        catch (Exception e)
        {
            ConsoleUI.DisplayErrorMessage($"\nFailed to save the current game: {e.Message}");
        }
    }

    // deserialize a game state loaded from file to initialize the board with the parsed data, reset history 
    internal void Load()
    {
        try
        {
            if (!File.Exists(_serializedSnapshotFilename))
            {
                ConsoleUI.DisplayErrorMessage($"No game found at {_serializedSnapshotFilename}");
                return;
            }

            string loadedGameData = File.ReadAllText(_serializedSnapshotFilename);
            if (string.IsNullOrWhiteSpace(loadedGameData))
            {
                ConsoleUI.DisplayErrorMessage($"\nLoaded game file {_serializedSnapshotFilename} is empty");
                Thread.Sleep(500);
                return;
            }

            if (_game == null)
            {
                ConsoleUI.DisplayErrorMessage("\nGame is not initialized. Please start a new game first.");
                return;
            }

            // deserialize the loaded snapshot file
            _game.Deserialize(loadedGameData);

            // update the players list in GameState from the game's current players
            _players = _game.Players.Select(p => p.Clone()).ToList();

            // update the board snapshot to reflect the loaded state
            _boardSnapshot = _game.Board.Clone();

            // refresh state references and board
            SynchronizeReferences();

            Console.Clear();
            ConsoleUI.DisplayBoard(_game.Board);
            ConsoleUI.DisplayInfoMessage($"\nSuccessfully loaded game from {_serializedSnapshotFilename}");

            // clear move history
            _game.MoveHistory.ClearHistory();
        }
        catch (Exception e)
        {
            ConsoleUI.DisplayErrorMessage($"\nCouldn't load game: {e.Message}");
            if (e.StackTrace != null)
            {
                ConsoleUI.DisplayErrorMessage($"Stack trace: {e.StackTrace}");
            }
            // reset state
            //ResetGameState();
        }
    }


    // keep all current state references synced after any state update
    public void SynchronizeReferences()
    {
        // ensure the game reference is set
        if (_game == null) return;

        // ensure player references are consistent
        for (int i = 0; i < _players.Count && i < _game.Players.Count; i++)
        {
            // store a reference to both the source and target player for clarity
            var sourcePlayer = _players[i];
            var targetPlayer = _game.Players[i];

            // ensure pieces owned by players are properly linked
            foreach (var piece in sourcePlayer._remainingPieces)
            {
                piece.Owner = sourcePlayer;
            }

            // clear the target player's remaining pieces before copying
            targetPlayer._remainingPieces.Clear();

            // copy remaining pieces from source state to active game state - create new pieces rather than reference the old ones
            foreach (var sourcePiece in sourcePlayer._remainingPieces)
            {
                // create a new piece with the same properties but owned by the target player
                var newPiece = BoardGameFramework.GetFactory()
                    .CreatePiece((int)sourcePiece.Value, targetPlayer);

                // add the new piece to the target player's collection
                targetPlayer._remainingPieces.Add(newPiece);
            }
        }

        // update board state if needed
        if (_boardSnapshot != null)
        {
            // ensure board squares are properly linked with pieces
            for (int row = 0; row < _boardSnapshot.Size; row++)
            {
                for (int col = 0; col < _boardSnapshot.Size; col++)
                {
                    var sourceSquare = _boardSnapshot.GetSquare(row, col);
                    var targetSquare = _game.Board.GetSquare(row, col);

                    // first, clear the target square regardless of whether we're setting a new piece
                    targetSquare.ResetSquare();

                    // only attempt to place a piece if the source square has one
                    if (sourceSquare.IsOccupied && sourceSquare.Piece != null)
                    {
                        var sourcePiece = sourceSquare.Piece;
                        var owner = _game.Players.FirstOrDefault(p => p.Id == sourcePiece.Owner.Id);

                        if (owner != null)
                        {
                            try
                            {
                                // create a new piece with the correct type and properties
                                var newPiece = BoardGameFramework.GetFactory()
                                    .CreatePiece((int)sourcePiece.Value, owner);

                                // use the explicit interface to ensure type compatibility -
                                // utilizes the internal type checking in the SetPiece method
                                ((IBoardSquare)targetSquare).SetPiece(newPiece);
                            }
                            catch (ArgumentException ex)
                            {
                                // log the error but continue with synchronization
                                Console.WriteLine($"Error synchronizing piece at [{row},{col}]: {ex.Message}");
                            }
                        }
                    }
                }
            }
        }
    }

    // utility method for getting current game status
    internal GameStatus GetGameStatus()
    {
        return GameStatus;
    }

    // restore just the state date fields to default values. Players/Board keep previous state
    internal void ResetGameState()
    {
        UpdateGameStatus(GameStatus.IN_PROGRESS);
        TurnNumber = 0;
        CurrentPlayerIndex = 0;
    }

    // utility method for getting the current player via its index
    internal Player GetCurrentPlayer()
    {
        return _players[CurrentPlayerIndex];
    }

    // utility method for getting the turn number for the current move
    internal int GetCurrentTurnNumber()
    {
        return TurnNumber;
    }

    // utility to update game status
    internal void UpdateGameStatus(GameStatus newStatus)
    {
        GameStatus = newStatus;
    }
}
