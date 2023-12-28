using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockVisuals : MonoBehaviour
{
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    public MeshRenderer[] Blocks { get; private set; }
    public Color Color { get; private set; }
    
    public void SetColor(Color target)
    {
        Color = target;
        var emissiveColor = target;
        emissiveColor.a = .1f;
        foreach (var sr in Blocks)
        {
            if (sr == null)
            {
                continue;
            }
            sr.material.color = target;
            sr.material.SetColor(EmissionColor, emissiveColor);
        }
    }

    public void Bind(GameGrid.Block target)
    {
        if (Blocks != null)
        {
            foreach (var block in Blocks)
            {
                if (block == null)
                {
                    continue;
                }
                Destroy(block.gameObject);
            }
        }        
        
        var staticData = StaticData.Instance;

        Blocks = new MeshRenderer[target.values.Length];

        var halfDim = new Vector3(target.width / 2f, target.height / 2f);
        var halfBlockSize = new Vector3(.5f, .5f);
        for (var x = 0; x < target.width; x++)
        {
            for (var y = 0; y < target.height; y++)
            {
                var idx = x + y * target.width;
                if (!target.values[idx])
                {
                    continue;
                }
                var b = new GameObject($"{target.width}x{target.height}");

                var mr = b.AddComponent<MeshRenderer>();
                mr.material = staticData.BlockMaterial;

                var mf = b.AddComponent<MeshFilter>();
                mf.mesh = staticData.blockMesh;

                Blocks[idx] = mr;
                
                var offset = new Vector3(x, y);               
                b.transform.parent = transform;
                b.transform.localPosition = offset - halfDim + halfBlockSize;
            }
        }
        
        var blockColors = staticData.blockColors;
        SetColor(blockColors[Random.Range(0, blockColors.Count)]);

    }
    
}
