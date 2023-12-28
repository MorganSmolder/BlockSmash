using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class DraggableBlock : MonoBehaviour
{
    public GameGrid.Block GameData { get; private set; }
    public BlockVisuals Visuals { get; private set; }

    private bool _isPlaced;
    private GameGrid _grid;
    private Vector3 _targetPos;
    private Color _currentColor;
    private BoxCollider2D _collider;
    
    // shared state
    private static DraggableBlock _dragTarget;
    private static bool _inDrag => _dragTarget != null;
    private static int _lastGlobalUpdateFrame = -1;
    private static BlockVisuals _placePreview;

    private static BlockVisuals PlacePreview
    {
        get
        {
            if (_placePreview == null)
            {
                _placePreview = new GameObject("PlacePreview")
                    .AddComponent<BlockVisuals>();
            }

            return _placePreview;
        }
    }

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
        PlacePreview.gameObject.SetActive(_inDrag);
        if (!_inDrag)
        {
            return;
        }

        if (_inDrag)
        {
            var gridPos = _dragTarget.GetGridPos();
            var worldPos = GameGrid.GridPosToWorldPos(gridPos);
            _placePreview.transform.position = worldPos + _dragTarget.GameData.HalfSize;

            // if (Input.GetMouseButtonUp(0))
            // {
            //     if (_dragTarget._grid.AttemptToPlaceBlock(_dragTarget, gridPos, true))
            //     {
            //         _dragTarget._isPlaced = true;
            //         _dragTarget.transform.parent = null;
            //         _dragTarget.TeleportBlockToPosition(worldPos);
            //         _dragTarget._collider.enabled = false;
            //     }
            
            //     _dragTarget = null;
            //     return;
            // }
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
        PlacePreview.Bind(GameData);
        var c = Visuals.Color;
        c.a = .7f;
        PlacePreview.SetColor(c);
    }

    public static DraggableBlock Create(GameGrid.Block target, GameGrid grid, Sprite sprite)
    {
        var parent = new GameObject($"{target.width}x{target.height}_Block").transform;
        var block = parent.AddComponent<DraggableBlock>();
        block.Visuals = parent.AddComponent<BlockVisuals>();
        
        block.Visuals.Bind(target);

        block._collider = block.AddComponent<BoxCollider2D>();
        block._collider.size = new Vector2(target.width, target.height);

        block.GameData = target;
        block._grid = grid;

        
        return block;
    }
}
