using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Grid), typeof(Tilemap), typeof(TilemapRenderer))]
public class GameGrid : MonoBehaviour
{
    [Serializable]
    public struct Block
    {
        public int width;
        public int height;
        public bool[] values;
    }
    
    [Serializable]
    public struct CellMeta
    {
        public bool occupied;
        public MeshRenderer sceneObject;
    }

    [SerializeField] private Transform gameBorder;
    [SerializeField] private TMP_Text score;
    [SerializeField] private Tile blockTile;
    [SerializeField] private Tile blankTile;

    private List<DraggableBlock> _unplacedBlocks = new();
    private Grid _grid;
    private Tilemap _tilemap;
    private TilemapRenderer _renderer;
    private List<Block> _blocks = new();

    private Transform _draggableBlockParent;
    private int _score;
    private CellMeta[] _logicalGrid;

    private const int GameWidth = 8;
    private const int GameHeight = 8;
    const int HalfGameWidth = GameWidth / 2;
    const int HalfGameHeight = GameHeight / 2;

    private static float InverseLerpUnclamped(float min, float max, float t) => (t - min) / (max - min);
    private static float LerpUnclamped(float min, float max, float t) => min + (max - min) * t;

    public static Vector2Int WorldPosToGridPos(Vector3 posWorld)
    {
        var x = InverseLerpUnclamped(-HalfGameWidth, HalfGameWidth, posWorld.x);
        var y = InverseLerpUnclamped(-HalfGameHeight, HalfGameHeight, posWorld.y);

        return new Vector2Int(
            (int) LerpUnclamped(0, GameWidth, x),
            (int) LerpUnclamped(0, GameHeight, y)
        );
    }
    
    public static Vector3 GridPosToWorldPos(Vector2Int posGrid)
    {
        var x = InverseLerpUnclamped(0, GameWidth, posGrid.x);
        var y = InverseLerpUnclamped(0, GameHeight, posGrid.y);

        return new Vector3(
            LerpUnclamped(-HalfGameWidth, HalfGameWidth, x),
            LerpUnclamped(-HalfGameHeight, HalfGameHeight, y)
        );
    }

    public bool AttemptToPlaceBlock(DraggableBlock draggableBlock, Vector2Int bottomLeftGridPos, bool commit)
    {
        var gameData = draggableBlock.GameData;
        
        var topRightGridPos = bottomLeftGridPos + new Vector2Int(gameData.width, gameData.height);
        var inRangeX = bottomLeftGridPos.x >= 0 && topRightGridPos.x <= GameWidth;
        var inRangeY = bottomLeftGridPos.y >= 0 && topRightGridPos.y <= GameHeight;

        if (!inRangeX || !inRangeY)
        {
            return false;
        }

        // Check the logical grid to see if the placement is valid
        for (var x = bottomLeftGridPos.x; x < topRightGridPos.x; x++)
        {
            for (var y = bottomLeftGridPos.y; y < topRightGridPos.y; y++)
            {
                var idxGame = x + y * GameWidth;
                var idxBlock = x - bottomLeftGridPos.x + (y - bottomLeftGridPos.y) * gameData.width;

                var valGame = _logicalGrid[idxGame].occupied;
                var valBlock = gameData.values[idxBlock];
                
                var spaceOccupied = valGame && valBlock;
                if (spaceOccupied)
                {
                    return false;
                }
            }
        }

        if (!commit)
        {
            return true;
        }
        
        // Valid placement, commit the operation
        for (var x = bottomLeftGridPos.x; x < topRightGridPos.x; x++)
        {
            for (var y = bottomLeftGridPos.y; y < topRightGridPos.y; y++)
            {
                var idxGame = x + y * GameWidth;
                var idxBlock = x - bottomLeftGridPos.x + (y - bottomLeftGridPos.y) * gameData.width;

                ref var valGame = ref _logicalGrid[idxGame];
                var valBlock = gameData.values[idxBlock];

                valGame.sceneObject ??= draggableBlock.Blocks[idxBlock];
                valGame.occupied |= valBlock;
            }
        }

        _unplacedBlocks.Remove(draggableBlock);
        
        CheckForScore();
        
        if (!_unplacedBlocks.Any())
        {
            SpawnBlocks();
        }
        
        if (HasLostGame())
        {
            score.text = $"Lost, Final Score: {_score}";
        }
        
        return true;
    }
    
