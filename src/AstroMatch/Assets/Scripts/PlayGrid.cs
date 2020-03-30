﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayGrid : MonoBehaviour
{
    [Header("Grid Parameters")]
    [SerializeField] private int numberOfColumns = 8;
    [SerializeField] private int numberOfRows = 8;

    [Header("What piece prefabs to fill cells with")]
    [SerializeField] private Piece[] playablePieces;

    [Header("An empty cell prefab")]
    [SerializeField] private Cell playableCell;
    
    private Cell[,] cellArray;

    private UnityEngine.UI.Image playField; // TODO Should this be exposed? Also this visually breaks when Rows and/or columns get changed!    

    // Start is called before the first frame update
    void Start()
    {
        cellArray = new Cell[numberOfColumns, numberOfRows];
        playField = GetComponent<UnityEngine.UI.Image>();
        InitializeGrid();        
    }

    private void InitializeGrid() // TODO This needs to be cleaned up!
    {
        float pieceSizeWidth = playablePieces[0].PieceRectTransform.rect.width;
        float pieceSizeHeight = playablePieces[0].PieceRectTransform.rect.height;

        float xPiecePlacement = -((playField.rectTransform.sizeDelta.x / 2) - (pieceSizeWidth / 2)); 
        float yPiecePlacement = ((playField.rectTransform.sizeDelta.y / 2) - (pieceSizeHeight / 2)); 
        Vector2 piecePlacement = new Vector2(xPiecePlacement, yPiecePlacement); // Where the pieces should start spawning

        for (int currentColumn = 0; currentColumn < numberOfColumns; currentColumn++)
        {
            for (int currentRow = 0; currentRow < numberOfRows; currentRow++)
            {
                cellArray[currentRow, currentColumn] = Instantiate(playableCell, this.transform, false);
                cellArray[currentRow, currentColumn].CellLocation = new Vector2(currentRow, currentColumn);               
                cellArray[currentRow, currentColumn].PieceInCell = Instantiate(playablePieces[Random.Range(0, playablePieces.Length)], cellArray[currentRow, currentColumn].transform, false); // TODO This should be done by the cell's START method, not here!
                bool doesSpawnMatch = CheckForMatch(cellArray[currentRow, currentColumn], Directions.RecursiveDirections);
                while (doesSpawnMatch) // Ensures that the initial piece spawns does not come with a match three (or more) already in place
                {
                    cellArray[currentRow, currentColumn].PieceInCell.gameObject.SetActive(false); // TODO Needs to be sent to a pool
                    cellArray[currentRow, currentColumn].PieceInCell = Instantiate(playablePieces[Random.Range(0, playablePieces.Length)], cellArray[currentRow, currentColumn].transform, false);
                    doesSpawnMatch = CheckForMatch(cellArray[currentRow, currentColumn], Directions.RecursiveDirections);
                }
                cellArray[currentRow, currentColumn].RectTransform.localPosition = piecePlacement;             
                piecePlacement.x += pieceSizeWidth;
            }
            piecePlacement.y -= pieceSizeHeight;
            piecePlacement.x = -((playField.rectTransform.sizeDelta.x / 2) - (pieceSizeWidth / 2));
        }        
    }

    public void SwapPieces(Cell cellOne, Cell cellTwo, bool isSwapBack) 
    {
        Transform cellOneCurrentTransform = cellOne.transform; // Cache transform reference as it will change
        cellOne.PieceInCell.transform.SetParent(cellTwo.transform, false); // Set new parents
        cellTwo.PieceInCell.transform.SetParent(cellOneCurrentTransform, false);        
        cellOne.PieceInCell = cellTwo.PieceInCell;
        cellTwo.PieceInCell = cellOne.InitialPiece;
        if (!isSwapBack)
        {
            if (CheckForMatch(cellOne, Directions.AllDirections) || CheckForMatch(cellTwo, Directions.AllDirections)) // Doesn't work
            {
                Debug.Log("Match!");
            }
            else
            {
                Debug.Log("No Match!");
                Debug.Log(cellOne.Log());
                Debug.Log(cellTwo.Log());
                SwapPieces(cellTwo, cellOne, true); // No Match, swap the cell piece back to whence it came
            }
        }
    }

    private bool CheckForMatch(Cell currentCell, Vector2[] directionsToCheck) // TODO This might be able to be cleaned up a bit
    {
        foreach (Vector2 dir in directionsToCheck) // DOESN'T CHECK FOR WHEN MOVING MIDDLE PIECE, NEEDS TO LOOK BOTH LEFT AND RIGHT, UP AND DOWN
        {
            try
            {
                if (cellArray[(int)currentCell.CellLocation.x + (int)dir.x, (int)currentCell.CellLocation.y + (int)dir.y].PieceInCell.GetType() == currentCell.PieceInCell.GetType()) // TODO this is dirty. Can we clean this up?
                {
                    if (cellArray[(int)currentCell.CellLocation.x + (int)dir.x + (int)dir.x, (int)currentCell.CellLocation.y + (int)dir.y + (int)dir.y].PieceInCell.GetType() == currentCell.PieceInCell.GetType()) // TODO this is dirty. Can we clean this up?
                    {                        
                        return true; // Only gets here if three cells in the same direction have the same piece
                    }                    
                }                
            }
            catch
            {                
                // Out of bounds of the array, or that array cell hasn't been initialized
                // TODO Don't need this so find a way to not do a try catch
            }            
        }
        return false;
    }
}