using System;
using System.Threading;
using Framework.Enums;
using Framework.Interfaces;

namespace Framework.Core
{
    /* Manages the high-level flow of the main game loop - handle moves, updating state/history/board, 
       process turns, in-game commands, handle end of game */

    // implements IDisposable to allow event subscriptions cleanup
    public class GameController : IDisposable
    {
        // fields/properties
        private GameState _gameState;
        private readonly IBoardGame _game;
        public IBoardGame IBoardGame => _game; // public accessor of private field
        private bool isUndoRedoActive = false; // flag to pause computer turn during undo/redo operations

        public GameController(IBoardGame game, GameState gameState)
        {
            _game = game;
            _gameState = gameState;

            // TODO: use for Observer implementation
            // Observer event subscribers
            // _gameState.GameStatusChanged += OnGameStatusChanged;
            // _gameState.GameModeChanged += OnGameModeChanged;
            // _gameState.TurnChanged += OnTurnChanged;
        }

        // initialize a new game - set up game state, reset the board and move history
        public void InitializeGame()
        {
            _game.MoveHistory.ClearHistory();
            ConsoleUI.DisplayBoard(_game.Board); //display initial empty board on the console
        }

        // wrapper for launching a new game
        public void PlayNewGame()
        {
            RunGame();
        }

        // wrapper for launching a saved game
        public void PlaySavedGame()
        {
            RunGame();
        }

        // main gameplay loop
        public void RunGame()
        {
            var currentGameStatus = _gameState.GetGameStatus();

            // continue processing player turns until a game-over state is reached
            while (currentGameStatus == GameStatus.IN_PROGRESS)
            {
                ConsoleUI.DisplayPlayerTurnPrompt(_gameState.GetCurrentPlayer());
                ConsoleUI.DisplayMovePrompt(_gameState.GetCurrentPlayer());

                Player currentPlayer = _gameState.GetCurrentPlayer();
                ProcessTurn(currentPlayer);

                // check for game-end conditions
                currentGameStatus = _gameState.GetGameStatus();

                // Exit loop if game is no longer in progress
                if (currentGameStatus != GameStatus.IN_PROGRESS)
                {
                    break;
                }
            }
            // If game is over, process game over
            if (currentGameStatus == GameStatus.WON || currentGameStatus == GameStatus.DRAW)
            {
                ProcessGameOver(currentGameStatus, _gameState.GetCurrentPlayer());
            }
        }

        // get and make a move based on player type, add it to history, update state, handle game-over
        private void ProcessTurn(Player currentPlayer)
        {
            string input = ConsoleUI.GetUserInput();

            if (InputValidator.IsEmptyInput(input))
            {
                ConsoleUI.DisplayErrorMessage("\nInvalid input - cannot be empty.");
                return;
            }

            // process moves and commands input by user
            IMove? move = ProcessGameCommands(input, currentPlayer);
            if (move == null) return; // if null returned then a command was input, thus no move to make
            /* Player logic determines if move is AI-generated (computer) or input via console for a human.
               If a human player is still doing undo/redo operations, skip computer turn until finished. */
            if (isUndoRedoActive && currentPlayer.Type == PlayerType.COMPUTER) return;

            // make the make and return a boolean to see if it was a valid move
            bool successfulMove = _game.MakeMove(move);

            // Check if game is over after move
            if (_gameState.GetGameStatus() == GameStatus.WON || _gameState.GetGameStatus() == GameStatus.DRAW)
            {
                ProcessGameOver(_gameState.GetGameStatus(), currentPlayer);
                return;
            }

            // only update board, state and history if valid
            if (successfulMove)
            {
                _gameState.UpdateStateAfterMove(_game); // update game state after move 
                _game.MoveHistory.AddMoveToHistory(move, _gameState); // add to move history for undo/redo
                ConsoleUI.DisplayBoard(_game.Board); // update board with new move

                // // check if game over and handle it otherwise switch turns to other player  
                // GameStatus gameStatus = _gameState.GetGameStatus();

                // if (gameStatus == GameStatus.WON || gameStatus == GameStatus.DRAW)
                // {
                //     ProcessGameOver(gameStatus, currentPlayer);
                //     return;
                // }

                // else if (gameStatus == GameStatus.IN_PROGRESS)
                // {

                SwitchTurn();

                // if next player is a computer, automatically process its move
                Player nextPlayer = _gameState.GetCurrentPlayer();
                if (nextPlayer.Type == PlayerType.COMPUTER)
                {
                    ProcessComputerTurn(nextPlayer);
                }

                else
                {
                    ConsoleUI.DisplayErrorMessage("\nInvalid move format. Please input: 'm <row col value>'");
                }
            }
        }

