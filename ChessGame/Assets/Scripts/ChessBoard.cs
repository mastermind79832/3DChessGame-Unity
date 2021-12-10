using System;
using System.Collections.Generic;
using UnityEngine;

public enum SpecialMove
{
    None =0,
    EnPassant,
    Castling,
    Promotion
}
public class ChessBoard : MonoBehaviour
{
    [Header("Art stuff")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] private float tileSize = 1;
    [SerializeField] private float yOffSet = 0.2f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;
    [SerializeField] private float dragOffSet = 0.5f;

    [Header("Prefab and Mats")]
    [SerializeField] private Material[] TeamMaterial;
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private GameObject victoryScreen;

    // LOGIC
    private ChessPiece[,] chessPieces;
    private ChessPiece currentlyDragging;
    private List<Vector2Int> availableMoves = new List<Vector2Int>();
   
    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8;
    private GameObject[,] tiles;
    private Camera currentCamera;
    private Vector2Int currentHover;
    private Vector3 bounds;
    private bool isWhiteTurn;
    private SpecialMove specialMove;
    private List<Vector2Int[]> moveList = new List<Vector2Int[]>();

    private void Awake()
    {
        isWhiteTurn = true;
        GenerateAllTiles(tileSize,TILE_COUNT_X,TILE_COUNT_Y);   

        SpwanAllPieces();
        PositionAllPiece();
    }

