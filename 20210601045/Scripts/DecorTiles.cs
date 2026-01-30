using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;

public class CreateTiles : MonoBehaviour
{
    [MenuItem("Tools/Create Decor Tiles")]
    static void CreateDecorTiles()
    {
        
        if (!AssetDatabase.IsValidFolder("Assets/Tiles"))
        {
            AssetDatabase.CreateFolder("Assets", "Tiles");
        }

    
        Tile tile1 = ScriptableObject.CreateInstance<Tile>();

        AssetDatabase.CreateAsset(tile1, "Assets/Tiles/DecorTile1.asset");

        Tile tile2 = ScriptableObject.CreateInstance<Tile>();
       
        AssetDatabase.CreateAsset(tile2, "Assets/Tiles/DecorTile2.asset");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(" DecorTile1 (oluşturuldu!");
    }
}
#endif