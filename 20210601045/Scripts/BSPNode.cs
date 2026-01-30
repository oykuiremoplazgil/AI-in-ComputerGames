using UnityEngine;

public class BSPNode
{
    public Vector2Int position; 
    public Vector2Int size;     
    
    public BSPNode child1;      
    public BSPNode child2;      
    
    public RectInt room;         

    public BSPNode(Vector2Int pos, Vector2Int s)
    {
        position = pos;
        size = s;

        room = new RectInt(0, 0, 0, 0); 
    }

    public bool IsLeaf()
    {
        return child1 == null && child2 == null;
    }
}
