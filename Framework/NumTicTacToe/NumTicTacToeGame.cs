using System;
using System.Collections.Generic;
using System.Text;
using Framework.Core;
using Framework.Interfaces;
using Framework.Enums;
namespace Framework.NumTicTacToe;

/* Concrete implementation of IBoardGame representing a Numerical TicTacToe game */

public class NumTicTacToeGame : IBoardGame
{
    // fields and properties
    private const char DELIM = ','; // delimiter representation for splitting data
    private const char EMPTY_CELL = '-'; // empty cell representation in the file
    private int _currentPlayerIndex;
    private readonly GameState _gameState;

    public IBoard Board { get; set; }
    public IGameRules Rules { get; private set; }
    public List<Player> Players { get; private set; }
    public IPiece? Piece { get; private set; }
    public MoveHistory MoveHistory { get; private set; }

    // constructor
    public NumTicTacToeGame(
        IBoard board,
        List<Player> players,
        IGameRules rules,
        GameState gameState)
    {
        Board = board ?? throw new ArgumentNullException(nameof(board));
        Players = players ?? throw new ArgumentNullException(nameof(players));
        Rules = rules ?? throw new ArgumentNullException(nameof(rules));
        _gameState = gameState;
        _gameState.InitializeNewGameState(this);
        MoveHistory = new(_gameState);
    }

    // Make a move on a board - check for validity, and win/draws 
    public bool MakeMove(IMove move)
    {
        // check if it is a NumTicTacToeMove type and casts it to one if not
        if (move is not NumTicTacToeMove numMove) return false;

        // check if valid - IsValidMove() returns a tuple
        (bool isValid, string? errorMessage) = Rules.IsValidMove(Board, move);
        if (!isValid)
        {
            ConsoleUI.DisplayErrorMessage(errorMessage!);
            return false;
        }

        /* Create piece from move and place on the board at square corresponding 
           to the move's specified grid position, and ensure it's the correct type. */
        var piece = BoardGameFramework.GetFactory().CreatePiece(numMove.Value, numMove.Owner);

        // place the correctly type-checked piece
        Board.PlacePiece(numMove.Row, numMove.Col, piece, numMove.Owner);

        // Remove the piece only if the move is valid
        numMove.Owner.UsePiece((int)piece.Value);

        // check if move resulted in a game winning condition
        if (Rules.IsGameWon())
        {
            _gameState.UpdateGameStatus(GameStatus.WON);
            return true;
        }

        // check if move resulted in a game draw 
        if (Rules.IsStalemate())
        {
            _gameState.UpdateGameStatus(GameStatus.DRAW);
            return true;
        }

        return true;
    }

    /* Serializes a current game to a string representation in the format:
     "board snapshot, player name, state data snapshot". */
    public string Serialize()
    {
        return $"{SerializeBoard()}{DELIM}{SerializePlayer()}{DELIM}{SerializeGameState()}";
    }

    // utility to convert the board to a CSV string for saving
    private string SerializeBoard()
    {
        /* Note: String.Text's StringBuilder class is a C# built-in "Builder" design pattern implementation. */
        StringBuilder stringBuilder = new(); // format the board snapshot
        /* Traverse and convert all board square's piece values to a string in the correct position sequence as determined by the row/col values. Insert a placeholder value if empty. */
        for (int row = 0; row < Board.Size; row++)
        {
            for (int col = 0; col < Board.Size; col++)
            {
                IBoardSquare square = Board.GetSquare(row, col);

                if (square.IsOccupied)
                {
                    // string of values on each board square 
                    stringBuilder.Append($"{square.Piece?.Value ?? EMPTY_CELL}{DELIM}");
                }
                else
                {           // mark vacant squares with a "-" in the file
                    stringBuilder.Append($"{EMPTY_CELL}{DELIM}");
                }
            }
        }
        return stringBuilder.ToString();
    }

    // serializes a Player object by returning the string of its Name
    private string SerializePlayer()
    {
        return Players[_currentPlayerIndex].Name;
    }

