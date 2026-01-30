using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;

public class SimpleWFC : MonoBehaviour
{
    [System.Serializable]
    public class TilePattern
    {
        public string name;
        public TileBase tile;
        public float weight = 1f;
        
        [Header("Komşu Kuralları (pattern isimleri)")]
        public List<string> validNeighborsNorth = new List<string>();
        public List<string> validNeighborsEast = new List<string>();
        public List<string> validNeighborsSouth = new List<string>();
        public List<string> validNeighborsWest = new List<string>();
    }
    
    [Header("WFC Ayarları")]
    public List<TilePattern> tilePatterns = new List<TilePattern>();
    public Tilemap decorTilemap; 
    public bool showDebugLogs = false;
    
    [Header("Template Seçimi")]
    public WFCTemplate currentTemplate = WFCTemplate.Basic;
    
    public enum WFCTemplate
    {
        Basic,      
        Detailed    
    }
    
    private class Cell
    {
        public Vector2Int position;
        public List<string> possiblePatterns;
        public bool collapsed = false;
        public string chosenPattern;
        
        public Cell(Vector2Int pos, List<string> patterns)
        {
            position = pos;
            possiblePatterns = new List<string>(patterns);
        }
        
        public int Entropy => possiblePatterns.Count;
    }
    
    public void GenerateInRoom(RectInt room, Tilemap floorTilemap)
    {
        if (tilePatterns == null || tilePatterns.Count == 0)
        {
            Debug.LogWarning("⚠️ WFC: Tile patterns tanımlanmamış!");
            return;
        }
        
        if (decorTilemap == null)
        {
            Debug.LogWarning("⚠️ WFC: Decor tilemap null!");
            return;
        }
        
        if (showDebugLogs)
            Debug.Log($"🎨 WFC başlatılıyor: Room {room}");
        
       
        Dictionary<Vector2Int, Cell> grid = new Dictionary<Vector2Int, Cell>();
        List<string> allPatternNames = tilePatterns.Select(p => p.name).ToList();
        
        
        for (int x = room.xMin + 1; x < room.xMax - 1; x++) 
        {
            for (int y = room.yMin + 1; y < room.yMax - 1; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                Vector3Int tilePos = new Vector3Int(x, y, 0);
                
                // Floor tile var mı?
                if (floorTilemap != null && floorTilemap.GetTile(tilePos) != null)
                {
                    grid[pos] = new Cell(pos, allPatternNames);
                }
            }
        }
        
        if (grid.Count == 0)
        {
            if (showDebugLogs)
                Debug.LogWarning("⚠️ WFC: Grid boş, floor tile yok!");
            return;
        }
        
       
        int maxIterations = grid.Count * 3;
        int iterations = 0;
        
        while (iterations < maxIterations)
        {
            iterations++;
            
            
            Cell lowestEntropyCell = FindLowestEntropyCell(grid);
            
            if (lowestEntropyCell == null)
            {
               
                if (showDebugLogs)
                    Debug.Log($"✅ WFC tamamlandı: {iterations} iterasyon");
                break;
            }
            
            CollapseCell(lowestEntropyCell);
            
            PropagateConstraints(grid, lowestEntropyCell);
        }
        
       
        DrawToTilemap(grid);
        
        if (showDebugLogs)
            Debug.Log($"🎨 WFC bitti: {iterations} iterasyon, {grid.Count} cell");
    }
    
    Cell FindLowestEntropyCell(Dictionary<Vector2Int, Cell> grid)
    {
        Cell lowestCell = null;
        int lowestEntropy = int.MaxValue;
        
        List<Cell> candidates = new List<Cell>();
        
        foreach (var cell in grid.Values)
        {
            if (!cell.collapsed && cell.Entropy > 0)
            {
                if (cell.Entropy < lowestEntropy)
                {
                    lowestEntropy = cell.Entropy;
                    candidates.Clear();
                    candidates.Add(cell);
                }
                else if (cell.Entropy == lowestEntropy)
                {
                    candidates.Add(cell);
                }
            }
        }
        
    
        if (candidates.Count > 0)
        {
            lowestCell = candidates[Random.Range(0, candidates.Count)];
        }
        
        return lowestCell;
    }
    
    void CollapseCell(Cell cell)
    {
        if (cell.possiblePatterns.Count == 0)
        {
        
            cell.chosenPattern = tilePatterns[0].name;
        }
        else
        {
        
            float totalWeight = 0f;
            foreach (string patternName in cell.possiblePatterns)
            {
                TilePattern pattern = tilePatterns.Find(p => p.name == patternName);
                if (pattern != null)
                    totalWeight += pattern.weight;
            }
            
            float randomValue = Random.Range(0f, totalWeight);
            float cumulativeWeight = 0f;
            
            foreach (string patternName in cell.possiblePatterns)
            {
                TilePattern pattern = tilePatterns.Find(p => p.name == patternName);
                if (pattern != null)
                {
                    cumulativeWeight += pattern.weight;
                    if (randomValue <= cumulativeWeight)
                    {
                        cell.chosenPattern = patternName;
                        break;
                    }
                }
            }
            
            if (cell.chosenPattern == null)
                cell.chosenPattern = cell.possiblePatterns[0];
        }
        
        cell.collapsed = true;
        cell.possiblePatterns.Clear();
    }
    