    private void Start()
    {        
        _logicalGrid = new CellMeta[GameWidth * GameHeight];

        gameBorder.transform.localScale = new Vector3(GameWidth + .2f, GameHeight + .1f);

        _draggableBlockParent = new GameObject("DraggableBlocks").transform;
        _draggableBlockParent.transform.position = new Vector3(0, -HalfGameHeight - HalfGameHeight / 2f);
        
        _grid = GetComponent<Grid>();
        _tilemap = GetComponent<Tilemap>();
        _renderer = GetComponent<TilemapRenderer>();

        _renderer.sortingOrder = -3;
        gameBorder.GetComponent<SpriteRenderer>().sortingOrder = -1;
        
        var blockTypes = Resources.Load<TextAsset>("block_types").text;
        foreach (var line in blockTypes.Split("\n"))
        {
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            var data = line.Split(";").SkipLast(1).ToArray();
            Debug.Assert(data.All(i => i.Length == data[0].Length));

            var values = data
                .SelectMany(i => i.ToArray())
                .Select(i =>
                {
                    Debug.Assert(i == '0' || i == '1');
                    return i == '1';
                }).ToArray();
            var height = data.Length;
            var width = data[0].Length;
            
            _blocks.Add(new Block
            {
                width = width,
                height = height,
                values = values
            });
        }

        for (var x = -HalfGameWidth; x < HalfGameWidth; x++)
        {
            for (var y = -HalfGameHeight; y < HalfGameHeight; y++)
            {
                var idx = new Vector3Int(x, y);
                _tilemap.SetTile(idx, blankTile);
            }
        }
        
        SpawnBlocks();
        RenderScore();
    }

    private void SpawnBlocks()
    {
        for (var i = -1; i <= 1; i++)
        {
            var b = DraggableBlock.Create(_blocks[Random.Range(0, _blocks.Count)], this, blockTile.sprite);

            b.transform.parent = _draggableBlockParent;
            b.TeleportBlockToPosition(new Vector3(i * 3, 0));
            b.transform.localScale = Vector3.one * .5f;
            _unplacedBlocks.Add(b);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private void CheckForScore()
    {
        const int ptsPerBlock = 5;
        var baseScore = 0;
        var combo = 0;

        // horizontal scan
        for (var x = 0; x < GameWidth; x++)
        {
            var allOccupied = true;
            for (var y = 0; y < GameHeight && allOccupied; y++)
            {
                var idx = x + y * GameWidth;
                var meta = _logicalGrid[idx];

                allOccupied &= meta.occupied;
            }

            if (!allOccupied)
            {
                continue;
            }

            baseScore += GameWidth * ptsPerBlock;
            
            // the entire row was filled
            for (var y = 0; y < GameHeight; y++)
            {
                var idx = x + y * GameWidth;
                ref var meta = ref _logicalGrid[idx];

                meta.occupied = false;
                var obj = meta.sceneObject;
                meta.sceneObject = null;
                obj.transform.DOScale(Vector3.zero, .25f);
            }

            combo++;
        }

        // vertical scan
        for (var y = 0; y < GameHeight; y++)
        {
            var allOccupied = true;
            for (var x = 0; x < GameWidth && allOccupied; x++)
            {
                var idx = x + y * GameWidth;
                var meta = _logicalGrid[idx];

                allOccupied &= meta.occupied;
            }

            if (!allOccupied)
            {
                continue;
            }

            baseScore += GameHeight * ptsPerBlock;
            
            // the entire col filled
            for (var x = 0; x < GameWidth; x++)
            {
                var idx = x + y * GameWidth;
                ref var meta = ref _logicalGrid[idx];

                meta.occupied = false;
                var obj = meta.sceneObject;
                meta.sceneObject = null;
                obj.transform.DOScale(Vector3.zero, .25f);
            }

            combo++;
        }

        _score += baseScore * combo;
        RenderScore();
    }

    public bool HasLostGame()
    {
        foreach (var block in _unplacedBlocks)
        {
            for (var x = 0; x <= GameWidth - block.GameData.width; x++)
            {
                for (var y = 0; y <= GameHeight - block.GameData.height; y++)
                {
                    var gridPos = new Vector2Int(x, y);
                    if (AttemptToPlaceBlock(block, gridPos, false))
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private void RenderScore()
    {
        score.text = _score.ToString();
    }

}