        // automatically process the computer's move by generating a move based on the chosen AI move strategy
        private void ProcessComputerTurn(Player computerPlayer)
        {
            // exit immediately if game over to stop the computer's automatic turn.
            if (_gameState.GetGameStatus() != GameStatus.IN_PROGRESS) return;

            ConsoleUI.DisplayLoadingMessage(7, "Computer thinking...");

            IMove move = computerPlayer.GetMove(_game.Board);
            bool successfulMove = _game.MakeMove(move);

            if (successfulMove)
            {
                _gameState.UpdateStateAfterMove(_game);
                _game.MoveHistory.AddMoveToHistory(move, _gameState);
                ConsoleUI.DisplayBoard(_game.Board);

                // Check if game is over after computer's move
                // GameStatus currentStatus = _gameState.GetGameStatus();

                // if (currentStatus == GameStatus.WON || currentStatus == GameStatus.DRAW)
                // {
                //     ProcessGameOver(currentStatus, computerPlayer);
                //     return; // Don't switch turns if game is over
                // }

                SwitchTurn(); //Switch back to the human
            }
            else
            {
                ConsoleUI.DisplayErrorMessage($"\nComputer tried to use piece {move.Value}, but it was not available.");
            }
        }


        // handle the move entered and in-game commands input during gameplay
        private IMove? ProcessGameCommands(string input, Player currentPlayer)
        {
            // switch on the first character input
            switch (input[0])
            {
                case 'm':
                    return ProcessMove(input, currentPlayer);
                case 'u':
                    isUndoRedoActive = true;
                    UndoMove();
                    return null;
                case 'r':
                    isUndoRedoActive = true;
                    RedoMove();
                    return null;
                case 's':
                    SaveGame();
                    return null;
                case 'h':
                    ConsoleUI.DisplayFrameworkHelp();
                    return null;
                case 'q':
                    QuitGame();
                    return null;
                default:
                    if (!char.IsLetter(input[0])) // if input is a number, show a helpful message
                    {
                        ConsoleUI.DisplayErrorMessage($"\nInvalid command: '{input}'. Enter 'm' for a move, 'u' for undo, etc.");
                        return null;
                    }

                    ConsoleUI.DisplayErrorMessage("\nInvalid command, please re-enter.");
                    return null;
            }
        }

        /* Receive a human player's move input, parse into its constituent parts, validate and return 
           the created move object after delegating the piece and move object creations to GetMove() */
        public IMove? ProcessMove(string input, Player currentPlayer)
        {
            // split the move into a parts[] array for parsing delimited by a space character
            var parts = input.Split(' ');

            // check inputs are within correct bounds
            if (!_game.Rules.IsValidMoveFormat(parts, out string? errorMessage))
            {
                ConsoleUI.DisplayErrorMessage(errorMessage!);
                ConsoleUI.DisplayMoveFormatHelp();
                return null;
            }

            // return the input inside a newly create move object via GetMove()
            return currentPlayer.GetMove(parts, currentPlayer);
        }

        // remove move from history via move index, return updated state and refresh the board
        private void UndoMove()
        {
            _gameState = _game.MoveHistory.UndoMove();
            var clonedGameState = _gameState.Clone();
            if (clonedGameState?.Game?.Board != null) _game.Board = clonedGameState.Game.Board;
            Console.Clear();
            ConsoleUI.DisplayBoard(_game.Board);
        }

        // re-apply undone move to history, return updated state and refresh the board
        private void RedoMove()
        {
            _gameState = _game.MoveHistory.RedoMove();
            var clonedGameState = _gameState.Clone();
            if (clonedGameState?.Game?.Board != null) _game.Board = clonedGameState.Game.Board;
            Console.Clear();
            ConsoleUI.DisplayBoard(_game.Board);
        }

        // takes a snapshot of the current game state to save to a file on disk or other location formats
        private void SaveGame()
        {
            _gameState.SaveGame();
            ConsoleUI.DisplayInfoMessage("Game saved successfully.");
        }

        // quit the current game and return to the main menu
        private void QuitGame()
        {
            ConsoleUI.DisplayLoadingMessage(10, "Quitting game, returning to main menu...");
            _gameState.UpdateGameStatus(GameStatus.QUIT);
            Dispose(); // clean up previous event subscription resources
            Console.Clear();
            BoardGameFramework.GetBGFInstance().StartFramework();
        }

        // utility to switch player turns after a valid move that didn't result in game-over conditions
        private void SwitchTurn()
        {
            // use the modulus result of the index (one-indexed) and number of players to alternate 0 and 1
            _gameState.CurrentPlayerIndex = (_gameState.CurrentPlayerIndex + 1) % _game.Players.Count;
        }

