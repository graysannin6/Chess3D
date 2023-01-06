using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class ChessBoard : MonoBehaviour
{
    [Header("ChessDesign")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private float yOffset = 0.2f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;
    [SerializeField] private float deathSize = 0.3f;
    [SerializeField] private float deathSpacing = 0.3f;
    [SerializeField] private float dragOffset = 1.5f;

    [Header("Prefabs && Materials")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] materials;

    private ChessPiece[,] chessPieces;
    private ChessPiece currentlDragging;
    private List<ChessPiece> deadWhites = new List<ChessPiece>();
    private List<ChessPiece> deadBlacks = new List<ChessPiece>();
    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8;
    private GameObject[,] tiles;
    private Camera currentCamera;
    private Vector2Int currentHover;
    private Vector3 bounds;

    private void Awake()
    {
        GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);
        SpawnAllPieces();
        PositionAllPieces();
    }
    private void Update()
    {
        MouseHandler();
    }



    //Generate the Board
    private void GenerateAllTiles(float tileSize,int tileCountX,int tileCountY)
    {

        yOffset += transform.position.y;
        bounds = new Vector3((tileCountX / 2) * tileSize, 0, (tileCountX / 2) * tileSize) + boardCenter;

        tiles = new GameObject[tileCountX, tileCountY];
        for (int x = 0; x < tileCountX; x++)
        {
            for (int y = 0; y < tileCountY; y++)
            {
                tiles[x, y] = GenerateSingleTile(tileSize, x,y);
            }
        }
    }
    private GameObject GenerateSingleTile(float tileSize, int x, int y)
    {
        GameObject tileObject = new GameObject(string.Format("X:{0},Y:{1}", x, y));
        tileObject.transform.parent = transform; 

        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(x * tileSize, yOffset, y * tileSize) - bounds;
        vertices[1] = new Vector3(x * tileSize, yOffset, (y + 1) * tileSize) - bounds;
        vertices[2] = new Vector3((x+1) * tileSize, yOffset, y * tileSize) - bounds;
        vertices[3] = new Vector3((x+1) * tileSize, yOffset, (y+1) * tileSize) - bounds;

        int[] tris = new int[] { 0, 1, 2, 1, 3, 2 };

        mesh.vertices = vertices;
        mesh.triangles = tris;

        mesh.RecalculateNormals();

        tileObject.layer = LayerMask.NameToLayer("Tile");
        tileObject.AddComponent<BoxCollider>();

        return tileObject;
    }

    private void MouseHandler()
    {
        if (!currentCamera)
        {
            currentCamera = Camera.main;
            return;
        }
        RaycastHit info;
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover")))
        {
            //Get the indexes of tile we hit
            Vector2Int hitPosition = LookuptileIndex(info.transform.gameObject);

            //If we are hovering any tile after not hovering any tile
            if (currentHover == -Vector2Int.one)
            {
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }
            //if we were already hovering a tile, change previous
            if (currentHover != hitPosition)
            {
                tiles[currentHover.x, currentHover.y].layer = LayerMask.NameToLayer("Tile");
                currentHover = hitPosition;
                tiles[currentHover.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }

            // if we prees down the mouse
            if (Input.GetMouseButtonDown(0))
            {
                if (chessPieces[hitPosition.x,hitPosition.y] != null)
                {
                    if (true)
                    {
                        currentlDragging = chessPieces[hitPosition.x, hitPosition.y];
                    }
                    
                }
            }
            // if we release the mouse 
            if (currentlDragging != null && Input.GetMouseButtonUp(0))
            {
                Vector2Int previousPosition = new Vector2Int(currentlDragging.currentX,currentlDragging.currentY);

                bool validMove = MoveTo(currentlDragging, hitPosition.x, hitPosition.y);

                if(!validMove)
                {
                    currentlDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y));
                    currentlDragging = null;
                }
                else
                {
                    currentlDragging = null;

                }
            }
        }
        else
        {
            if (currentHover != -Vector2Int.one)
            {
                tiles[currentHover.x, currentHover.y].layer = LayerMask.NameToLayer("Tile");
                currentHover = -Vector2Int.one;
            }

            if (currentlDragging && Input.GetMouseButtonUp(0))
            {
                currentlDragging.SetPosition(GetTileCenter(currentlDragging.currentX, currentlDragging.currentY));
                currentlDragging = null;
            }
        }

        if (currentlDragging)
        {
            Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * yOffset);
            float distance = 0.0f;
            if (horizontalPlane.Raycast(ray,out distance))
            {
                currentlDragging.SetPosition(ray.GetPoint(distance) +Vector3.up * dragOffset);
            }
        }
    }

    private Vector2Int LookuptileIndex(GameObject hitInfo)
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (tiles[x,y] == hitInfo)
                    return new Vector2Int(x, y);
                
         
        return -Vector2Int.one;
    }

    //Spawn Pieces
    private void SpawnAllPieces()
    {
        chessPieces = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];

        int whiteTeam = 0, blackTeam = 1;

        //whiteTeam
        chessPieces[0, 0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);
        chessPieces[1, 0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[2, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[3, 0] = SpawnSinglePiece(ChessPieceType.King, whiteTeam);
        chessPieces[4, 0] = SpawnSinglePiece(ChessPieceType.Queen, whiteTeam);
        chessPieces[5, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[6, 0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[7, 0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);
        
        for (int i = 0; i < TILE_COUNT_X; i++)
        {
            chessPieces[i, 1] = SpawnSinglePiece(ChessPieceType.Pawn, whiteTeam);
        }

        //black Team
        chessPieces[0, 7] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam);
        chessPieces[1, 7] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
        chessPieces[2, 7] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[3, 7] = SpawnSinglePiece(ChessPieceType.Queen, blackTeam);
        chessPieces[4, 7] = SpawnSinglePiece(ChessPieceType.King, blackTeam);
        chessPieces[5, 7] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[6, 7] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
        chessPieces[7, 7] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam);

        for (int i = 0; i < TILE_COUNT_X; i++)
        {
            chessPieces[i, 6] = SpawnSinglePiece(ChessPieceType.Pawn, blackTeam);
        }
    }

    private ChessPiece SpawnSinglePiece(ChessPieceType type ,int team)
    {
        ChessPiece piece = Instantiate(prefabs[(int)type - 1],transform.position + (Vector3.up * 0.8f),transform.rotation).GetComponent<ChessPiece>();

        piece.team = team;
        piece.type = type;
        piece.GetComponent<MeshRenderer>().material = materials[team];

        return piece;
    }

    //Positioning
    private void PositionAllPieces()
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (chessPieces[x,y] != null)
                {
                    PositionSinglePieces(x, y, true);
                }
            }
        }
    }
    private void PositionSinglePieces(int x, int y, bool force = false)
    {
        chessPieces[x, y].currentX = x;
        chessPieces[x, y].currentY = y;
        chessPieces[x, y].SetPosition(GetTileCenter(x, y),force);
    }

    private Vector3 GetTileCenter(int x, int y)
    {
        return new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3(tileSize/2,0,tileSize/2);    
    }

    private bool MoveTo(ChessPiece piece, int x, int y)
    {
        Vector2Int previousPosition = new Vector2Int(piece.currentX, piece.currentY);

        if (chessPieces[x,y] != null)
        {
            ChessPiece ocp = chessPieces[x, y];
            if (piece.team == ocp.team)
            {
                return false;
            }
            //if its the enemy team
            if (ocp.team == 0)
            {
                deadWhites.Add(ocp);
                ocp.SetScale(Vector3.one *  deathSize);
                ocp.SetPosition(new Vector3(8 * tileSize,yOffset,-1 * tileSize) - bounds + new Vector3(tileSize/2,0,tileSize/2)
                    + (Vector3.forward * deathSpacing) * deadWhites.Count);
            }
            else
            {
                deadBlacks.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize);
                ocp.SetPosition(new Vector3( -0.8f * tileSize, yOffset, 8 * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2)
                   + (Vector3.back * deathSpacing) * deadBlacks.Count);
            }
        }

        chessPieces[x,y] = piece;
        chessPieces[previousPosition.x,previousPosition.y] = null;

        PositionSinglePieces(x, y);

        return true;
    }
}
