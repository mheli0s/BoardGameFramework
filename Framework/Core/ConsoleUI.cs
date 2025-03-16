using System;
using System.Linq;
using System.Threading; // for Sleep()
using Framework.Enums;
using Framework.Interfaces;
namespace Framework.Core;

/* Handle all console-based user interactions and display outputs */

public class ConsoleUI
{
    // display a loading indicator with a message for the specified delay iterations
    internal static void DisplayLoadingMessage(int iterations, string message)
    {
        Console.WriteLine("");
        char[] spinner = ['/', '-', '\\', '|'];

        for (int i = 0; i < iterations; i++) // iterations x 100ms = number of seconds it will be displayed
        {
            Console.Write($"\r{message}" + spinner[i % spinner.Length]);
            Thread.Sleep(100); // 100ms delay between each iteration
        }
    }

    // wrapper method for displaying initial console visual elements for the framework
    internal static void DisplayInitialFrameworkScreen()
    {
        DisplayFrameworkBanner();
        DisplayBriefFrameWorkDesc();
        Console.WriteLine("");
        DisplayInfoMessage("Welcome! Choose an option:\n");
        DisplayFrameworkMenu();
    }

    // wrapper method for redrawing framework headers after error messages or other temporary screen info
    internal static void DisplayFrameworkMainHeaders()
    {
        DisplayFrameworkBanner();
        DisplayBriefFrameWorkDesc();
        Console.WriteLine("");
    }

