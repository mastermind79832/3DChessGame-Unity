using UnityEngine;
using System.Collections.Generic;

public enum ChessPieceType
{
    None = 0,
    Pawn = 1,
    Rook = 2,
    Knight = 3,
    Bishop = 4,
    Queen = 5,
    King = 6
}

public class ChessPiece : MonoBehaviour
{
    public int team;    //0 - white Team ; 1 - Black team
    public int currentX;
    public int currentY;
    public ChessPieceType type;

    private Vector3 desiredPosition;        // used to move the positons in a smooth manner (lerp)
    private Vector3 desiredScale;           // used when the pieces die

    private void Start()
    {
        desiredScale = transform.localScale;
    }
    
    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 10);
        transform.localScale = Vector3.Lerp(transform.localScale, desiredScale, Time.deltaTime * 10);
    }

    public virtual void SetPosition(Vector3 position, bool force = false)
    {
        desiredPosition = position;
        if(force)
        {
            transform.position = desiredPosition;
        }

    }

    public virtual List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int TileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        r.Add(new Vector2Int(3,3));
        r.Add(new Vector2Int(3,4));
        r.Add(new Vector2Int(4,3));
        r.Add(new Vector2Int(4,4));
        
        return r;
    }

    public virtual SpecialMove GetSpecialMove(ref ChessPiece[,] board, ref List<Vector2Int[]> moveList, ref List<Vector2Int> availableMove)
    {
        return SpecialMove.None;
    }

    public virtual void SetScale( Vector3 scale , bool force = false)
    {
        desiredScale = scale;
        if(force)
        {
            transform.localScale = desiredScale;
        }
    }
}
