using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class DraggableBlock : MonoBehaviour
{
    public GameGrid.Block GameData { get; private set; }
    public MeshRenderer[] Blocks { get; private set; }

    private bool _isPlaced;
    private GameGrid _grid;
    private Vector3 _targetPos;
    private Color _currentColor;
    private BoxCollider2D _collider;
    
    // shared state
    private static DraggableBlock _dragTarget;
    private static bool _inDrag => _dragTarget != null;
    private static int _lastGlobalUpdateFrame = -1;
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    public Vector2Int GetGridPos()
    {
        var bottomLeft = _collider.bounds.min;
        return GameGrid.WorldPosToGridPos(bottomLeft);
    }

    public void TeleportBlockToPosition(Vector3 pos)
    { 
        _targetPos = pos;
        transform.localPosition = pos;
    }
    
    public void SetColor(Color target)
    {
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

    private void Update()
    {
        if (_lastGlobalUpdateFrame != Time.frameCount)
        {
            _lastGlobalUpdateFrame = Time.frameCount;
            GlobalBlockUpdate();
        }

        var isDragged = _dragTarget == this;

        var targetScale = isDragged || _isPlaced
            ? Vector3.one
            : Vector3.one * .5f;
        
        transform.localScale = Vector3.MoveTowards(transform.localScale, targetScale, Time.deltaTime * 100f);
        
        if (isDragged)
        {
            // our position is controlled by the mouse
            return;
        }

        transform.localPosition = Vector3.MoveTowards(transform.localPosition, _targetPos, Time.deltaTime * 100f);
    }

    private static void GlobalBlockUpdate()
    {
        if (!_inDrag)
        {
            return;
        }
        
        if (_inDrag && Input.GetMouseButtonUp(0))
        {
            var gridPos = _dragTarget.GetGridPos();

            if (_dragTarget._grid.AttemptToPlaceBlock(_dragTarget, gridPos, true))
            {
                _dragTarget._isPlaced = true;
                _dragTarget.transform.parent = null;
                var worldPos = GameGrid.GridPosToWorldPos(gridPos);
                // center the block on the new world pos
                worldPos += new Vector3(_dragTarget.GameData.width, _dragTarget.GameData.height) / 2f;
                _dragTarget.TeleportBlockToPosition(worldPos);
                _dragTarget._collider.enabled = false;

                foreach (var sr in _dragTarget.Blocks)
                {
                    if (sr == null)
                    {
                        continue;
                    }
                    sr.sortingOrder = -2;
                }
            }
            
            _dragTarget = null;
            return;
        }

        var mousePosWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosWorld.z = -1f;

        _dragTarget.transform.position = mousePosWorld + Vector3.up * 2f;
    }

    private void OnDrawGizmos()
    {
        if (!_inDrag)
        {
            return;
        }
        var gridPos = _dragTarget.GetGridPos();
        var worldPos = GameGrid.GridPosToWorldPos(gridPos);
        Gizmos.DrawCube(worldPos, Vector3.one * .5f);
    } 

    public void OnMouseDown()
    {
        if (_inDrag)
        {
            return;
        }

        _dragTarget = this;
    }

    public static DraggableBlock Create(GameGrid.Block target, GameGrid grid, Sprite sprite)
    {
        var staticData = StaticData.Instance;
        
        var parent = new GameObject($"{target.width}x{target.height}_Block").transform;
        var block = parent.AddComponent<DraggableBlock>();
        
        block.Blocks = new MeshRenderer[target.values.Length];
        
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

                var sr = b.AddComponent<MeshRenderer>();
                sr.material = staticData.BlockMaterial;

                var mf = b.AddComponent<MeshFilter>();
                mf.mesh = staticData.blockMesh;

                block.Blocks[idx] = sr;
                
                var offset = new Vector3(x, y);               
                b.transform.parent = parent;
                b.transform.localPosition = offset - halfDim + halfBlockSize;
            }
        }

        block._collider = block.AddComponent<BoxCollider2D>();
        block._collider.size = new Vector2(target.width, target.height);

        block.GameData = target;
        block._grid = grid;

        var blockColors = staticData.blockColors;
        block.SetColor(blockColors[Random.Range(0, blockColors.Count)]);
        
        return block;
    }
}
