using System;
using System.Collections.Generic; // for List<> 
using System.IO;   // for File operations
using System.Linq;  // .Select() queries etc
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
        _serializedSnapshotFilename = Path.Combine(saveDirectory, "GameSnapshot.txt");

    }

    // updates the saved filename to include game type after new game instance fully initialized
    public void UpdateGameSavePath(IBoardGame game)
    {
        CurrentGame = game; // ensure game instance is stored correctly
        _game = Game;

        // create a cross-platform filesystem path format to save the file: '{GameType}-GameSnapshot.txt'
        // prepends the current game type to the default filename
        string savedGameType = game.GetType().Name ?? "UnknownGame";
        _serializedSnapshotFilename = Path.Combine(saveDirectory, $"{savedGameType}-GameSnapshot.txt");
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
            _game = _game
        };

        return clonedGameState;
    }

    internal void SaveGame() // TODO: use factory method pattern to call SaveGameToDisk()/etc?
    {
        // Save the current board state when saving a game (not the full history):
        // When saving to disk, only persist the current GameState
        try
        {
            if (!Directory.Exists(saveDirectory))
            {
                Directory.CreateDirectory(saveDirectory);
            }

            string gameTypeString = _game!.GetType().Name;
            _serializedSnapshotFilename = $"{gameTypeString}-GameSnapshot.txt";

            // use the IGameSerialize interface for uniform serialization
            var serializedSnapshotData = _game?.Serialize();

            // save file - overwrites contents since we only save one snapshot to disk at a time
            File.WriteAllText(_serializedSnapshotFilename, serializedSnapshotData ?? "");
            ConsoleUI.DisplayInfoMessage($"Game saved successfully to {_serializedSnapshotFilename}.");
        }
        catch (Exception e)
        {
            ConsoleUI.DisplayErrorMessage($"Failed to save the current game: {e.Message}");
        }
    }

    // deserialize a game state loaded from file to initialize the board with the parsed data, reset history 
    internal void LoadGame()
    {
        // try to load the saved game file, and parse it into a game state, display errors if fails
        try
        {
            if (!File.Exists(_serializedSnapshotFilename))
            {
                ConsoleUI.DisplayErrorMessage($"No game found at {_serializedSnapshotFilename}");
                return;
            }

            string[] loadedGameSnapshot = File.ReadAllLines(_serializedSnapshotFilename);
            if (_serializedSnapshotFilename.Length == 0)
            {
                ConsoleUI.DisplayErrorMessage("Loaded game is empty");
                return;
            }

            if (_game == null)
            {
                ConsoleUI.DisplayErrorMessage("Game is not initialized. Please start a new game first.");
                return;
            }

            // Reset current board's state data, but we reuse same players and board with the reset state
            ResetGameState();

            if (_game.Board == null)
            {
                ConsoleUI.DisplayErrorMessage("Board is not initialized.");
                return;
            }

            _boardSnapshot = _game.Board.Clone();


            // Convert the loadedGameSnapshot array into a single string by concatenating each string
            // in the array, delimited by newline chars, then deserialise the resulting joined string.
            _game?.Deserialize(string.Join(Environment.NewLine, loadedGameSnapshot));

            // display the loaded state on the console board if not null otherwise throw an error.
            ConsoleUI.DisplayBoard(_game?.Board ?? throw new InvalidOperationException(
                                                                    "Game uninitialized."));

            // also reset the move history when loading a saved game
            _game.MoveHistory.ClearHistory();

            ConsoleUI.DisplayInfoMessage($"Successfully loaded game from {_serializedSnapshotFilename}");
        }

        catch (Exception e)
        {
            ConsoleUI.DisplayErrorMessage($"Couldn't load game: {e.Message}");
            ResetGameState(); // re-initialize state if fail to load a saved game
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