    /* serializes a GameSTate object by returning a string concatenation of:
       "turn number, player name, game mode" */
    private string SerializeGameState()
    {
        return $"{_gameState.GetCurrentTurnNumber()}{DELIM}{SerializePlayer()}{DELIM}{_gameState.GameMode}";
    }


    // converts CSV-serialized game state components to GameState objects
    public void Deserialize(string serializedSnapshotData)
    {
        var snapshotParts = serializedSnapshotData.Split(DELIM);
        DeserializeBoard(snapshotParts[0]);
        DeserializePlayer(snapshotParts[1]);
        DeserializeGameState(snapshotParts[2]);
    }

    // deserializes a string representation of the current game state back to their respective state values
    private void DeserializeGameState(string serializedGameStateData)
    {
        var stateParts = serializedGameStateData.Split(DELIM);

        // make sure we have all required parts of file to parse. note: player parsing done separately 
        if (stateParts.Length >= 3)
        {
            if (int.TryParse(stateParts[0], out int turnNumber))
            {
                _gameState.TurnNumber = turnNumber;
            }

            if (Enum.TryParse(stateParts[2], out GameMode savedMode))
            {
                _gameState.GameMode = savedMode; // set GameState property to the saved mode
            }

            _gameState.UpdateGameStatus(GameStatus.IN_PROGRESS); // we're restarting a game so update the status
        }
        else
        {
            ConsoleUI.DisplayInfoMessage("All required state data not available, setting values to defaults.");
            _gameState.TurnNumber = 0;
            _gameState.GameMode = GameMode.HumanVsComputer;
            _gameState.UpdateGameStatus(GameStatus.IN_PROGRESS);
        }
    }

    // assign the internal current player index to the deserialized player name
    private void DeserializePlayer(string serializedPlayer)
    {
        // load parsed current player by name from players collection
        string currentPlayerName = serializedPlayer;

        // set the player index to the index matching the parsed current player name or 0 if not found
        int playerIndex = Players.FindIndex(p => p.Name == currentPlayerName);

        if (playerIndex == -1) playerIndex = 0; // default fallback if player not found

        _currentPlayerIndex = playerIndex;
    }

    // convert a CSV-serialized boardsnapshot to repopulate the pieces on a board
    private void DeserializeBoard(string serializedBoardSnapshot)
    {
        Board.ResetAllSquares(); // initialize squares to empty

        // parse piece values into the array via CSV delimiter and filter out non-occupied empty 
        // TODO: handle non-CSV formatted files?
        string[] pieceValues = serializedBoardSnapshot.Split(DELIM, StringSplitOptions.RemoveEmptyEntries);
        int pieceIndex = 0; // track position in the pieceValues while operating on each serialized piece value

        /* Loop through each square on the board, and convert serialized cell values to GameState piece objects 
         * and populate them on the reset board in their corresponding positions to recreate the saved game's
         * board state ready for continuing the gameplay. 
         */
        for (int row = 0; row < Board.Size; row++)
        {
            for (int col = 0; col < Board.Size; col++)
            {
                // get each square
                var square = Board.GetSquare(row, col);

                // skip over empty squares
                if (pieceValues[pieceIndex] == EMPTY_CELL.ToString())
                {
                    pieceIndex++;
                    continue;
                }

                try
                {
                    int value = int.Parse(pieceValues[pieceIndex]); // parse piece value

                    // find which player the piece belongs to
                    var pieceOwner = value % 2 == 1 ? Players[0] : Players[1];
                    // encapsulate each value and owner into a Piece object and place on square
                    IPiece piece = BoardGameFramework.GetFactory().CreatePiece(value, pieceOwner);
                    square.SetPiece(piece);
                }
                catch (Exception e)
                {
                    ConsoleUI
                    .DisplayErrorMessage($"\nInvalid piece value format - {pieceValues[pieceIndex]}: {e.Message}");
                }

                pieceIndex++;
            }
        }
    }

    // utility to return the current game status
    public GameStatus GetGameStatus()
    {
        return _gameState.GameStatus;
    }
}
