using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Board : MonoBehaviour
{
    public int rows;
    public int columns;
    public float swapDuration;
    public float fallDurationPerTile;

    private const string SelectEffectName = "SelectEffect";
    private const string HoverEffectName = "HoverEffect";

    private Tile _selectedTile;
    private Tile[,] _tiles;

    private TileType[] _types = new TileType[]
    {
        TileType.Apple,
        TileType.Broccoli,
        TileType.Coconut,
        TileType.Loaf,
        TileType.MilkCarton,
        TileType.Orange
    };

    private float _tileHeight;
    private float _tileWidth;

    private BoardState _state;
    private int _destroyedTiles;

    // This is a flag that is toggled to false after the first tile finishes swapping and then 
    // toggled again to true after the second tile finishes swapping.
    private bool _bothTilesSwapped = true;

    private void Start()
    {
        SetTileDimensions();
        InitTiles();
        RemoveHorizontalMatchesAfterInit();
        RemoveVerticalMatchesAfterInit();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            for (int i = 0; i < columns; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    Debug.Log($"({i}, {j}): type({_tiles[j, i].Type.ToString()}) null({_tiles[j, i] == null}) toBeDestroyed({_tiles[j, i].ToBeDestroyed})");
                }
            }
        }
    }

    private void InitTiles()
    {
        _tiles = new Tile[rows, columns];

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                _tiles[row, col] = CreateTile(row, col);
            }
        }

        Camera.main.orthographicSize = rows * 1.1f;
    }

    private Tile CreateTile(int row, int col)
    {
        return CreateTile(row, col, 0);
    }

    private Tile CreateTile(int row, int col, float yOffset)
    {
        TileType randomType = _types[Random.Range(0, _types.Length)];
        GameObject tileObj = new GameObject();
        SpriteRenderer renderer = tileObj.AddComponent<SpriteRenderer>();

        Tile tile = tileObj.AddComponent<Tile>();
        tile.Init(new Vector2Int(col, row), randomType);
        AddEventsToTile(tile);

        BoxCollider2D collider = tileObj.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(_tileWidth - _tileWidth * 0.05f, _tileHeight - _tileHeight * 0.05f);

        tileObj.transform.position = GetTileWorldPosition(row, col, yOffset);
        tileObj.transform.parent = transform;

        return tile;
    }

    private Vector3 GetTileWorldPosition(int row, int col)
    {
        return GetTileWorldPosition(row, col, 0);
    }

    private Vector3 GetTileWorldPosition(int row, int col, float yOffset)
    {
        float startX = -_tileWidth * (rows - 1) / 2;
        float startY = -_tileHeight * (columns - 1) / 2;

        return new Vector3(startX + col * _tileWidth, startY + (row + yOffset) * _tileHeight);
    }

    private void SetTileDimensions()
    {
        Sprite sprite = Resources.Load<Sprite>($"Sprites/{TileType.Apple}");
        _tileWidth = sprite.bounds.size.x;
        _tileHeight = sprite.bounds.size.y;
    }

    private void SelectTile(Tile tile)
    {
        if (_state == BoardState.InPlay)
        {
            if (_selectedTile != null && _selectedTile.transform.childCount > 0)
            {
                Destroy(_selectedTile.transform.GetChild(0).gameObject);
            }

            if (_selectedTile != null && IsTileInRange(tile))
            {
                if (tile.transform.childCount > 0)
                {
                    Destroy(tile.transform.GetChild(0).gameObject);
                }

                if ((_selectedTile.Position - tile.Position).sqrMagnitude == 1)
                {
                    SwapTiles(tile);
                }

                _selectedTile = null;
            }
            else
            {
                _selectedTile = tile;

                if (tile.transform.childCount > 0)
                {
                    GameObject child = tile.transform.GetChild(0).gameObject;
                    child.name = SelectEffectName;

                    SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
                    renderer.color = Color.white;
                }
            }
        }
    }

    private void SwapTiles(Tile toSwap)
    {
        _state = BoardState.SwappingTiles;

        Vector3 selectedTilePos = _selectedTile.transform.position;
        _selectedTile.MoveToPosition(toSwap.transform.position + Vector3.back, swapDuration);
        toSwap.MoveToPosition(selectedTilePos, swapDuration);

        _tiles[_selectedTile.Position.y, _selectedTile.Position.x] = toSwap;
        _tiles[toSwap.Position.y, toSwap.Position.x] = _selectedTile;

        Vector2Int toSwapPos = toSwap.Position;
        toSwap.Position = _selectedTile.Position;
        _selectedTile.Position = toSwapPos;
    }

    private bool IsTileInRange(Tile tile)
    {
        return (_selectedTile.Position - tile.Position).sqrMagnitude <= 1;
    }

    private void AddEventsToTile(Tile tile)
    {
        tile.OnMouseClick += SelectTile;
        tile.OnMouseHover += AddHoverEffect;
        tile.OnMouseExitHover += RemoveHoverEffect;
        tile.AfterMove += () => { _state = BoardState.InPlay; };
        tile.AfterMove += RemoveMatchedTiles;
        tile.AfterDisappear += DropTiles;
    }

    private void AddHoverEffect(Tile tile)
    {
        if (tile.transform.childCount == 0 && _state == BoardState.InPlay)
        {
            GameObject hoverEffect = new GameObject(HoverEffectName);
            SpriteRenderer renderer = hoverEffect.AddComponent<SpriteRenderer>();
            renderer.sprite = Resources.Load<Sprite>("Sprites/select");
            renderer.color = new Color(1, 1, 1, 0.5f);

            hoverEffect.transform.position = tile.transform.position + Vector3.forward;
            hoverEffect.transform.parent = tile.transform;
        }
    }

    private void RemoveHoverEffect(Tile tile)
    {
        if (tile.transform.childCount > 0 && tile.transform.GetChild(0).name == HoverEffectName)
        {
            Destroy(tile.transform.GetChild(0).gameObject);
        }
    }

    private void RemoveMatchedTiles()
    {
        _bothTilesSwapped = !_bothTilesSwapped;

        if (_bothTilesSwapped)
        {
            IEnumerable<Tile> matches = GetHorizontalMatches().Union(GetVerticalMatches());

            _destroyedTiles = matches.Count();

            if (_destroyedTiles > 0)
            {
                _state = BoardState.RemovingTiles;

                foreach (Tile tile in matches)
                {
                    tile.Disappear();
                }
            }
        }
    }

    private void DropTiles()
    {
        while (_destroyedTiles > 0)
        {
            _destroyedTiles--;
        }

        _state = BoardState.DroppingTiles;

        for (int col = 0; col < columns; col++)
        {
            Queue<Vector2Int> emptyTiles = new Queue<Vector2Int>();

            for (int row = 0; row < rows; row++)
            {
                if (_tiles[row, col].ToBeDestroyed)
                {
                    emptyTiles.Enqueue(new Vector2Int(col, row));
                }
                else if (emptyTiles.Count > 0)
                {
                    emptyTiles.Enqueue(new Vector2Int(col, row));

                    Vector2Int newPosition = emptyTiles.Dequeue();
                    _tiles[newPosition.y, newPosition.x] = _tiles[row, col];
                    _tiles[newPosition.y, newPosition.x].Position = newPosition;

                    int distance = row - newPosition.y;
                    _tiles[newPosition.y, newPosition.x].MoveToPosition(
                        GetTileWorldPosition(newPosition.y, newPosition.x), 
                        fallDurationPerTile * distance);

                    _tiles[row, col] = null;
                }
            }

            int numberOfNewTiles = emptyTiles.Count;

            while (emptyTiles.Count > 0)
            {
                Vector2Int position = emptyTiles.Dequeue();

                _tiles[position.y, position.x] = CreateTile(position.y, position.x, numberOfNewTiles);
                _tiles[position.y, position.x].MoveToPosition(
                    GetTileWorldPosition(position.y, position.x), 
                    fallDurationPerTile * numberOfNewTiles);
            }
        }
    }

    private ICollection<Tile> GetHorizontalMatches()
    {
        ICollection<Tile> matchedTiles = new List<Tile>();

        for (int row = 0; row < rows; row++)
        {
            int counter = 0;
            TileType type = TileType.Apple;

            for (int col = 0; col < columns; col++)
            {
                if (type == _tiles[row, col].Type)
                {
                    counter++;
                }
                else
                {
                    type = _tiles[row, col].Type;
                    counter = 1;
                }

                if (counter == 3)
                {
                    matchedTiles.Add(_tiles[row, col - 2]);
                    matchedTiles.Add(_tiles[row, col - 1]);
                    matchedTiles.Add(_tiles[row, col]);

                    _tiles[row, col - 2].ToBeDestroyed = true;
                    _tiles[row, col - 1].ToBeDestroyed = true;
                    _tiles[row, col].ToBeDestroyed = true;
                }
                else if (counter > 3)
                {
                    matchedTiles.Add(_tiles[row, col]);
                    _tiles[row, col].ToBeDestroyed = true;
                }
            }
        }

        return matchedTiles;
    }

    private ICollection<Tile> GetVerticalMatches()
    {
        ICollection<Tile> matchedTiles = new List<Tile>();

        for (int col = 0; col < columns; col++)
        {
            int counter = 0;
            TileType type = TileType.Apple;

            for (int row = 0; row < rows; row++)
            {
                if (type == _tiles[row, col].Type)
                {
                    counter++;
                }
                else
                {
                    type = _tiles[row, col].Type;
                    counter = 1;
                }

                if (counter == 3)
                {
                    matchedTiles.Add(_tiles[row - 2, col]);
                    matchedTiles.Add(_tiles[row - 1, col]);
                    matchedTiles.Add(_tiles[row, col]);

                    _tiles[row - 2, col].ToBeDestroyed = true;
                    _tiles[row - 1, col].ToBeDestroyed = true;
                    _tiles[row, col].ToBeDestroyed = true;
                }
                else if (counter > 3)
                {
                    matchedTiles.Add(_tiles[row, col]);
                    _tiles[row, col].ToBeDestroyed = true;
                }
            }
        }

        return matchedTiles;
    }

    private void RemoveHorizontalMatchesAfterInit()
    {
        for (int row = 0; row < rows; row++)
        {
            int counter = 0;
            TileType type = TileType.Apple;

            for (int col = 0; col < columns; col++)
            {
                if (type == _tiles[row, col].Type)
                {
                    counter++;
                }
                else
                {
                    type = _tiles[row, col].Type;
                    counter = 1;
                }

                if (counter > 2)
                {
                    _tiles[row, col].Type = GetNewRandomType(row, col);
                    counter = 0;
                }
            }
        }
    }

    private void RemoveVerticalMatchesAfterInit()
    {
        for (int col = 0; col < columns; col++)
        {
            int counter = 0;
            TileType type = TileType.Apple;

            for (int row = 0; row < rows; row++)
            {
                if (type == _tiles[row, col].Type)
                {
                    counter++;
                }
                else
                {
                    type = _tiles[row, col].Type;
                    counter = 1;
                }

                if (counter > 2)
                {
                    _tiles[row, col].Type = GetNewRandomType(row, col);
                    counter = 0;
                }
            }
        }
    }

    private TileType GetNewRandomType(int row, int col)
    {
        List<TileType> availableTypes = System.Enum.GetValues(typeof(TileType))
                        .Cast<TileType>()
                        .ToList();

        for (int i = Mathf.Max(0, row - 1); i <= Mathf.Min(rows - 1, row + 1); i++)
        {
            for (int j = Mathf.Max(0, col - 1); j <= Mathf.Min(columns - 1, col + 1); j++)
            {
                if ((_tiles[row, col].Position - _tiles[i, j].Position).sqrMagnitude <= 1)
                {
                    availableTypes.Remove(_tiles[i, j].Type);
                }
            }
        }

        return availableTypes[Random.Range(0, availableTypes.Count)];
    }
}