    void PropagateConstraints(Dictionary<Vector2Int, Cell> grid, Cell collapsedCell)
    {
        Queue<Cell> propagationQueue = new Queue<Cell>();
        propagationQueue.Enqueue(collapsedCell);
        
        HashSet<Cell> visited = new HashSet<Cell>();
        
        while (propagationQueue.Count > 0)
        {
            Cell currentCell = propagationQueue.Dequeue();
            
            if (visited.Contains(currentCell)) continue;
            visited.Add(currentCell);
            
            if (!currentCell.collapsed) continue;
            
            TilePattern currentPattern = tilePatterns.Find(p => p.name == currentCell.chosenPattern);
            if (currentPattern == null) continue;
            
         
            Vector2Int[] directions = {
                new Vector2Int(0, 1),  // North
                new Vector2Int(1, 0),  // East
                new Vector2Int(0, -1), // South
                new Vector2Int(-1, 0)  // West
            };
            
            List<string>[] validNeighbors = {
                currentPattern.validNeighborsNorth,
                currentPattern.validNeighborsEast,
                currentPattern.validNeighborsSouth,
                currentPattern.validNeighborsWest
            };
            
            for (int i = 0; i < 4; i++)
            {
                Vector2Int neighborPos = currentCell.position + directions[i];
                
                if (grid.ContainsKey(neighborPos))
                {
                    Cell neighbor = grid[neighborPos];
                    
                    if (!neighbor.collapsed)
                    {
                        int oldCount = neighbor.possiblePatterns.Count;
                        
                     
                        if (validNeighbors[i].Count > 0)
                        {
                            neighbor.possiblePatterns = neighbor.possiblePatterns
                                .Intersect(validNeighbors[i])
                                .ToList();
                        }
                        
                      
                        if (neighbor.possiblePatterns.Count < oldCount && neighbor.possiblePatterns.Count > 0)
                        {
                            propagationQueue.Enqueue(neighbor);
                        }
                        else if (neighbor.possiblePatterns.Count == 0)
                        {
                           
                            neighbor.possiblePatterns.Add(tilePatterns[0].name);
                        }
                    }
                }
            }
        }
    }
    
    void DrawToTilemap(Dictionary<Vector2Int, Cell> grid)
    {
        foreach (var cell in grid.Values)
        {
            if (cell.collapsed && cell.chosenPattern != null)
            {
                TilePattern pattern = tilePatterns.Find(p => p.name == cell.chosenPattern);
                
                if (pattern != null && pattern.tile != null)
                {
                    Vector3Int tilePos = new Vector3Int(cell.position.x, cell.position.y, 0);
                    decorTilemap.SetTile(tilePos, pattern.tile);
                }
            }
        }
    }
    
    // ✅ HELPER: Basit template patterns oluştur
    public void SetupBasicTemplate(TileBase floorTile, TileBase decorTile1, TileBase decorTile2)
    {
        tilePatterns.Clear();
        
        // Pattern 1: Empty (boş floor)
        TilePattern empty = new TilePattern();
        empty.name = "empty";
        empty.tile = null; // Boş
        empty.weight = 50f; 
        empty.validNeighborsNorth = new List<string> { "empty", "decor1", "decor2" };
        empty.validNeighborsEast = new List<string> { "empty", "decor1", "decor2" };
        empty.validNeighborsSouth = new List<string> { "empty", "decor1", "decor2" };
        empty.validNeighborsWest = new List<string> { "empty", "decor1", "decor2" };
        tilePatterns.Add(empty);
        
        // Pattern 2: Decoration 1
        TilePattern decor1 = new TilePattern();
        decor1.name = "decor1";
        decor1.tile = decorTile1;
        decor1.weight = 1f; // Az
        decor1.validNeighborsNorth = new List<string> { "empty" };
        decor1.validNeighborsEast = new List<string> { "empty" };
        decor1.validNeighborsSouth = new List<string> { "empty" };
        decor1.validNeighborsWest = new List<string> { "empty" };
        tilePatterns.Add(decor1);
        
        // Pattern 3: Decoration 2
        if (decorTile2 != null)
        {
            TilePattern decor2 = new TilePattern();
            decor2.name = "decor2";
            decor2.tile = decorTile2;
            decor2.weight = 0.5f; // Daha az
            decor2.validNeighborsNorth = new List<string> { "empty" };
            decor2.validNeighborsEast = new List<string> { "empty" };
            decor2.validNeighborsSouth = new List<string> { "empty" };
            decor2.validNeighborsWest = new List<string> { "empty" };
            tilePatterns.Add(decor2);
        }
        
        Debug.Log($"✅ Basic WFC template hazır: {tilePatterns.Count} pattern");
    }
}