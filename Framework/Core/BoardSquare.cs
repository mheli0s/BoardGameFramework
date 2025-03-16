using System;
using Framework.Interfaces;
namespace Framework.Core;

/* A generic BoardSquare<> implementation of interface IBoardSquare for TPiece typed squares. */

// primary constructor syntax
public class BoardSquare<TPiece>(int row, int col) : IBoardSquare where TPiece : IPiece
{
    public int Row { get; } = row;
    public int Col { get; } = col;
    public TPiece? Piece { get; protected set; } = default;
    public bool IsOccupied => Piece != null;

    // Implement IBoardSquare.Piece as an explicit interface implementation
    IPiece? IBoardSquare.Piece => Piece;

    // Set a piece on the square
    public void SetPiece(TPiece piece)
    {
        Piece = piece;
    }

    // Implement IBoardSquare.SetPiece as explicit interface implementation
    void IBoardSquare.SetPiece(IPiece piece)
    {
        if (piece is TPiece typedPiece)
        {
            SetPiece(typedPiece);
        }
        else
        {
            throw new ArgumentException($"Cannot set piece of type {piece.GetType()}. Expected {typeof(TPiece)}");
        }
    }

    // Remove a piece from the square
    public TPiece? RemovePiece()
    {
        TPiece? pieceToRemove = Piece;
        Piece = default; // sets to null
        return pieceToRemove;
    }

    // Implement IBoardSquare.RemovePiece as explicit interface implementation
    IPiece? IBoardSquare.RemovePiece()
    {
        return RemovePiece();
    }

    // Reset the square
    public void ResetSquare()
    {
        Piece = default;
    }
}
