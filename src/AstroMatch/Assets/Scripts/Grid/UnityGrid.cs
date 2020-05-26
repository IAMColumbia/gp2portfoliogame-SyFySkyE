using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnityGrid : MonoBehaviour
{
    [Header("Grid Parameters")]
    [SerializeField] private int numberOfColumns = 8;
    [SerializeField] private int numberOfRows = 8;

    [Header("Unity Piece Prefab")]
    [SerializeField] private UnityPiece unityPiece;

    [Header("How long before a match visually occurs")]
    [SerializeField] private float secondsBeforeCheckForMatch = 0.5f;
    [SerializeField] private float matchCheckTimeDecrement = 0.05f;
    public int UserID
    {
        get
        {
            return this.userID;
        }
    }
    private int userID;

    private Sprite waterImage;
    private Sprite iceImage;
    private Sprite redImage;
    private Sprite purpleImage;
    private Sprite sandImage;
    private Sprite emptyImage;

    public Grid ConceptualGrid { get; private set; } // Conceptual Grid, pure logic 
    public UnityPiece[,] UnityPieces { get; private set; } // Visual Concrete GO Array 
    private UnityEngine.UI.Image playField; // TODO Should this be exposed? Also this visually breaks when Rows and/or columns get changed!    

    private SinglePiece pieceSelected;
    public event Action<int, int> OnCellsMatched;

    public void TestStart() // For Test Runner purposes
    {
        ConceptualGrid = new Grid(numberOfColumns, numberOfRows);
        UnityPieces = new UnityPiece[numberOfColumns + 2, numberOfRows + 2]; // We make an outer ring of cells with piece type NONE to avoid out of indexes when searhcing for matches
    }

    // Start is called before the first frame update
    void Awake()
    {
        this.userID = PlayerID.PlayerUserID++;
        LoadResources();
        ConceptualGrid = new Grid(numberOfColumns, numberOfRows);
        UnityPieces = new UnityPiece[numberOfColumns + 2, numberOfRows + 2]; // We make an outer ring of cells with piece type NONE to avoid out of indexes when searhcing for matches
        playField = GetComponent<Image>();
        DrawCells();
        foreach (UnityPiece uPiece in UnityPieces) 
        {
            uPiece.OnPieceSelect += UnityPiece_OnPieceSelect;
        }
    }

    public void Reset()
    {
        Awake();
    }

    private void LoadResources()
    {
        iceImage = Resources.Load<Sprite>("Art/Sprites/Pieces/Ice");
        purpleImage = Resources.Load<Sprite>("Art/Sprites/Pieces/Purple");
        waterImage = Resources.Load<Sprite>("Art/Sprites/Pieces/Water");
        redImage = Resources.Load<Sprite>("Art/Sprites/Pieces/Red");
        sandImage = Resources.Load<Sprite>("Art/Sprites/Pieces/Sand");
        emptyImage = Resources.Load<Sprite>("Art/Sprites/Pieces/empty");
    }

    private void UnityPiece_OnPieceSelect(Vector2 location)
    {
        if (ConceptualGrid.PieceArray[(int)location.x, (int)location.y].PieceType != SinglePieceType.None) // We're clicking a cell that's not a null piece
        {
            SelectPiece(location);
        }
    }

    private void SelectPiece(Vector2 location)
    {
        if (pieceSelected == null)
        {
            UnityPieces[(int)location.x, (int)location.y].UISelectPiece();
            pieceSelected = ConceptualGrid.PieceArray[(int)location.x, (int)location.y];
        }
        else if (pieceSelected.Location == location) // If we click the same space
        {
            pieceSelected = null;
            UnityPieces[(int)location.x, (int)location.y].DeselectPiece();
        }
        else
        {
            AttemptToSwap(location);
        }        
    }

    private void AttemptToSwap(Vector2 newLocation)
    {
        UnityPieces[(int)pieceSelected.Location.x, (int)pieceSelected.Location.y].DeselectPiece();
        UnityPieces[(int)newLocation.x, (int)newLocation.y].DeselectPiece();
        SoundPlayer.Instance.PlayOneShot(SoundClips.Move);

        foreach (Vector2 dir in Directions.AllDirections)
        {
            if ((this.pieceSelected.Location) + dir == ConceptualGrid.PieceArray[(int)newLocation.x, (int)newLocation.y].Location)
            {
                SwapPieces(pieceSelected.Location, newLocation);
                pieceSelected = null;
                return;
            }
        }
        pieceSelected = null;
    }

    private void SwapPieces(Vector2 pieceOneLoc, Vector2 pieceTwoLoc)
    {        
        UpdateConceptualGrid(pieceOneLoc, pieceTwoLoc); // Update conceptual array and check for match
        if (Matching.CheckForMatch(ConceptualGrid.PieceArray[(int)pieceOneLoc.x, (int)pieceOneLoc.y], ConceptualGrid.PieceArray, Directions.AllDirections) ||
            Matching.CheckForMatch(ConceptualGrid.PieceArray[(int)pieceTwoLoc.x, (int)pieceTwoLoc.y], ConceptualGrid.PieceArray, Directions.AllDirections))
        {            
            UpdateGameObjectGrid(pieceOneLoc, pieceTwoLoc); // If there is a match, update the GO array as well.      
            StartCoroutine(CheckForMatches(pieceOneLoc, pieceTwoLoc));
        }
        else
        {
            UpdateConceptualGrid(pieceTwoLoc, pieceOneLoc); // If there isn't a match, switch it back to where it was. No point in updating GO array.
        }
    }

    public void RevertToPreviousGridState()
    {
        ConceptualGrid.RevertArray();
        RedrawCells();
    }

    public void DecrementMatchTime()
    {
        if (secondsBeforeCheckForMatch <= 0)
            return;
        secondsBeforeCheckForMatch -= matchCheckTimeDecrement;
    }

    private IEnumerator CheckForMatches(Vector2 pieceOneLoc, Vector2 pieceTwoLoc)
    {
        yield return new WaitForSeconds(secondsBeforeCheckForMatch);
        if (Matching.CheckForMatch(ConceptualGrid.PieceArray[(int)pieceOneLoc.x, (int)pieceOneLoc.y], ConceptualGrid.PieceArray, Directions.AllDirections))
        {
            Match(pieceOneLoc);
        }
        if (Matching.CheckForMatch(ConceptualGrid.PieceArray[(int)pieceTwoLoc.x, (int)pieceTwoLoc.y], ConceptualGrid.PieceArray, Directions.AllDirections))
        {
            Match(pieceTwoLoc);
        }
    }

    private void Match(Vector2 pieceLoc)
    {
        SoundPlayer.Instance.PlayOneShot(SoundClips.Match);
        List<SinglePiece> matchingPieces = Matching.GetConnectedPieces(ConceptualGrid.PieceArray[(int)pieceLoc.x, (int)pieceLoc.y], ConceptualGrid.PieceArray, Directions.AllDirections, true);
        
        foreach (SinglePiece piece in matchingPieces)
        {
            ConceptualGrid.SetPieceToNull(piece.Location);
        }
        foreach (SinglePiece piece in matchingPieces)
        {
            FillCell(piece.Location);
        }
        OnCellsMatched(matchingPieces.Count, this.userID);
        StartCoroutine(CheckForNewMatches());
    }

    private void UpdateGameObjectGrid(Vector2 pieceOneLoc, Vector2 pieceTwoLoc)
    {
        Vector2 pieceOneCachedLoc = UnityPieces[(int)pieceOneLoc.x, (int)pieceOneLoc.y].pieceLocation;
        UnityPieces[(int)pieceOneLoc.x, (int)pieceOneLoc.y].InitializeLocation(UnityPieces[(int)pieceTwoLoc.x, (int)pieceTwoLoc.y].pieceLocation);
        UnityPieces[(int)pieceTwoLoc.x, (int)pieceTwoLoc.y].InitializeLocation(pieceOneCachedLoc);

        Vector3 pieceOneRectLoc = UnityPieces[(int)pieceOneLoc.x, (int)pieceOneLoc.y].UnityPieceRectTransform.localPosition;
        UnityPieces[(int)pieceOneLoc.x, (int)pieceOneLoc.y].SetTransform(UnityPieces[(int)pieceTwoLoc.x, (int)pieceTwoLoc.y].UnityPieceRectTransform.localPosition);
        UnityPieces[(int)pieceTwoLoc.x, (int)pieceTwoLoc.y].SetTransform(pieceOneRectLoc);

        UnityPiece unityPieceOne = UnityPieces[(int)pieceOneLoc.x, (int)pieceOneLoc.y];
        UnityPieces[(int)pieceOneLoc.x, (int)pieceOneLoc.y] = UnityPieces[(int)pieceTwoLoc.x, (int)pieceTwoLoc.y];
        UnityPieces[(int)pieceTwoLoc.x, (int)pieceTwoLoc.y] = unityPieceOne;
    }

    private void UpdateConceptualGrid(Vector2 pieceOneLoc, Vector2 pieceTwoLoc)
    {
        ConceptualGrid.CacheArray();
        SinglePiece pieceOne = ConceptualGrid.PieceArray[(int)pieceOneLoc.x, (int)pieceOneLoc.y];
        ConceptualGrid.PieceArray[(int)pieceOneLoc.x, (int)pieceOneLoc.y] = ConceptualGrid.PieceArray[(int)pieceTwoLoc.x, (int)pieceTwoLoc.y];
        ConceptualGrid.PieceArray[(int)pieceTwoLoc.x, (int)pieceTwoLoc.y] = pieceOne;
        ConceptualGrid.ResetPieceLocation(pieceOneLoc);
        ConceptualGrid.ResetPieceLocation(pieceTwoLoc);
    }

    private void FillCell(Vector2 pieceLoc)
    {
        const int updir = -1; // Grid's top left is 0, 0
                
        while (ConceptualGrid.PieceArray[(int)pieceLoc.x, (int)pieceLoc.y].PieceType == SinglePieceType.None)
        {
            if (pieceLoc.x > 1) // Did we reach the top of the grid? Notice we're using the x and not the y
            {
                if (ConceptualGrid.PieceArray[(int)pieceLoc.x + updir, (int)pieceLoc.y].PieceType == SinglePieceType.None)
                {
                    FillCell(ConceptualGrid.PieceArray[(int)pieceLoc.x + updir, (int)pieceLoc.y].Location);
                }
                else
                {
                    ConceptualGrid.SetPieceType(pieceLoc, ConceptualGrid.PieceArray[(int)pieceLoc.x + updir, (int)pieceLoc.y].PieceType); // Copy piece type from above cell
                    UnityPieces[(int)pieceLoc.x, (int)pieceLoc.y].SetImage(GetSprite(ConceptualGrid.PieceArray[(int)pieceLoc.x, (int)pieceLoc.y])); // Update sprite                    
                    ConceptualGrid.SetPieceToNull(ConceptualGrid.PieceArray[(int)pieceLoc.x + updir, (int)pieceLoc.y].Location); // Set above piecetype to none
                    FillCell(ConceptualGrid.PieceArray[(int)pieceLoc.x + updir, (int)pieceLoc.y].Location); // Then we gotta fill it again
                }
            }
            else
            {
                ConceptualGrid.PieceArray[(int)pieceLoc.x, (int)pieceLoc.y].RandomizeType();
                UnityPieces[(int)pieceLoc.x, (int)pieceLoc.y].SetImage(GetSprite(ConceptualGrid.PieceArray[(int)pieceLoc.x, (int)pieceLoc.y]));                
            }
        }
    }

    private void DrawCells()
    {
        RectTransform unityPieceRect = unityPiece.GetComponent<RectTransform>();
        float pieceSizeWidth = unityPieceRect.rect.width;
        float pieceSizeHeight = unityPieceRect.rect.height;

        float xPiecePlacement = -(playField.rectTransform.rect.width / 2) - (pieceSizeWidth / 2);
        float yPiecePlacement = (playField.rectTransform.rect.height / 2) + (pieceSizeHeight / 2);
        Vector2 piecePlacement = new Vector2(xPiecePlacement, yPiecePlacement); // Where the pieces should start spawning

        foreach (SinglePiece currentPiece in ConceptualGrid.PieceArray)
        {
            UnityPiece newPiece = Instantiate(unityPiece, this.transform, false);
            newPiece.InitializeLocation(currentPiece.Location);
            newPiece.SetImage(GetSprite(currentPiece));
            newPiece.SetTransform(piecePlacement);
            piecePlacement.x += pieceSizeWidth;
            if (piecePlacement.x > playField.rectTransform.rect.width / 2 + (pieceSizeWidth / 2))
            {
                piecePlacement.y -= pieceSizeHeight;
                piecePlacement.x = -(playField.rectTransform.rect.width / 2) - (pieceSizeWidth / 2);
            }
            UnityPieces[(int)currentPiece.Location.x, (int)currentPiece.Location.y] = newPiece;
        }
    }

    public void RedrawCells()
    {
        foreach(SinglePiece cPiece in ConceptualGrid.PieceArray)
        {
            UnityPieces[(int)cPiece.Location.x, (int)cPiece.Location.y].SetImage(GetSprite(cPiece));
        }
    }

    private IEnumerator CheckForNewMatches()
    {
        yield return new WaitForSeconds(secondsBeforeCheckForMatch); // TODO Magic number and a coroutine!
        foreach (SinglePiece piece in ConceptualGrid.PieceArray)
        {
            if (piece.PieceType != SinglePieceType.None)
            {
                if (Matching.CheckInitialMatch(piece, ConceptualGrid.PieceArray, Directions.AllDirections))
                {
                    Match(piece.Location);
                }
            }
        }
#if UNITY_EDITOR
        IsConceptualAndUnityGridEqual();
#endif
    }

    private Sprite GetSprite(SinglePiece piece)
    {
        switch (piece.PieceType)
        {
            case SinglePieceType.Ice:
                return iceImage;
            case SinglePieceType.Purple:
                return purpleImage;
            case SinglePieceType.Red:
                return redImage;
            case SinglePieceType.Sand:
                return sandImage;
            case SinglePieceType.Water:
                return waterImage;
            default:
                return emptyImage;
        }
    }
#if UNITY_EDITOR
    private void Update()
    {
        DebugPieceType();
        if (Input.GetKeyDown(KeyCode.N))
        {
            //RevertToPreviousGridState();
        }
    }

    private void DebugPieceType() // Press corresponding key to change type. For testing edge cases and specific scenarios
    {
        if (pieceSelected != null && Input.anyKeyDown)
        {
            if (Input.GetKeyDown(KeyCode.I))
            {
                pieceSelected.PieceType = SinglePieceType.Ice;               
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                pieceSelected.PieceType = SinglePieceType.Red;
            }
            else if (Input.GetKeyDown(KeyCode.W))
            {
                pieceSelected.PieceType = SinglePieceType.Water;
            }
            else if (Input.GetKeyDown(KeyCode.P))
            {
                pieceSelected.PieceType = SinglePieceType.Purple;
            }
            else if (Input.GetKeyDown(KeyCode.S))
            {
                pieceSelected.PieceType = SinglePieceType.Sand;
            }            
            ConceptualGrid.SetPieceType(pieceSelected.Location, pieceSelected.PieceType);
            UnityPieces[(int)pieceSelected.Location.x, (int)pieceSelected.Location.y].SetImage(GetSprite(pieceSelected));
        }
    }

    private void IsConceptualAndUnityGridEqual()
    {
        foreach(UnityPiece uPiece in UnityPieces)
        {
            if (ConceptualGrid.PieceArray[(int)uPiece.pieceLocation.x, (int)uPiece.pieceLocation.y].PieceType != SinglePieceType.None)
            {
                if (UnityPieces[(int)uPiece.pieceLocation.x, (int)uPiece.pieceLocation.y].SpriteImage.sprite.name != ConceptualGrid.PieceArray[(int)uPiece.pieceLocation.x, (int)uPiece.pieceLocation.y].PieceType.ToString())
                {
                    Debug.LogError("Conceptual and Unity grid are misaligned!!");
                }
            }            
        }
    }
#endif
}
