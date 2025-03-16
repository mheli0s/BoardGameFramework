using System;
using System.IO;
using System.Threading;
using Framework.Interfaces;
using Framework.Enums;
using Framework.NumTicTacToe;
namespace Framework.Core;

/* The top-level framework manager for creating the framework itself, the abstract factories 
   for creating game families to create and start games, show framework help or exiting.  */

public sealed class BoardGameFramework
{
    // we only need one framework instance, so implement as a Singleton pattern
    private static BoardGameFramework? _bgfinstance;
    private static readonly Lock _threadSafeLock = new(); // make the Singleton thread-safe
    private readonly ConsoleUI _ui = new();
    private GameState _gameState;
    //private GameMode _gameMode;
    private GameController? _gameController;
    private IGameFactory? _gameFactory;
    private IBoardGame? _game;
    public GameState GameState { get; private set; }


    // private constructor for Singleton design
    private BoardGameFramework()
    {
        _gameState = new GameState(_ui);
        GameState = _gameState;
    }

    // return a Singleton instance with thread-safe creation checking
    public static BoardGameFramework GetBGFInstance()
    {
        // double-check locking: lock thread for instance creation or return existing
        if (_bgfinstance == null)
        {
            lock (_threadSafeLock)
            {   // create only if still null after thread lock
                _bgfinstance ??= new BoardGameFramework();
            }
        }
        return _bgfinstance;
    }

    // process the main framework menu options input via the user 
    internal void StartFramework()
    {
        // flag to control loop 
        bool exitLoop = false;

        // menu options loop
        while (!exitLoop)
        {
            ConsoleUI.DisplayInitialFrameworkScreen();
            FrameworkMenuOption mainMenuChoice =
                (FrameworkMenuOption)Enum.Parse(typeof(FrameworkMenuOption),
                                                ConsoleUI.GetValidFrameworkMenuOption());

            switch (mainMenuChoice)
            {
                // Numerical TicTacToe
                case FrameworkMenuOption.NUMTICTACTOE:
                    ProcessGameOptions();
                    break;
                case FrameworkMenuOption.HELP_PAGE:
                    ConsoleUI.DisplayFrameworkHelp();
                    break;
                case FrameworkMenuOption.EXIT:
                    // cleans up resources and exits the program
                    _gameController?.Dispose();
                    exitLoop = true;
                    break;
            }
        }
        Environment.Exit(0); // exits the program here after option "0" chosen
    }

    // processes user-chosen framework menu options
    internal void ProcessGameOptions()
    {
        bool returnToMain = false; // flag to control loop

        // game menu loop
        while (!returnToMain)
        {
            int gameMenuChoice = ConsoleUI.GetValidGameMenuOption();

            switch (gameMenuChoice)
            {
                case 1: // new game
                    try
                    {
                        // get chosen option and check if a valid GameMode type before starting game with it
                        GameMode? selectedMode = ConsoleUI.GetValidGameModeOption();
                        if (selectedMode.HasValue)
                        {
                            // create factory and game
                            _gameFactory ??= CreateFactory(GameType.NUMTICTACTOE);
                            // Ensure _gameState is initialized
                            _gameState ??= new GameState(_ui);
                            _gameState.GameMode = selectedMode.Value;
                            _game = _gameFactory
                                        .CreateBoardGame(selectedMode.Value == GameMode.HumanVsComputer,
                                                            _gameState);
                            if (_game == null || _game.Board == null)
                            {
                                ConsoleUI.DisplayPausedErrorMessage("Failed to initialize game: game or board is null");
                                return;
                            }
                            // initialize game state references
                            _gameState.CurrentGame = _game;
                            _gameState.InitializeNewGameState(_game);
                            _gameState.UpdateGameSavePath(_game);

                            // create the controller and start the game with it
                            _gameController?.Dispose(); // clean up old controller resources if not null
                            _gameController = new GameController(_game, _gameState);
                            _gameController.InitializeGame();
                            _gameController.PlayNewGame();

                            // _gameController = StartNewGame(GameType.NUMTICTACTOE, _gameMode);
                        }
                    }
                    catch (Exception e)
                    {
                        ConsoleUI.DisplayPausedErrorMessage($"Error starting game: {e.Message}");
                    }
                    break;
                case 2:
                    StartSavedGame();
                    break;
                case 3:
                    if (_gameFactory != null)
                    {
                        ConsoleUI.DisplayGameRules(_gameFactory);
                    }
                    else
                    {
                        ConsoleUI.DisplayPausedErrorMessage("No game factory available to display rules.");
                    }
                    break;
                case 0:
                    Console.Clear();
                    returnToMain = true;
                    break;
            }
        }
    }

    internal void StartSavedGame()
    {
        // file save format: '{GameType}-GameSnapshot.txt'
        /* Try to convert and cast the string to the GameType enum, then call the matching game factory
            to setup game components needed to start the saved game. It grabs the first part of saved 
            filename up to '-' as the string to convert, ignores case and saves the result to savedGameType.
        */
        try
        {
            string filename = _gameState.SerializedSnapshotFilename ?? string.Empty;

            if (!File.Exists(filename))
            {
                ConsoleUI.DisplayPausedErrorMessage("No saved game file found.");
                return;
            }
            // get game type from filename eg. NumTicTacToeGame-GameSnapshot.txt -> NumTicTacToeGame
            string gameTypeString = Path.GetFileName(filename).Split('-')[0]
                                        .Replace("Game", "")  // remove "Game" suffix if Included
                                        .ToUpper();          // convert to match enum case format

            // create the factory matching parsed game type, load saved game and setup state data
            if (Enum.TryParse<GameType>(gameTypeString, ignoreCase: true, out GameType savedGameType))
            {
                _gameFactory = CreateFactory(savedGameType);
                _gameState.LoadGame(); // populate the board with saved state and reset move history
                // create game components needed to restart a save game
                _game = _gameFactory
                            .CreateBoardGame(_gameState.GameMode == GameMode.HumanVsComputer, _gameState);

                _gameController?.Dispose(); // clean up previous event subscription resources if not null
                                            // create the GameController object only if null
                _gameController ??= new GameController(_game, _gameState);
                _gameController.PlaySavedGame();
            }
            else
            {
                ConsoleUI.DisplayErrorMessage($"\nInvalid game type in saved game file: {gameTypeString}.");
            }
        }
        catch (Exception e)
        {
            ConsoleUI.DisplayPausedErrorMessage($"Error loading saved game: {e.Message}");
        }
    }

    // creates a specific abstract pattern game factory via this factory method based on game family type
    private static IGameFactory CreateFactory(GameType gameType)
    {
        return gameType switch
        {
            GameType.NUMTICTACTOE => new NumTicTacToeFactory() as IGameFactory,

            _ => throw new ArgumentException($"No factory available for{gameType}.")
        };

    }
    // retrieves the current game factory instance reference
    public static IGameFactory GetFactory()
    {
        if (_bgfinstance?._gameFactory == null)
        {
            throw new InvalidOperationException("No active game factory, start a game first.");
        }

        return _bgfinstance._gameFactory;
    }
}

