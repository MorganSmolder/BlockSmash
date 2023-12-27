using System.Collections.Generic;
using UnityEngine;

public class StaticData : MonoBehaviour
{
    public static StaticData Instance { get; private set;  }

    public List<Sprite> blockColors;
    
    private void Awake()
    {
        Debug.Assert(Instance = null);
        Instance = this;
    }
}
