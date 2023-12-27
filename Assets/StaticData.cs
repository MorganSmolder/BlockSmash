using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class StaticData : MonoBehaviour
{
    public static StaticData Instance { get; private set;  }

    public List<Sprite> blockSprites;
    public List<Color> blockColors;
    public Mesh blockMesh;
    public Material BlockMaterial;
    
    private void Awake()
    {
        Debug.Assert(Instance = null);
        Instance = this;
    }
}
