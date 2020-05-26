using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid 
{
    public int numberOfColumns;
    public int numberOfRows;

    public SinglePiece[,] PieceArray { get; private set; }
    private SinglePiece[,] cachedPieceArray;

    public Grid(int numberOfColumns, int numberOfRows)
    {
        this.numberOfColumns = numberOfColumns;
        this.numberOfRows = numberOfRows;
        InitializeGrid();        
    }

    private void InitializeGrid()
    {
        PieceArray = new SinglePiece[this.numberOfColumns + 2, this.numberOfRows + 2];
        cachedPieceArray = new SinglePiece[this.numberOfColumns + 2, this.numberOfRows + 2];
        InitializeNullCells();
        InitializeNormalCells();
    }

    private void InitializeNullCells()
    {
        for (int currentColumn = 0; currentColumn == 0; currentColumn++)
        {
            for (int currentRow = 0; currentRow <= numberOfRows + 1; currentRow++)
            {
                PieceArray[currentRow, currentColumn] = new SinglePiece(currentRow, currentColumn, true); // Makes a new piece, makes it of enum type null
            }
        }

        for (int currentColumn = numberOfColumns + 1; currentColumn == numberOfColumns + 1; currentColumn--)
        {
            for (int currentRow = 0; currentRow <= numberOfRows + 1; currentRow++)
            {
                PieceArray[currentRow, currentColumn] = new SinglePiece(currentRow, currentColumn, true);
            }
        }

        for (int currentRow = 0; currentRow == 0; currentRow++)
        {
            for (int currentColumn = 0; currentColumn <= numberOfColumns + 1; currentColumn++)
            {
                PieceArray[currentRow, currentColumn] = new SinglePiece(currentRow, currentColumn, true);
            }
        }

        for (int currentRow = numberOfRows + 1; currentRow == numberOfRows + 1; currentRow++)
        {
            for (int currentColumn = 0; currentColumn <= numberOfColumns + 1; currentColumn++)
            {
                PieceArray[currentRow, currentColumn] = new SinglePiece(currentRow, currentColumn, true);
            }
        }
    }

    private void InitializeNormalCells() // Check to make sure we don't lay down a piece that ends in a match
    {
        for (int currentRow = 1; currentRow <= numberOfRows; currentRow++)
        {
            for (int currentColumn = 1; currentColumn <= numberOfColumns; currentColumn++)
            {
                PieceArray[currentColumn, currentRow] = new SinglePiece(currentColumn, currentRow, false);
                if (PieceArray[currentColumn, currentRow].PieceType != SinglePieceType.None) // Make sure we don't hit the outer ring of null pieces
                {
                    bool doesSpawnMatch = Matching.CheckInitialMatch(PieceArray[currentColumn, currentRow], PieceArray, Directions.RecursiveDirections); // We only check rescurviely (left, up) as the pieces are being laid from left to right, top to bottom, so no piece to the right and down
                    while (doesSpawnMatch)
                    {
                        PieceArray[currentColumn, currentRow].RandomizeType();
                        doesSpawnMatch = Matching.CheckInitialMatch(PieceArray[currentColumn, currentRow], PieceArray, Directions.RecursiveDirections);
                    }
                }
            }
        }
    }

    public void SetPieceToNull(Vector2 pieceLocation)
    {
        this.PieceArray[(int)pieceLocation.x, (int)pieceLocation.y].PieceType = SinglePieceType.None;
    }

    public void SetPieceType(Vector2 pieceLocation, SinglePieceType newType) // For debug purposes
    {
        this.PieceArray[(int)pieceLocation.x, (int)pieceLocation.y].PieceType = newType;
    }

    public void ResetPieceLocation(Vector2 pieceLocation)
    {
        PieceArray[(int)pieceLocation.x, (int)pieceLocation.y].Location = pieceLocation;
    }

    public void CacheArray()
    {
        cachedPieceArray = PieceArray.Clone() as SinglePiece[,];
    }

    public void RevertArray()
    {
        PieceArray = cachedPieceArray.Clone() as SinglePiece[,];
    }
}
