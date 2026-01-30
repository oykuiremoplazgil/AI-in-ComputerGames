using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;

public class AStarPathfinding : MonoBehaviour
{
    [Header("Tilemap References - Assign in Inspector!")]
    public Tilemap floorTilemap;
    public Tilemap wallTilemap;
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    
    public class Node
    {
        public Vector2Int position;
        public Node parent;
        public float gCost;
        public float hCost;
        public float fCost => gCost + hCost;
        
        public Node(Vector2Int pos)
        {
            position = pos;
        }
    }
    
    void Awake()
    {
        if (floorTilemap == null)
        {
            floorTilemap = GameObject.Find("FloorTileMap")?.GetComponent<Tilemap>();
            if (floorTilemap == null)
                floorTilemap = GameObject.Find("Floor")?.GetComponent<Tilemap>();
        }
        
        if (wallTilemap == null)
        {
            wallTilemap = GameObject.Find("WallTileMap")?.GetComponent<Tilemap>();
            if (wallTilemap == null)
                wallTilemap = GameObject.Find("Wall")?.GetComponent<Tilemap>();
        }
        
        if (floorTilemap == null)
            Debug.LogError("FloorTilemap bulunamadı! Inspector'dan manuel olarak atayın.");
        else if (showDebugLogs)
            Debug.Log($"FloorTilemap bulundu: {floorTilemap.name}");
            
        if (wallTilemap == null)
            Debug.LogError("WallTilemap bulunamadı! Inspector'dan manuel olarak atayın.");
        else if (showDebugLogs)
            Debug.Log($"WallTilemap bulundu: {wallTilemap.name}");
    }
    
    public List<Vector2Int> FindPath(Vector3 startWorldPos, Vector3 targetWorldPos)
    {
        if (floorTilemap == null || wallTilemap == null)
        {
            Debug.LogError("Tilemap referansları null! Path bulunamıyor.");
            return null;
        }
        
        Vector2Int start = WorldToGrid(startWorldPos);
        Vector2Int target = WorldToGrid(targetWorldPos);
        
        if (showDebugLogs)
        {
            Debug.Log($"Path aranıyor: {start} → {target}");
            Debug.Log($"   Start Walkable: {IsWalkable(start)}, Target Walkable: {IsWalkable(target)}");
        }
        
        if (!IsWalkable(start))
        {
            if (showDebugLogs)
                Debug.LogWarning($"Başlangıç yürünebilir değil: {start}. En yakın noktayı arıyorum...");
            start = FindNearestWalkable(start);
            if (!IsWalkable(start))
            {
                Debug.LogError("Yürünebilir başlangıç noktası bulunamadı!");
                return null;
            }
        }
        
        if (!IsWalkable(target))
        {
            if (showDebugLogs)
                Debug.LogWarning($"Hedef yürünebilir değil: {target}. En yakın noktayı arıyorum...");
            target = FindNearestWalkable(target);
            if (!IsWalkable(target))
            {
                Debug.LogError("Yürünebilir hedef noktası bulunamadı!");
                return null;
            }
        }
        
        List<Node> openList = new List<Node>();
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();
        
        Node startNode = new Node(start);
        startNode.gCost = 0;
        startNode.hCost = GetDistance(start, target);
        
        openList.Add(startNode);
        
        int maxIterations = 10000;
        int iterations = 0;
        
        while (openList.Count > 0 && iterations < maxIterations)
        {
            iterations++;
            
            Node currentNode = openList.OrderBy(n => n.fCost).ThenBy(n => n.hCost).First();
            
            if (currentNode.position == target)
            {
                List<Vector2Int> path = GetPath(currentNode);
                if (showDebugLogs)
                    Debug.Log($"Yol bulundu! {iterations} iterasyon, {path.Count} adım");
                return path;
            }
            
            openList.Remove(currentNode);
            closedSet.Add(currentNode.position);
            
            foreach (Vector2Int direction in GetNeighborDirections())
            {
                Vector2Int neighborPos = currentNode.position + direction;
                
                if (closedSet.Contains(neighborPos) || !IsWalkable(neighborPos))
                    continue;
                
                float newGCost = currentNode.gCost + 1;
                
                Node neighborNode = openList.FirstOrDefault(n => n.position == neighborPos);
                
                if (neighborNode == null)
                {
                    neighborNode = new Node(neighborPos);
                    neighborNode.gCost = newGCost;
                    neighborNode.hCost = GetDistance(neighborPos, target);
                    neighborNode.parent = currentNode;
                    openList.Add(neighborNode);
                }
                else if (newGCost < neighborNode.gCost)
                {
                    neighborNode.gCost = newGCost;
                    neighborNode.parent = currentNode;
                }
            }
        }
        
        Debug.LogWarning($"Yol bulunamadı! {iterations} iterasyon yapıldı. Start: {start}, Target: {target}");
        return null;
    }
    
    Vector2Int FindNearestWalkable(Vector2Int position)
    {
        for (int radius = 1; radius <= 10; radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    Vector2Int checkPos = position + new Vector2Int(dx, dy);
                    if (IsWalkable(checkPos))
                    {
                        if (showDebugLogs)
                            Debug.Log($"En yakın yürünebilir nokta bulundu: {checkPos}");
                        return checkPos;
                    }
                }
            }
        }
        return position;
    }
    
    List<Vector2Int> GetPath(Node endNode)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Node currentNode = endNode;
        
        while (currentNode != null)
        {
            path.Add(currentNode.position);
            currentNode = currentNode.parent;
        }
        
        path.Reverse();
        return path;
    }
    
    bool IsWalkable(Vector2Int gridPos)
    {
        if (floorTilemap == null || wallTilemap == null)
            return false;
        
        Vector3Int cellPos = new Vector3Int(gridPos.x, gridPos.y, 0);
        
        bool hasFloor = floorTilemap.GetTile(cellPos) != null;
        bool hasWall = wallTilemap.GetTile(cellPos) != null;
        
        return hasFloor && !hasWall;
    }
    
    float GetDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
    
    Vector2Int WorldToGrid(Vector3 worldPos)
    {
        return new Vector2Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.y));
    }
    
    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x, gridPos.y, 0);
    }
    
    List<Vector2Int> GetNeighborDirections()
    {
        return new List<Vector2Int>
        {
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0)
        };
    }
    
    public void DrawPath(List<Vector2Int> path)
    {
        if (path == null || path.Count < 2) return;
        
        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector3 start = GridToWorld(path[i]);
            Vector3 end = GridToWorld(path[i + 1]);
            Debug.DrawLine(start, end, Color.cyan, 2f);
        }
    }
}