    private void Update()
    {
        if(!currentCamera)
        {
            currentCamera = Camera.main;
            return;
        }

        RaycastHit info;
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
        if(Physics.Raycast(ray,out info, 100,LayerMask.GetMask("Tile","Hover","HighLight")))
        {
            //Get Index of tile that's hit
            Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);

            //If we are hoverin over a tile and not hovered any tile previously
            if(currentHover == -Vector2Int.one)
            {
                currentHover = hitPosition;
                tiles[hitPosition.x,hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }

            //if we are hover over a tile after hover over a tile, change the previous one
            if(currentHover != hitPosition)
            {
                tiles[currentHover.x,currentHover.y].layer = (ContainsValidMove(ref availableMoves,currentHover))? LayerMask.NameToLayer("HighLight"):LayerMask.NameToLayer("Tile");
                currentHover = hitPosition;
                tiles[hitPosition.x,hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }
        
            //if we press down the mouse
            if(Input.GetMouseButtonDown(0))
            {
                if(chessPieces[hitPosition.x,hitPosition.y] != null)
                {
                    // is it our turn
                    if((chessPieces[hitPosition.x,hitPosition.y].team == 0 && isWhiteTurn) || (chessPieces[hitPosition.x,hitPosition.y].team == 1 && !isWhiteTurn) )
                    {
                        currentlyDragging = chessPieces[hitPosition.x,hitPosition.y];

                        // list of available Moves and Highlight Tiles
                        availableMoves = currentlyDragging.GetAvailableMoves( ref chessPieces,TILE_COUNT_X,TILE_COUNT_Y);
                        //Get list of special moves
                        specialMove = currentlyDragging.GetSpecialMove(ref chessPieces,ref moveList, ref availableMoves);

                        PreventCheck(currentlyDragging, ref availableMoves);
                        HighLightTiles();
                    }
                }
            }

            // If we release the mouse button
            if(currentlyDragging!=null && Input.GetMouseButtonUp(0))
            {
                Vector2Int previousPostion = new Vector2Int(currentlyDragging.currentX,currentlyDragging.currentY);

                
                bool validMove = MoveTo(currentlyDragging,hitPosition.x,hitPosition.y);
                if(!validMove)
                    currentlyDragging.SetPosition(GetTileCenter(previousPostion.x,previousPostion.y));
                currentlyDragging = null;
                RemoveHighLightTiles();

            }
        }
        else
        {
            if(currentHover != -Vector2Int.one)
            {
                tiles[currentHover.x,currentHover.y].layer = (ContainsValidMove(ref availableMoves,currentHover))? LayerMask.NameToLayer("HighLight"):LayerMask.NameToLayer("Tile");
                currentHover = -Vector2Int.one;
            }

            if(currentlyDragging && Input.GetMouseButtonUp(0))
            {
                currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX,currentlyDragging.currentY));
                currentlyDragging = null;
                RemoveHighLightTiles();
            }
        }
        //If draggin a piece
        if(currentlyDragging)
        {
            Plane horizontalPlane = new Plane(Vector3.up,Vector3.up * yOffSet);
            float distance = 0f;
            if(  horizontalPlane.Raycast(ray,out distance))
                currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * dragOffSet);
        }
    }

    // Generate Board
    private void GenerateAllTiles(float tileSize, int tileCountX, int tileCountY)
    {
        yOffSet += transform.position.y;
        bounds = new Vector3((tileCountX/2) * tileSize, 0 , (tileCountY/2)* tileSize) + boardCenter;

        tiles = new GameObject[tileCountX, tileCountY];

        for (int x = 0; x < tileCountX; x++)
            for (int y = 0; y < tileCountY; y++)
                tiles[x,y]= GenerateSingleTile(tileSize,x,y);     
    }
    private GameObject GenerateSingleTile(float tileSize, int x, int y)
    {
        GameObject tileObject  = new GameObject(string.Format("X:{0}, Y:{1}", x, y));
        tileObject.transform.parent = transform;

        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

        Vector3[] vertices  = new Vector3[4];
        vertices[0] = new Vector3(x*tileSize, yOffSet , y*tileSize) - bounds;
        vertices[1] = new Vector3(x*tileSize, yOffSet , (y+1)*tileSize) - bounds;
        vertices[2] = new Vector3((x+1)*tileSize, yOffSet , y*tileSize) - bounds;
        vertices[3] = new Vector3((x+1)*tileSize, yOffSet , (y+1)*tileSize) - bounds;

        int[] tris = new int[] { 0, 1, 2, 1, 3, 2};

        mesh.vertices =vertices;
        mesh.triangles = tris;
        mesh.RecalculateNormals();

        tileObject.layer = LayerMask.NameToLayer("Tile");
        tileObject.AddComponent<BoxCollider>();

        return tileObject;
    }

    // Spwaning of Pieces
    private void SpwanAllPieces()
    {
        chessPieces = new ChessPiece[TILE_COUNT_X,TILE_COUNT_Y];
        int whiteTeam = 0;
        int blackTeam = 1;

        //WhiteTeam
        chessPieces[0,0]=SpwanSinglePiece(ChessPieceType.Rook,whiteTeam);
        chessPieces[1,0]=SpwanSinglePiece(ChessPieceType.Knight,whiteTeam);
        chessPieces[2,0]=SpwanSinglePiece(ChessPieceType.Bishop,whiteTeam);
        chessPieces[3,0]=SpwanSinglePiece(ChessPieceType.Queen,whiteTeam);
        chessPieces[4,0]=SpwanSinglePiece(ChessPieceType.King,whiteTeam);
        chessPieces[5,0]=SpwanSinglePiece(ChessPieceType.Bishop,whiteTeam);
        chessPieces[6,0]=SpwanSinglePiece(ChessPieceType.Knight,whiteTeam);
        chessPieces[7,0]=SpwanSinglePiece(ChessPieceType.Rook,whiteTeam);
        for(int i = 0; i < TILE_COUNT_X; i++)
        {
            chessPieces[i,1]=SpwanSinglePiece(ChessPieceType.Pawn,whiteTeam);
        }

        //BlackTeam
        chessPieces[0,7]=SpwanSinglePiece(ChessPieceType.Rook,blackTeam);
        chessPieces[1,7]=SpwanSinglePiece(ChessPieceType.Knight,blackTeam);
        chessPieces[2,7]=SpwanSinglePiece(ChessPieceType.Bishop,blackTeam);
        chessPieces[3,7]=SpwanSinglePiece(ChessPieceType.Queen,blackTeam);
        chessPieces[4,7]=SpwanSinglePiece(ChessPieceType.King,blackTeam);
        chessPieces[5,7]=SpwanSinglePiece(ChessPieceType.Bishop,blackTeam);
        chessPieces[6,7]=SpwanSinglePiece(ChessPieceType.Knight,blackTeam);
        chessPieces[7,7]=SpwanSinglePiece(ChessPieceType.Rook,blackTeam);
        for(int i = 0; i < TILE_COUNT_X; i++)
        {
            chessPieces[i,6]=SpwanSinglePiece(ChessPieceType.Pawn,blackTeam);
        }

    }
    private ChessPiece SpwanSinglePiece(ChessPieceType type, int team)
    {
        GameObject piece = Instantiate(prefabs[(int)type - 1]);
        piece.transform.parent = transform;
        if(team == 1)
            piece.transform.Rotate(Vector3.up * 180, Space.World);
        
        ChessPiece cp = piece.GetComponent<ChessPiece>();

        cp.type = type;
        cp.team = team;
        cp.GetComponent<MeshRenderer>().material = TeamMaterial[team];
        return cp;
    }

    // Positioning
    private void PositionAllPiece()
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if(chessPieces[x,y]!= null)
                    PositionSinglePiece(x,y,true);
    }
    private void PositionSinglePiece(int x, int y, bool force = false)
    {
        chessPieces[x,y].currentX = x;
        chessPieces[x,y].currentY = y;
        chessPieces[x,y].SetPosition( GetTileCenter(x,y),force);
    
    }
    private Vector3 GetTileCenter(int x ,int y)
    {
         return new Vector3( x* ((byte)tileSize), yOffSet, y* tileSize) - bounds + new Vector3(tileSize/2,0,tileSize/2);
    }

    // HighLight TIles
    private void HighLightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
            tiles[availableMoves[i].x,availableMoves[i].y].layer = LayerMask.NameToLayer("HighLight");
    }
    private void RemoveHighLightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
            tiles[availableMoves[i].x,availableMoves[i].y].layer = LayerMask.NameToLayer("Tile");
        
        availableMoves.Clear();
    }

    // CheckMate
    private void CheckMate(int team)
    {
        DisplayVictory(team);
    }
    private void DisplayVictory(int winningTeam)
    {
        victoryScreen.SetActive(true);
        victoryScreen.transform.GetChild(winningTeam).gameObject.SetActive(true);
    }
    public void OnResetButton()
    {
        //UI
        victoryScreen.transform.GetChild(0).gameObject.SetActive(false);
        victoryScreen.transform.GetChild(1).gameObject.SetActive(false);
        victoryScreen.SetActive(false);

        // Fields reset
        currentlyDragging = null;
        availableMoves.Clear();
        moveList.Clear();

        //clean up
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if(chessPieces[x,y] != null)
                    Destroy(chessPieces[x,y].gameObject);

                chessPieces[x,y] = null;
            }            
        }

        SpwanAllPieces();
        PositionAllPiece();
        isWhiteTurn = true;
    }
    public void OnExitButton()
    {
        Application.Quit();
    }
    private bool CheckForCheckMate()
    {
        var lastMove = moveList[moveList.Count - 1];
        int targetTeam = (chessPieces[lastMove[1].x, lastMove[1].y].team == 0)? 1 :0;
        
        List<ChessPiece> attackingPieces = new List<ChessPiece>();
        List<ChessPiece> defendingPieces = new List<ChessPiece>();
        ChessPiece targetKing = null;
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if(chessPieces[x,y] != null)
                {
                    if(chessPieces[x,y].team == targetTeam)
                    {
                        defendingPieces.Add(chessPieces[x,y]);
                        if(chessPieces[x,y].type == ChessPieceType.King)
                            targetKing = chessPieces[x,y];
                    } 
                    else
                    {
                        attackingPieces.Add(chessPieces[x,y]);
                    }
            
                }   
            }

        // Is the king attacking King rn
        List<Vector2Int> currentAvailableMove = new List<Vector2Int>();
        for (int i = 0; i < attackingPieces.Count; i++)
        {
            List<Vector2Int> pieceMoves = attackingPieces[i].GetAvailableMoves(ref chessPieces, TILE_COUNT_X,TILE_COUNT_Y);
                for( int b=0; b < pieceMoves.Count; b++)
                    currentAvailableMove.Add(pieceMoves[b]);
        }
        if(ContainsValidMove(ref currentAvailableMove,new Vector2Int (targetKing.currentX, targetKing.currentY)))
        {
            // King is under attack, can king be saved
            for (int i = 0; i < defendingPieces.Count; i++)
            {
                List<Vector2Int> defendingMoves = defendingPieces[i].GetAvailableMoves(ref chessPieces,TILE_COUNT_X,TILE_COUNT_Y);
                SimulateMoveForSinglePiece(defendingPieces[i], ref defendingMoves, targetKing);

                if(defendingMoves.Count != 0)
                    return false;
            }
            return true; // checkMate Exit
        }

        return false;
    }

    private bool CheckForStaleMate()
    {
        List<Vector2Int> enemyAllMoves = new List<Vector2Int>();
        List<Vector2Int> teamAllMoves = new List<Vector2Int>(); 

        bool isTeamMoveable = false;
        bool isEnemyMoveable = false;

        // checking for OtherTeam StaleMate
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if(chessPieces[x,y] != null)
                {   if(chessPieces[x,y].team != currentlyDragging.team)
                    {
                        // get all moves of black
                        enemyAllMoves = chessPieces[x,y].GetAvailableMoves(ref chessPieces, TILE_COUNT_X,TILE_COUNT_Y);
                        // prevent Check moves
                        PreventCheck(chessPieces[x,y], ref enemyAllMoves);   
                        if(enemyAllMoves.Count > 0)
                            isEnemyMoveable = true;
                    }  
                }

        // checking for currentTeam StaleMate
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if(chessPieces[x,y] != null)
                {   if(chessPieces[x,y].team == currentlyDragging.team)
                    {
                        // get all moves of black
                        teamAllMoves = chessPieces[x,y].GetAvailableMoves(ref chessPieces, TILE_COUNT_X,TILE_COUNT_Y);
                        // prevent Check moves
                        PreventCheck(chessPieces[x,y], ref teamAllMoves);      
                        if(teamAllMoves.Count > 0)
                            isTeamMoveable = true;                   
                    }  
                }  

        if(!isTeamMoveable || !isEnemyMoveable)
            return true; 

        return false;
     
    }
    // Special Moves
    private void ProcessSpecialMove()
    {
        if(specialMove == SpecialMove.EnPassant)
        {
            var newMove = moveList[moveList.Count -1];
            ChessPiece myPawn = chessPieces[newMove[1].x, newMove[1].y];
            var targetPawnPosition = moveList[moveList.Count -2];
            ChessPiece enemyPawn = chessPieces[targetPawnPosition[1].x , targetPawnPosition[1].y];

            if(myPawn.currentX == enemyPawn.currentX)
            {
                if(myPawn.currentY == enemyPawn.currentY - 1 || myPawn.currentY == enemyPawn.currentY + 1)
                {
                    Destroy(enemyPawn.gameObject, 0.3f);
                    chessPieces[enemyPawn.currentX,enemyPawn.currentY]=null;
                }
            }
            
        }

        if(specialMove == SpecialMove.Promotion)
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1];
            ChessPiece targetPawn = chessPieces[lastMove[1].x, lastMove[1].y];

            if( targetPawn.type == ChessPieceType.Pawn)
            {
                if( targetPawn.team == 0 && lastMove[1].y == 7) // White
                {
                    ChessPiece newQueen = SpwanSinglePiece(ChessPieceType.Queen, 0);
                    Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                    chessPieces[lastMove[1].x, lastMove[1].y] = newQueen;
                    PositionSinglePiece(lastMove[1].x,lastMove[1].y, true);
                }
                if( targetPawn.team ==1 && lastMove[1].y == 0)  // black
                {
                    ChessPiece newQueen = SpwanSinglePiece(ChessPieceType.Queen, 1);
                    Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                    chessPieces[lastMove[1].x, lastMove[1].y] = newQueen;
                    PositionSinglePiece(lastMove[1].x,lastMove[1].y, true);
                }
            }
        }

        if( specialMove == SpecialMove.Castling)
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1];

            //LeftRook
            if(lastMove[1].x == 2)
            {
                if(lastMove[1].y == 0) // White Side
                {
                    ChessPiece rook = chessPieces[0,0];
                    chessPieces[3,0]= rook;
                    PositionSinglePiece(3,0);
                    chessPieces[0,0] = null;
                }
                else if ( lastMove[1].y == 7) // Black Side
                {
                    ChessPiece rook = chessPieces[0,7];
                    chessPieces[3,7]= rook;
                    PositionSinglePiece(3,7);
                    chessPieces[0,7] = null;
                }
            }
            else if(lastMove[1].x == 6) // Right Rook
            {
                if(lastMove[1].y == 0) // White Side
                {
                    ChessPiece rook = chessPieces[7,0];
                    chessPieces[5,0]= rook;
                    PositionSinglePiece(5,0);
                    chessPieces[7,0] = null;
                }
                else if ( lastMove[1].y == 7) // Black Side
                {
                    ChessPiece rook = chessPieces[7,7];
                    chessPieces[5,7]= rook;
                    PositionSinglePiece(5,7);
                    chessPieces[7,7] = null;
                }
            }

        }
    }
    private void PreventCheck(ChessPiece dragging,ref List<Vector2Int> pieceMoves)
    {
        ChessPiece targetKing = null;
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if(chessPieces[x,y] != null)
                    if(chessPieces[x,y].type == ChessPieceType.King)
                        if(chessPieces[x,y].team == dragging.team)
                        {
                            targetKing = chessPieces[x,y];
                        }
            }
        // sending ref of available movve to delete the move which keeps king in check mate
        SimulateMoveForSinglePiece(dragging, ref pieceMoves, targetKing);
    }
    private void SimulateMoveForSinglePiece(ChessPiece cp, ref List<Vector2Int> moves, ChessPiece targetKing)
    {
        // Save the current value to reset after function call
        int actualX = cp.currentX;
        int actualY = cp.currentY;
        List<Vector2Int> movesToRemove =  new List<Vector2Int>();
        // going through all the move and simulate themto check if its "CHECK"
        for (int i = 0; i < moves.Count; i++)
        {
            int simX = moves[i].x;
            int simY = moves[i].y;

            Vector2Int kingPositionThisSim = new Vector2Int(targetKing.currentX, targetKing.currentY);
            // is King Move Simulated
            if(cp.type == ChessPieceType.King)
                kingPositionThisSim = new Vector2Int(simX,simY);
            
            // Copy the array and do not reference it
            ChessPiece[,] simulation = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];
            List<ChessPiece> simAttackingPieces = new List<ChessPiece>();
            for (int x = 0; x < TILE_COUNT_X; x++)
            {
                for (int y = 0; y < TILE_COUNT_Y; y++)
                {
                    if(chessPieces[x,y] != null)
                    {
                        simulation[x,y] = chessPieces[x,y];
                        if(simulation[x,y].team != cp.team)
                            simAttackingPieces.Add(simulation[x,y]);
                    }
                }
            }

            // Simulate Those Moves
            simulation[actualX,actualY] = null;
            cp.currentX = simX;
            cp.currentY = simY;
            simulation[simX,simY] = cp;

            // Did one of the piece got taken down during our simulation
            ChessPiece deadpiece = simAttackingPieces.Find(c=> c.currentX == simX && c.currentY == simY);
            if(deadpiece != null)
                simAttackingPieces.Remove(deadpiece);

            //Get all the simulated attacking pieces moved
            List<Vector2Int> simMoves = new List<Vector2Int>();

            for(int a= 0; a < simAttackingPieces.Count; a++)
            {
                List<Vector2Int> pieceMoves = simAttackingPieces[a].GetAvailableMoves(ref simulation, TILE_COUNT_X,TILE_COUNT_Y);
                for( int b=0; b < pieceMoves.Count; b++)
                    simMoves.Add(pieceMoves[b]);
            }

            // Is the king in trouble? if so, Remove the move
            if( ContainsValidMove(ref simMoves, kingPositionThisSim))
            {
                movesToRemove.Add(moves[i]);
            }

            // Restore Values
            cp.currentX = actualX;
            cp.currentY = actualY;
        }

        // remove from the current available move list
        for (int i = 0; i < movesToRemove.Count; i++)
        {
            moves.Remove(movesToRemove[i]);
        }
    }   
   
    // Operations
    private bool ContainsValidMove(ref List<Vector2Int> moves, Vector2Int pos)
    {
        for (int i = 0; i < moves.Count; i++)
        {   
            if(moves[i].x == pos.x && moves[i].y ==  pos.y)
                return true;
        }
        return false;
    }
    private bool MoveTo(ChessPiece cp, int x, int y)
    {
        if(!ContainsValidMove(ref availableMoves, new Vector2Int(x,y)))
        {
            return false;
        }

        Vector2Int previousPosition = new Vector2Int(cp.currentX,cp.currentY);

        //Is there another Piece
        if(chessPieces[x,y] !=null)
        {
            ChessPiece ocp = chessPieces[x,y];
            if(cp.team == ocp.team)
            {
                return false;
            }
            else    //If enemy Team
            {
                if( ocp.type == ChessPieceType.King)
                    CheckMate(cp.team);
                Destroy(ocp.gameObject, 0.3f);
            }
        }

        chessPieces[x,y] = cp;
        chessPieces[previousPosition.x,previousPosition.y] = null;

        PositionSinglePiece(x,y);

        isWhiteTurn = !isWhiteTurn;
        moveList.Add(new Vector2Int[] {previousPosition, new Vector2Int(x,y)});

        ProcessSpecialMove();

        if(CheckForCheckMate())
            CheckMate(cp.team);

        if(CheckForStaleMate())
            CheckMate(2);
        return true;
    }
    private Vector2Int LookupTileIndex(GameObject hitInfo)
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if(tiles[x,y] == hitInfo)
                    return new Vector2Int(x,y);
        
        return -Vector2Int.one; // Invalid 
    }
}