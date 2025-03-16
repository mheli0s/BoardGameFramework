using System;
using System.Linq;
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
                ProcessTurn(ref currentPlayer);

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
        private void ProcessTurn(ref Player currentPlayer)
        {
            currentPlayer = _game.Players[_gameState.CurrentPlayerIndex];

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

            if (currentPlayer is HumanPlayer)
            {
                // skip - move has already been processed by ProcessGameCommands
            }
            else if (currentPlayer is ComputerPlayer computerPlayer)
            {
                // if the player is a computer player, generate a move
                move = computerPlayer.GetMove(_game.Board);
            }
            else
            {
                throw new InvalidOperationException("Player type not supported");
            }

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
                    ConsoleUI.DisplayInfoMessage("\nPress 'u' until done then press 'f' to continue playing.\n");
                    isUndoRedoActive = true;
                    UndoMove();
                    return null;
                case 'r':
                    ConsoleUI.DisplayInfoMessage("\nPress 'r' until done then press 'f' to continue playing.\n");
                    isUndoRedoActive = true;
                    RedoMove();
                    return null;
                case 's':
                    SaveGame();
                    return null;
                case 'h':
                    ConsoleUI.DisplayFrameworkHelp();
                    Console.Clear();
                    ConsoleUI.DisplayBoard(_game.Board);
                    return null;
                case 'f':
                    EndUndoRedoMode();
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
            // get the move that needs to be undone
            var moveToUndo = _game.MoveHistory.GetCurrentMove();

            // Only update the specific square affected by this move
            if (moveToUndo != null)
            {
                // get the square where the piece was placed
                var square = _game.Board.GetSquare(moveToUndo.Row, moveToUndo.Col);

                // remove the piece from this square only
                var removedPiece = square.RemovePiece();

                // return the piece to the player's available pieces if needed
                if (removedPiece != null)
                {
                    var owner = removedPiece.Owner;
                    _game.Players.FirstOrDefault(p => p == owner)?._remainingPieces.Add(removedPiece);
                }

                // get updated state from history (for turn management)
                GameState previousState = _game.MoveHistory.Undo();

                // only update the game state reference and player turn
                _gameState = previousState;
            }
            else
            {
                ConsoleUI.DisplayErrorMessage("\nNo further moves in history to undo.");
            }

            // If we've undone all moves
            if (_game.MoveHistory.GetCurrentMoveIndex() < 0)
            {
                EndUndoRedoMode();
            }

            // clear the console and redisplay the board to show the changes
            Console.Clear();
            ConsoleUI.DisplayBoard(_game.Board);
        }

        // re-apply undone move to history, return updated state and refresh the board
        private void RedoMove()
        {
            // exit if there are no moves to redo
            if (_game.MoveHistory.GetCurrentMoveIndex() >= _game.MoveHistory.GetMoveCount() - 1)
            {
                ConsoleUI.DisplayErrorMessage("\nNo more moves to redo.");
                return;
            }

            // get the move that needs to be redone from history
            GameState nextState = _game.MoveHistory.Redo();

            // only proceed if we have a valid state to redo to
            if (nextState != null)
            {
                // after successfully redoing the move and updating the state
                _gameState = nextState;

                // clear and redisplay the board to show the changes
                Console.Clear();
                ConsoleUI.DisplayBoard(_game.Board);


                // get the move that was redone (after advancing the move index)
                var moveToRedo = _game.MoveHistory.GetCurrentMove();

                if (moveToRedo != null)
                {
                    // get the square where the piece should be placed
                    var square = _game.Board.GetSquare(moveToRedo.Row, moveToRedo.Col);

                    // before placing a piece, make sure the square is empty
                    if (!square.IsOccupied)
                    {
                        // get the player who made this move
                        var player = _game.Players.FirstOrDefault(p => p == moveToRedo.Owner);

                        if (player != null)
                        {
                            // find the piece with the matching value in the player's remaining pieces
                            var pieceToPlace = player._remainingPieces
                                .FirstOrDefault(p => p.Value == moveToRedo.Value);

                            if (pieceToPlace != null)
                            {
                                // remove the piece from player's available pieces
                                player._remainingPieces.Remove(pieceToPlace);

                                // place the piece on the board
                                square.SetPiece(pieceToPlace);
                            }
                        }
                    }

                    // update the game state reference
                    _gameState = nextState;

                    // update the current player
                    _gameState.CurrentPlayerIndex = _game.MoveHistory.GetCurrentMoveIndex() % _game.Players.Count;

                    // clear and redisplay the board to show the changes
                    Console.Clear();
                    ConsoleUI.DisplayBoard(_game.Board);
                    ConsoleUI.DisplayPlayerTurnPrompt(_game.Players[_gameState.CurrentPlayerIndex]);

                    // call ProcessTurn to update the current player
                    //ProcessTurn(ref _game.Players[_gameState.CurrentPlayerIndex]);

                }
            }

            if (_game.MoveHistory.GetCurrentMoveIndex() >= _game.MoveHistory.GetMoveCount() - 1)
            {
                isUndoRedoActive = false; // Reset the flag when done with redo operations
            }

            // clear the console and redisplay the board to show the changes
            Console.Clear();
            ConsoleUI.DisplayBoard(_game.Board);

            // check if we're done with redos
            if (_game.MoveHistory.GetCurrentMoveIndex() >= _game.MoveHistory.GetMoveCount() - 1)
            {
                EndUndoRedoMode();
            }
        }

        // utility for undo and redo methods
        private void EndUndoRedoMode()
        {
            isUndoRedoActive = false;

            _gameState.SynchronizeReferences(); // refresh references to keep all state objects in sync

            // ensure player states are properly synchronized with the game state
            if (_gameState != null && _game != null)
            {
                Console.Clear();
                ConsoleUI.DisplayBoard(_game.Board);
                Player currentPlayer = _gameState.GetCurrentPlayer();
                ConsoleUI.DisplayPlayerTurnPrompt(currentPlayer);

                // If the active player is a computer, auto-process its move
                if (currentPlayer.Type == PlayerType.COMPUTER)
                {
                    ProcessComputerTurn(currentPlayer);
                }
            }
        }


        // takes a snapshot of the current game state to save to a file on disk or other location formats
        private void SaveGame()
        {
            _gameState.Save();
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