        // handle game over conditions - update game status and display a message to the console
        internal void ProcessGameOver(GameStatus gameStatus, Player currentPlayer)
        {
            // release our event resourses first
            Dispose();
            switch (gameStatus)
            {
                // display winner message with name of current player based on matching type and handle null
                case GameStatus.WON:
                    string playerName = (currentPlayer as HumanPlayer)?.Name ?? (currentPlayer as ComputerPlayer)?.Name ?? "Unknown Player";
                    ConsoleUI.DisplayWinGameMessage(playerName);
                    Thread.Sleep(1000);
                    //ConsoleUI.DisplayPlayAgainPrompt();
                    ProcessEndGameOptions();
                    return;
                case GameStatus.DRAW:
                    ConsoleUI.DisplayDrawGameMessage();
                    Thread.Sleep(1000);
                    //ConsoleUI.DisplayPlayAgainPrompt();
                    ProcessEndGameOptions();
                    return;
                default:
                    ConsoleUI.DisplayErrorMessage("Unknown end game status.");
                    break;
            }

            //Console.WriteLine("====================================");

            // ConsoleUI.DisplayPlayAgainPrompt();
            // ProcessEndGameOptions();
        }

        // handle the action taken when a valid game-over option is input by the user
        private void ProcessEndGameOptions()
        {
            // clear any extra input in the buffer
            while (Console.KeyAvailable)
            {
                Console.ReadKey(true);
            }

            ConsoleUI.DisplayInfoMessage("Press any key to return to the main menu...");
            Console.ReadKey(true);
            Console.Clear();
            BoardGameFramework.GetBGFInstance().StartFramework();
        }

        // private void ProcessEndGameOptions()
        // {
        //     // Clear any extra input in the buffer
        //     while (Console.KeyAvailable)
        //     {
        //         Console.ReadKey(true);
        //     }

        //     string input = ConsoleUI.GetUserInput();

        //     if (!InputValidator.IsEmptyInput(input)
        //         && InputValidator.IsValidGameOverMenuOption(input, out char menuChoice))
        //     {
        //         switch (menuChoice)
        //         {
        //             case 'p':
        //                 RestartNewGame(); // play again with reset board and history
        //                 break;
        //             case 'e':
        //                 Environment.Exit(0); // exit program
        //                 break;
        //             case 'r':
        //                 // Use the framework's Singleton pattern instance to return to the framework menu
        //                 Console.Clear();
        //                 Console.WriteLine();
        //                 ConsoleUI.DisplayLoadingMessage(10, "Returning to main menu.");
        //                 BoardGameFramework.GetBGFInstance().StartFramework();
        //                 return;
        //         }
        //     }
        // }

        // Play another round of the current game with reset state, history and board but same players 
        private void RestartNewGame()
        {
            // First, unsubscribe from events to prevent double events
            _gameState.GameStatusChanged -= OnGameStatusChanged;
            _gameState.TurnChanged -= OnTurnChanged;
            _gameState.GameModeChanged -= OnGameModeChanged;

            // Reset everything properly
            _gameState.ResetGameState();
            _game.MoveHistory.ClearHistory();
            _game.Board.ResetAllSquares();

            // Make sure all players' pieces are reset
            foreach (var player in _game.Players)
            {
                player.ResetPieces(); // Make sure this method exists or implement it
            }

            // Re-subscribe to events
            _gameState.GameStatusChanged += OnGameStatusChanged;
            _gameState.TurnChanged += OnTurnChanged;
            _gameState.GameModeChanged += OnGameModeChanged;

            // Clear the console to start fresh
            Console.Clear();

            // Display the fresh board
            ConsoleUI.DisplayBoard(_game.Board);

            // Instead of PlayNewGame, directly use RunGame to avoid extra initialization
            RunGame();

            // TODO: use for Observer pattern implementation:
            // Re-subscribe to events if needed (only if you had unsubscribed)
            // _gameState.GameStatusChanged += OnGameStatusChanged;
            // _gameState.TurnChanged += OnTurnChanged;
            // _gameState.GameModeChanged += OnGameModeChanged;
        }

        // Observer events that publishers invoke when an event is received which sends notifications to all subscribers
        private void OnGameStatusChanged(GameStatus newStatus)
        {
            switch (newStatus)
            {
                case GameStatus.WON:
                    ConsoleUI.DisplayWinGameMessage(_gameState.GetCurrentPlayer().Name);
                    break;
                case GameStatus.DRAW:
                    ConsoleUI.DisplayDrawGameMessage();
                    break;
            }
        }

        private void OnTurnChanged(int turnNumber)
        {
            ConsoleUI.DisplayInfoMessage($"Turn {turnNumber}");
        }

        private void OnGameModeChanged(GameMode mode)
        {
            ConsoleUI.DisplayInfoMessage($"GameMode: {mode}");
        }

        // unsubscribe from event notifications
        public void Dispose()
        {
            _gameState.GameStatusChanged -= OnGameStatusChanged;
            _gameState.TurnChanged -= OnTurnChanged;
            _gameState.GameModeChanged -= OnGameModeChanged;
            // informs the garbage collector no finalizer is needed since it's done already via the Dispose() call
            GC.SuppressFinalize(this);
        }

    }
}
