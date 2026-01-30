using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class DungeonGenerator : MonoBehaviour
{
    [Header("Tilemap References")]
    public Tilemap floorTilemap; 
    public Tilemap wallTilemap;
    
    [Header("Tile References")]
    public TileBase floorTile;
    public TileBase wallTile;

    [Header("WFC Settings")]
    public SimpleWFC wfcGenerator;
    public TileBase decorTile1; 
    public TileBase decorTile2; 
    public Tilemap decorTilemap; 
    
    [Header("Generation Settings")]
    public int mapWidth = 100;
    public int mapHeight = 100;
    public int minRoomSize = 10;
    public int targetRoomCount = 10;

    [Header("Marker Settings")]
    public Sprite markerSprite; 
    public Color startMarkerColor = Color.green;
    public Color goalMarkerColor = Color.yellow;
    public float startMarkerSize = 1.2f;
    public float goalMarkerSize = 1.5f;
   
    [Header("AI Agent Setup")]
    public GameObject aiAgentPrefab;
    
    [Header("Enemy Setup")]
    public GameObject stationaryEnemyPrefab;
    public GameObject patrolEnemyPrefab;
    public GameObject chaserEnemyPrefab;
    
    [Header("Enemy Counts")]
    public int stationaryEnemyCount = 3;
    public int patrolEnemyCount = 4;
    public int chaserEnemyCount = 3;
    
    [Header("Debug Markers")]
    public GameObject startMarkerPrefab;
    public GameObject goalMarkerPrefab;

    private BSPNode rootNode;
    private List<BSPNode> allLeafNodes = new List<BSPNode>();
    private BSPNode startRoomNode;
    private BSPNode goalRoomNode;
    
    private GameObject spawnedAgent;
    private GameObject startMarker;
    private GameObject goalMarker;
    private GameObject wallColliderContainer;

    void Start()
    {
        GenerateDungeon();
        CreateWallColliders();
        ApplyWFCToAllRooms();
        SpawnAgent();
        SpawnEnemies();
        PlaceDebugMarkers();
    }

    void GenerateDungeon()
    {
        floorTilemap.ClearAllTiles();
        wallTilemap.ClearAllTiles();
        allLeafNodes.Clear();

        rootNode = new BSPNode(new Vector2Int(0, 0), new Vector2Int(mapWidth, mapHeight));
        SplitUntilTargetRoomCount(rootNode, targetRoomCount);
        CollectLeafNodes(rootNode, allLeafNodes);

        Debug.Log($"Oluşturulan oda sayısı: {allLeafNodes.Count}");

        CreateRoomsAndCorridors(rootNode);
        AssignStartAndGoalRooms();
        PlaceWallsAroundFloor();
    }
    
    void ApplyWFCToAllRooms()
    {
        if (wfcGenerator == null)
        {
            Debug.LogWarning("WFC Generator atanmamış!");
            return;
        }
        
        if (decorTilemap == null)
        {
            Debug.LogWarning("Decor Tilemap atanmamış!");
            return;
        }
        
        if (decorTile1 == null || decorTile2 == null)
        {
            Debug.LogWarning("Decor Tile'lar atanmamış!");
            return;
        }
        
        wfcGenerator.SetupBasicTemplate(floorTile, decorTile1, decorTile2);
        wfcGenerator.decorTilemap = decorTilemap;
        
        BoundsInt bounds = floorTilemap.cellBounds;
        RectInt wholeArea = new RectInt(bounds.xMin, bounds.yMin, bounds.size.x, bounds.size.y);
        wfcGenerator.GenerateInRoom(wholeArea, floorTilemap);
        
        Debug.Log($"WFC tamamlandı: Tüm dungeons'a uygulandı");
    }

    void CreateWallColliders()
    {
        if (wallColliderContainer != null)
        {
            Destroy(wallColliderContainer);
        }
        
        wallColliderContainer = new GameObject("WallColliders");
        wallColliderContainer.transform.parent = transform;
        
        int colliderCount = 0;
        
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                
                if (wallTilemap.GetTile(pos) != null)
                {
                    GameObject wallGO = new GameObject($"Wall_{x}_{y}");
                    wallGO.transform.parent = wallColliderContainer.transform;
                    wallGO.transform.position = new Vector3(x, y, 0);
                    wallGO.tag = "Wall";
                    wallGO.layer = LayerMask.NameToLayer("Wall");
                    
                    BoxCollider2D wallCollider = wallGO.AddComponent<BoxCollider2D>();
                    wallCollider.size = Vector2.one;
                    wallCollider.isTrigger = false;
                    
                    PhysicsMaterial2D wallMaterial = new PhysicsMaterial2D("WallMaterial");
                    wallMaterial.friction = 0.8f;
                    wallMaterial.bounciness = 0f;
                    wallCollider.sharedMaterial = wallMaterial;
                    
                    colliderCount++;
                }
            }
        }
        
        Debug.Log($"{colliderCount} wall collider oluşturuldu!");
    }

    void SpawnAgent()
    {
        if (aiAgentPrefab == null)
        {
            Debug.LogError("AI Agent Prefab atanmamış!");
            return;
        }

        Vector3 startPos = GetStartPosition();
        spawnedAgent = Instantiate(aiAgentPrefab, startPos, Quaternion.identity);
        spawnedAgent.name = "AI Agent";
    }

    void SpawnEnemies()
    {
        if (allLeafNodes.Count < 2)
        {
            Debug.LogWarning("Yeterli oda yok!");
            return;
        }
        
        BSPNode startRoom = startRoomNode;
        
        for (int i = 0; i < stationaryEnemyCount; i++)
        {
            Vector3 pos = GetRandomRoomPosition(startRoom);
            if (stationaryEnemyPrefab != null)
            {
                GameObject enemy = Instantiate(stationaryEnemyPrefab, pos, Quaternion.identity);
                enemy.name = $"Stationary Enemy {i + 1}";
                enemy.layer = LayerMask.NameToLayer("Enemy");
                enemy.tag = "Enemy";
            }
        }
        
        for (int i = 0; i < patrolEnemyCount; i++)
        {
            Vector3 pos = GetRandomRoomPosition(startRoom);
            if (patrolEnemyPrefab != null)
            {
                GameObject enemy = Instantiate(patrolEnemyPrefab, pos, Quaternion.identity);
                enemy.name = $"Patrol Enemy {i + 1}";
                enemy.layer = LayerMask.NameToLayer("Enemy");
                enemy.tag = "Enemy";
            }
        }
        
        for (int i = 0; i < chaserEnemyCount; i++)
        {
            Vector3 pos = GetRandomRoomPosition(startRoom);
            if (chaserEnemyPrefab != null)
            {
                GameObject enemy = Instantiate(chaserEnemyPrefab, pos, Quaternion.identity);
                enemy.name = $"Chaser Enemy {i + 1}";
                enemy.layer = LayerMask.NameToLayer("Enemy");
                enemy.tag = "Enemy";
            }
        }
        
        Debug.Log($"{stationaryEnemyCount + patrolEnemyCount + chaserEnemyCount} düşman spawn edildi!");
    }

    void CreateMarkers()
    {
        GameObject startMarker = new GameObject("Start Marker");
        SpriteRenderer startSR = startMarker.AddComponent<SpriteRenderer>();
        startSR.sprite = markerSprite; 
        startSR.color = startMarkerColor; 
        startSR.sortingOrder = 5; 
        startMarker.transform.localScale = Vector3.one * startMarkerSize;
        
        GameObject goalMarker = new GameObject("Goal Marker");
        SpriteRenderer goalSR = goalMarker.AddComponent<SpriteRenderer>();
        goalSR.sprite = markerSprite; 
        goalSR.color = goalMarkerColor; 
        goalSR.sortingOrder = 5;
        goalMarker.transform.localScale = Vector3.one * goalMarkerSize;
    }

    void PlaceDebugMarkers()
    {
        Vector3 startPos = GetStartPosition();
        Vector3 goalPos = GetGoalPosition();

        if (startMarkerPrefab != null)
        {
            startMarker = Instantiate(startMarkerPrefab, startPos, Quaternion.identity);
            startMarker.name = "Start Marker";
        }
        else
        {
            startMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            startMarker.transform.position = startPos;
            startMarker.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
            startMarker.GetComponent<Renderer>().material.color = Color.green;
            startMarker.name = "Start Marker";
            Destroy(startMarker.GetComponent<Collider>());
        }

        if (goalMarkerPrefab != null)
        {
            goalMarker = Instantiate(goalMarkerPrefab, goalPos, Quaternion.identity);
            goalMarker.name = "Goal Marker";
        }
        else
        {
            goalMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            goalMarker.transform.position = goalPos;
            goalMarker.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
            goalMarker.GetComponent<Renderer>().material.color = Color.red;
            goalMarker.name = "Goal Marker";
            Destroy(goalMarker.GetComponent<Collider>());
        }
    }

    Vector3 GetRandomRoomPosition(BSPNode excludeRoom)
    {
        BSPNode randomRoom;
        int attempts = 0;
        
        do
        {
            randomRoom = allLeafNodes[Random.Range(0, allLeafNodes.Count)];
            attempts++;
        } while (randomRoom == excludeRoom && attempts < 20);
        
        RectInt room = randomRoom.room;
        float x = Random.Range(room.xMin + 2, room.xMax - 2);
        float y = Random.Range(room.yMin + 2, room.yMax - 2);
        
        return new Vector3(x, y, 0);
    }

    public Vector3 GetStartPosition()
    {
        if (startRoomNode == null || startRoomNode.room.width == 0) return Vector3.zero;
        Vector2 center = startRoomNode.room.center;
        return new Vector3(center.x, center.y, 0);
    }

    public Vector3 GetGoalPosition()
    {
        if (goalRoomNode == null || goalRoomNode.room.width == 0) return Vector3.zero;
        Vector2 center = goalRoomNode.room.center;
        return new Vector3(center.x, center.y, 0);
    }

    public Vector3 GetRandomSpawnPoint()
    {
        if (allLeafNodes.Count == 0) return Vector3.zero;
        BSPNode randomNode = allLeafNodes[Random.Range(0, allLeafNodes.Count)];
        Vector2 center = randomNode.room.center;
        return new Vector3(center.x, center.y, 0);
    }

    void SplitUntilTargetRoomCount(BSPNode node, int targetCount)
    {
        List<BSPNode> currentLeaves = new List<BSPNode>();
        CollectLeafNodes(node, currentLeaves);

        if (currentLeaves.Count >= targetCount) return;

        BSPNode largestLeaf = FindLargestLeaf(currentLeaves);
        if (largestLeaf == null) return;

        if (TrySplitNode(largestLeaf))
        {
            SplitUntilTargetRoomCount(node, targetCount);
        }
    }

    BSPNode FindLargestLeaf(List<BSPNode> leaves)
    {
        BSPNode largest = null;
        int maxArea = 0;

        foreach (BSPNode leaf in leaves)
        {
            int area = leaf.size.x * leaf.size.y;
            if (area > maxArea && CanSplit(leaf))
            {
                maxArea = area;
                largest = leaf;
            }
        }
        return largest;
    }

    bool CanSplit(BSPNode node)
    {
        return (node.size.x >= minRoomSize * 2 || node.size.y >= minRoomSize * 2);
    }

    bool TrySplitNode(BSPNode node)
    {
        if (!CanSplit(node)) return false;

        bool splitHorizontally;

        if (node.size.x > node.size.y && node.size.x >= minRoomSize * 2)
            splitHorizontally = false;
        else if (node.size.y > node.size.x && node.size.y >= minRoomSize * 2)
            splitHorizontally = true;
        else if (node.size.x >= minRoomSize * 2 && node.size.y >= minRoomSize * 2)
            splitHorizontally = Random.value > 0.5f;
        else
            return false;

        if (splitHorizontally)
            SplitHorizontal(node);
        else
            SplitVertical(node);

        return true;
    }

    void CollectLeafNodes(BSPNode node, List<BSPNode> leaves)
    {
        if (node.IsLeaf())
            leaves.Add(node);
        else
        {
            if (node.child1 != null) CollectLeafNodes(node.child1, leaves);
            if (node.child2 != null) CollectLeafNodes(node.child2, leaves);
        }
    }

    void SplitHorizontal(BSPNode node)
    {
        int splitPoint = Random.Range(node.position.y + minRoomSize, node.position.y + node.size.y - minRoomSize);
        node.child1 = new BSPNode(node.position, new Vector2Int(node.size.x, splitPoint - node.position.y));
        node.child2 = new BSPNode(new Vector2Int(node.position.x, splitPoint), new Vector2Int(node.size.x, node.size.y - (splitPoint - node.position.y)));
    }

    void SplitVertical(BSPNode node)
    {
        int splitPoint = Random.Range(node.position.x + minRoomSize, node.position.x + node.size.x - minRoomSize);
        node.child1 = new BSPNode(node.position, new Vector2Int(splitPoint - node.position.x, node.size.y));
        node.child2 = new BSPNode(new Vector2Int(splitPoint, node.position.y), new Vector2Int(node.size.x - (splitPoint - node.position.x), node.size.y));
    }

    void AssignStartAndGoalRooms()
    {
        if (allLeafNodes.Count < 2) return;
        startRoomNode = allLeafNodes[0];
        goalRoomNode = allLeafNodes[allLeafNodes.Count - 1];
    }

    Vector2Int GetRoomCenter(BSPNode node)
    {
        if (node.IsLeaf())
            return Vector2Int.RoundToInt(node.room.center);

        if (node.child1 != null && node.child2 != null)
            return Random.value < 0.5f ? GetRoomCenter(node.child1) : GetRoomCenter(node.child2);
        else if (node.child1 != null)
            return GetRoomCenter(node.child1);
        else if (node.child2 != null)
            return GetRoomCenter(node.child2);

        return Vector2Int.zero;
    }

    void CreateRoomsAndCorridors(BSPNode node)
    {
        if (node.IsLeaf())
        {
            CreateRoom(node);
        }
        else
        {
            if (node.child1 != null) CreateRoomsAndCorridors(node.child1);
            if (node.child2 != null) CreateRoomsAndCorridors(node.child2);

            if (node.child1 != null && node.child2 != null)
            {
                Vector2Int center1 = GetRoomCenter(node.child1);
                Vector2Int center2 = GetRoomCenter(node.child2);
                CreateCorridor(center1, center2);
            }
        }
    }

    void CreateRoom(BSPNode node)
    {
        int roomWidth = Random.Range(minRoomSize, node.size.x - 2);
        int roomHeight = Random.Range(minRoomSize, node.size.y - 2);
        int xPos = Random.Range(node.position.x + 1, node.position.x + node.size.x - roomWidth - 1);
        int yPos = Random.Range(node.position.y + 1, node.position.y + node.size.y - roomHeight - 1);

        node.room = new RectInt(xPos, yPos, roomWidth, roomHeight);
        DrawRoom(node.room);
    }

    void CreateCorridor(Vector2Int center1, Vector2Int center2)
    {
        int corridorWidth = 2;
        Vector2Int pos = center1;

        while (pos.x != center2.x)
        {
            for (int offset = 0; offset < corridorWidth; offset++)
            {
                Vector3Int tilePos = new Vector3Int(pos.x, pos.y + offset, 0);
                floorTilemap.SetTile(tilePos, floorTile);
                wallTilemap.SetTile(tilePos, null);
            }
            pos.x += (pos.x < center2.x) ? 1 : -1;
        }

        while (pos.y != center2.y)
        {
            for (int offset = 0; offset < corridorWidth; offset++)
            {
                Vector3Int tilePos = new Vector3Int(pos.x + offset, pos.y, 0);
                floorTilemap.SetTile(tilePos, floorTile);
                wallTilemap.SetTile(tilePos, null);
            }
            pos.y += (pos.y < center2.y) ? 1 : -1;
        }
    }

    void DrawRoom(RectInt room)
    {
        for (int x = room.xMin; x < room.xMax; x++)
        {
            for (int y = room.yMin; y < room.yMax; y++)
            {
                floorTilemap.SetTile(new Vector3Int(x, y, 0), floorTile);
            }
        }

        for (int x = room.xMin - 1; x <= room.xMax; x++)
        {
            for (int y = room.yMin - 1; y <= room.yMax; y++)
            {
                if (x == room.xMin - 1 || x == room.xMax || y == room.yMin - 1 || y == room.yMax)
                {
                    if (floorTilemap.GetTile(new Vector3Int(x, y, 0)) == null)
                    {
                        wallTilemap.SetTile(new Vector3Int(x, y, 0), wallTile);
                    }
                }
            }
        }
    }

    void PlaceWallsAroundFloor()
    {
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);

                if (floorTilemap.GetTile(pos) != null)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;

                            Vector3Int neighborPos = new Vector3Int(x + dx, y + dy, 0);

                            if (neighborPos.x >= 0 && neighborPos.x < mapWidth &&
                                neighborPos.y >= 0 && neighborPos.y < mapHeight)
                            {
                                if (floorTilemap.GetTile(neighborPos) == null &&
                                    wallTilemap.GetTile(neighborPos) == null)
                                {
                                    wallTilemap.SetTile(neighborPos, wallTile);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}