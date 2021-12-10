using UnityEngine;
using System.Collections.Generic;

public class Pawn : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        int direction = (team == 0) ? 1 : -1;

        //one in front
        if(board[currentX, currentY + direction] == null)
            r.Add(new Vector2Int(currentX,currentY + direction));

        if(board[currentX, currentY + direction] == null)
        {
            //White Team
            if(team == 0 && currentY == 1 && board[currentX,currentY + (direction*2)] == null)
            {
                r.Add(new Vector2Int(currentX,currentY + (direction*2)));
            }

            //Black team
            if(team == 1 && currentY == 6 && board[currentX,currentY + (direction*2)] == null)
            {
                r.Add(new Vector2Int(currentX,currentY + (direction*2)));
            }
        }
        //Kill Move
        if(currentX != tileCountX -1)//Right Direction
        { 
            if(board[currentX +1, currentY + direction] != null && board[currentX+1, currentY + direction].team != team)
                r.Add(new Vector2Int(currentX + 1,currentY + direction));
        }
        if(currentX != 0)   //Left Direction
        {
            if(board[currentX -1, currentY + direction] != null && board[currentX -1, currentY + direction].team != team)
                r.Add(new Vector2Int(currentX - 1,currentY + direction));
        }
        
        return r;
    }

    public override SpecialMove GetSpecialMove(ref ChessPiece[,] board, ref List<Vector2Int[]> moveList, ref List<Vector2Int> availableMove)
    {
        int direction = (team == 0)? 1: -1;
        if((team == 0 && currentY == 6) || (team == 1 && currentY == 1))
            return SpecialMove.Promotion;

        if(moveList.Count > 0)
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1];
            if(board[lastMove[1].x, lastMove[1].y].type == ChessPieceType.Pawn)     // is last piece a pawn
            {
                if(Mathf.Abs(lastMove[0].y - lastMove[1].y) == 2)      // if the last move was +2 in either direction
                {
                    if(board[lastMove[1].x, lastMove[1].y].team != team)    // if the move was from other team
                    {
                        if(lastMove[1].y == currentY)
                        {
                            if(lastMove[1].x == currentX -1)
                            {
                                availableMove.Add(new Vector2Int(currentX -1, currentY + direction));
                                return SpecialMove.EnPassant;
                            }
                            if(lastMove[1].x == currentX + 1)
                            {
                                availableMove.Add(new Vector2Int(currentX +1, currentY + direction));
                                return SpecialMove.EnPassant;
                            }
                        }
                    }
                }     
            }
        }
        
        return SpecialMove.None;
    }

}