    // display a BGF (BoardGame Framework) ASCII Banner
    internal static void DisplayFrameworkBanner()
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine(
        """
         __     ____   ____ _____    __  
        / /____| __ ) / ___|  ___|___\ \ 
        / /_____|  _ \| |  _| |_ |_____\ \
        \ \_____| |_) | |_| |  _||_____/ /
        \_\    |____/ \____|_|       /_/          
        """);
        Console.ResetColor();
    }

    // short framework main screen introduction
    internal static void DisplayBriefFrameWorkDesc()
    {
        Console.ForegroundColor = ConsoleColor.DarkBlue;
        Console.WriteLine("\n    ***BoardGameFramework***\n");
        Console.ResetColor();
        DisplayInfoMessage("An extensible framework for playing 2 player boardgames.");
        DisplayInfoMessage("By Matthew Hansen. v1.0: 2025.");
    }

    // display the main menu
    internal static void DisplayFrameworkMenu()
    {
        Console.WriteLine("1. Numerical TicTacToe");
        Console.WriteLine("2. Help");
        Console.WriteLine("0. Exit\n");
    }

    // display the game menu
    internal static void DisplayGameMenu()
    {
        Console.WriteLine("1. Play new game.");
        Console.WriteLine("2. Load saved game.");
        Console.WriteLine("3. Show game rules.");
        Console.WriteLine("0. Return to main menu.\n");
    }

    // display game player mode after selecting a game
    internal static void DisplayGameModeMenu()
    {
        Console.WriteLine("\nChoose player mode:\n");
        Console.WriteLine("1. Human vs Computer");
        Console.WriteLine("2. Human vs Human\n");
    }

    // display the menu prompt for user input
    internal static void DisplayInputPrompt()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("Enter menu number: ");
        Console.ResetColor();
    }

    // display the prompt for entering moves or in-game commands
    internal static void DisplayMovePrompt(Player currentPlayer)
    {
        DisplayCommandsHeader();
        Console.ForegroundColor = ConsoleColor.Yellow;
        int boardStartRow = Console.CursorTop + 10; // move the board down 2 lines
        Console.SetCursorPosition(0, boardStartRow);
        Console.Write("\nEnter a move - 'm <row col value>' or command. ");
        Console.WriteLine($"Starting pieces: {string
                                    .Join(", ", currentPlayer._remainingPieces.Select(p => p.Value))}");
        Console.Write(">>> ");
        Console.ResetColor();
    }

    // display a message informing which player's turn it is
    internal static void DisplayPlayerTurnPrompt(Player currentPlayer)
    {
        Console.SetCursorPosition(0, 8); // Move to the prompt position
        Console.WriteLine("\n"); // Clear the previous prompt
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n\n{currentPlayer.Name}'s turn.\n");
        Console.WriteLine("\n");
        Console.SetCursorPosition(0, Console.CursorTop + 2); // move cursor down 2 lines from top
        Console.ResetColor();
    }

    // display the info banner of commands available during a game
    internal static void DisplayCommandsHeader()
    {
        Console.SetCursorPosition(0, 0); // place it at the top of the screen
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine();
        Console.WriteLine("| In-game commands: [u]ndo, [r]edo, [s]ave game, [h]elp, [f]inish undo/redo, [q]uit |");
        Console.ResetColor();
        Console.WriteLine("\n");
    }

    // display rules specific to a game via a factory pattern method
    internal static void DisplayGameRules(IGameFactory gameFactory)
    {
        DisplayInfoMessage(gameFactory.CreateGameRulesInfo());  // Calls the factory method for rules text
        Console.WriteLine("\nPress any key to return to game menu.");
        Console.ReadKey(intercept: false); // don't display key pressed in console
    }

    // get the input entered to the console by the user
    internal static string GetUserInput()
    {
        string input = Console.ReadLine()?.Trim() ?? string.Empty;
        return input;

        // strip any leading/trailing whitespace and convert to lowercase if necessary
        //return Console.ReadLine()?.Trim().ToLower() ?? string.Empty;;


    }

    // for all squares on a Size x Size board, get square state and set at corresponding grid position
    internal static void DisplayBoard(IBoard board)
    {
        // clear the console before updating the board state
        Console.Clear();
        Console.WriteLine();
        int boardStartRow = 3;
        Console.SetCursorPosition(0, boardStartRow);
        // the top border
        Console.WriteLine("---+---+---");
        // for each row in a board
        for (int row = 0; row < board.Size; row++)
        {
            // the left side border
            //Console.Write("| ");

            // for each col position in a row
            for (int col = 0; col < board.Size; col++)
            {
                // set each square to the piece value at each row/col position
                var square = board.GetSquare(row, col);
                Console.ForegroundColor = ConsoleColor.Magenta;

                // print piece value at board square or an empty string if not occupied
                Console.Write(square.IsOccupied ? $" {square.Piece?.Value} " : "   ");
                Console.ResetColor();

                // print "|" divider to separate grid squares vertically until reached the last col
                if (col < board.Size)
                {
                    Console.Write("|");
                }
            }

            // move down a line after each row and display a horizontal grid divider until reached last row
            Console.WriteLine();
            if (row < board.Size)
            {
                Console.WriteLine("---+---+---");
            }
        }
    }

    // displays a game-over menu
    internal static void DisplayPlayAgainPrompt()
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\nPress 'r' to return to main menu.");
        // TODO:
        //Console.WriteLine("\nWould you like to [p]lay another round, [e]xit program or [r]eturn" +" to main menu? Enter: p, e, or m.");
        //Console.Write(">>>");
        Console.ResetColor();
    }

    // displays the game won message with the winning player's name
    internal static void DisplayWinGameMessage(string currentPlayerName)
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"\nGame over, {currentPlayerName} wins the game!\n");
        Console.ResetColor();
    }

    // displays the game stalemate message
    internal static void DisplayDrawGameMessage()
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"\nGame over, the game is a draw!\n");
        Console.ResetColor();
    }

    // utility for displaying any error message to the user
    internal static void DisplayErrorMessage(string errorMessage)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{errorMessage}\n");
        Console.ResetColor();
    }

    // utility for displaying any informational message to the user
    internal static void DisplayInfoMessage(string infoMessage)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"{infoMessage}");
        Console.ResetColor();
    }

    // the help-page for the framework describing all features and options available
    internal static void DisplayFrameworkHelp()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.White;
        string helpInfo =
        """
        Welcome to BGF - the extensible framework for playing 2 player console boardgames!

        The framework offers a menu-driven flow. The main menu provides the current list of boardgames
        that can be played, a link to this help-page or the ability to exit the program. 

        Choosing a game displays that game's own menu where one can play a new game,
        load a previously saved game from a file, display that game's specific rules or return to the 
        main menu.

        Selecting a new game provides the choice of game modes:
                            - Human vs Computer - play a game against an AI opponent.
                            - Human vs Human    - play a game against another human player.

        Available features during gameplay (activated by pressing the associated shortcut key):

        - 'u': Undo a move - 
                            Removes consecutive previous moves with each keypress up to the initial blank 
                            board.
        - 'r': Redo a move - 
                            Reapplies moves that were undone until all moves returned to board (all undo 
                            operations are reversed).
        - 's': Save a game -
                            Saves a snapshot of the current state of the game to a file on disk. Includes 
                            the moves on the board, current player turn and other data to be able to load
                            the game to continue playing at a later time.
        - 'h': Help menu   -
                            Displays this help page.
        - 'f': Finish undo/redo - 
                            Indicates to the program that no further undo/redo opoerations are desired 
                            and the gameplay can continue with an automatic move by the computer or the
                            human inputting a new move into the prompt.
        - 'q': Quit a game -
                            Quits the current game without saving and returns to the main menu.
        """;

        Console.WriteLine(helpInfo);
        DisplayInfoMessage("Press any key to return.");
        Console.ReadKey(intercept: true); // don't echo kee pressed to console
        Console.Clear();
    }

    // utility to display a description of the right format for a move if input incorrrectly
    // TODO: not used currently
    internal static void DisplayMoveFormatHelp()
    {
        Console.WriteLine("\nMove input formats for available games:\n");
        DisplayInfoMessage("-Numerical TicTacToe: 'm row col value'. Eg. m 1 2 7");
    }

    // returns a valid main menu choice input by the user
    internal static string GetValidFrameworkMenuOption()
    {
        string input, menuChoice;

        do
        {
            DisplayInputPrompt();
            input = GetUserInput();

            if (!InputValidator.IsValidFrameworkMenuOption(input, out menuChoice))
            {
                DisplayErrorMessage("\nInvalid input. Enter an option between 0 - 2\n");
            }
            // get the input and confirm it was a valid menu number, save it to the menuChoice out variable
            // keep reprompting until valid input entered
        } while (!InputValidator.IsValidFrameworkMenuOption(input, out menuChoice));

        return menuChoice;
    }

    // returns a valid game menu choice input by the user
    internal static int GetValidGameMenuOption()
    {
        string input;
        int menuChoice;

        // get the input and confirm it was a valid menu number, save it to the menuChoice out variable
        // keep reprompting until valid input entered
        while (true)
        {
            Console.Clear();
            DisplayFrameworkMainHeaders();
            DisplayGameMenu();
            DisplayInputPrompt();
            input = GetUserInput();

            if (!InputValidator.IsValidGameMenuOption(input, out menuChoice))
            {
                DisplayErrorMessage("\nInvalid input, enter an option between 0 - 3\n");
                Thread.Sleep(1500); // delay execution to allow time for user to read any error messages
                continue;
            }
            return menuChoice;
        }
    }

    // returns a valid GameMode chosen by the user 
    internal static GameMode? GetValidGameModeOption()
    {
        string input;

        // get the input and confirm it was a valid menu number, save it to the mode out variable
        // keep reprompting until valid input entered
        while (true)
        {
            DisplayGameModeMenu();
            DisplayInputPrompt();
            input = GetUserInput();

            if (int.TryParse(input, out int mode) && mode >= 1 && mode <= 2)
            {
                return mode == 1 ? GameMode.HumanVsComputer : GameMode.HumanVsHuman;
            }

            DisplayErrorMessage("\nInvalid input, enter option 1 or 2.");
            Thread.Sleep(800);
        }
    }

    // utility to show an error message with a pause
    internal static void DisplayPausedErrorMessage(string errorMessage)
    {
        Console.Clear();
        DisplayFrameworkMainHeaders();
        DisplayErrorMessage(errorMessage);
        DisplayInfoMessage("Press any key to continue...");
        Console.ReadKey();
    }
}