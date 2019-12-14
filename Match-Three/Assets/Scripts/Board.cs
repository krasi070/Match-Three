using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Board : MonoBehaviour
{
    public int rows;
    public int columns;
    public int numberOfTypes;
    public float swapDuration;
    public float fallDurationPerTile;

    private const string SelectEffectName = "SelectEffect";
    private const string HoverEffectName = "HoverEffect";

    private Tile _selectedTile;
    private Tile[] _lastSwap;
    private Tile[,] _tiles;

    private TileType[] _types;

    private float _tileHeight;
    private float _tileWidth;

    private BoardState _state;
    private int _tilesToDestroy;
    private int _currMovingTiles;

    private void Start()
    {
        SetTileTypes();
        SetTileDimensions();

        InitTiles();
        RemoveHorizontalMatchesAfterInit();
        RemoveVerticalMatchesAfterInit();
    }

    private void SetTileTypes()
    {
        _types = System.Enum.GetValues(typeof(TileType))
            .Cast<TileType>()
            .Take(numberOfTypes)
            .ToArray();
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

        Camera.main.orthographicSize = Mathf.Max(rows, columns / 2 + _tileWidth) * (_tileHeight / 2) * 1.1f;
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
        float startX = -_tileWidth * (columns - 1) / 2;
        float startY = -_tileHeight * (rows - 1) / 2;

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
            if (_selectedTile != null && !_selectedTile.Equals(tile) && _selectedTile.transform.childCount > 0)
            {
                _selectedTile.Selected = false;
                Destroy(_selectedTile.transform.GetChild(0).gameObject);
            }

            if (_selectedTile != null && _selectedTile.IsAdjecentTo(tile))
            {
                if (tile.transform.childCount > 0)
                {
                    Destroy(tile.transform.GetChild(0).gameObject);
                }

                SwapTiles(_selectedTile, tile);
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

    private void DeselectTile(Tile tile)
    {
        if (_state == BoardState.InPlay && _selectedTile != null)
        {
            if (_selectedTile.Equals(tile))
            {
                if (tile.Selected && tile.transform.childCount > 0)
                {
                    Destroy(tile.transform.GetChild(0).gameObject);
                    tile.Selected = false;
                    _selectedTile = null;
                }
                else
                {
                    tile.Selected = true;
                }
            }
        }
    }

    private void DragTile(Tile tile)
    {
        if (_state == BoardState.InPlay && _selectedTile != null && Input.GetMouseButton(0))
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = -Camera.main.transform.position.z;
            Vector3 pos = Camera.main.ScreenToWorldPoint(mousePos);

            if ((_selectedTile.transform.position - pos).sqrMagnitude > Mathf.Min(_tileHeight, _tileWidth) / 2 &&
                _selectedTile.IsAdjecentTo(tile))
            {
                if (_selectedTile.transform.childCount > 0)
                {
                    Destroy(_selectedTile.transform.GetChild(0).gameObject);
                }

                if (tile.transform.childCount > 0)
                {
                    Destroy(tile.transform.GetChild(0).gameObject);
                }

                SwapTiles(_selectedTile, tile);

                _selectedTile = null;
            }
        }
    }

    private void SwapTiles(Tile t1, Tile t2)
    {
        _state = BoardState.SwappingTiles;

        Vector3 t1Pos = t1.transform.position;
        t1.MoveToPosition(t2.transform.position + Vector3.back, swapDuration);
        t2.MoveToPosition(t1Pos, swapDuration);
        _currMovingTiles += 2;

        _tiles[t1.Position.y, t1.Position.x] = t2;
        _tiles[t2.Position.y, t2.Position.x] = t1;

        Vector2Int t2Pos = t2.Position;
        t2.Position = t1.Position;
        t1.Position = t2Pos;

        _lastSwap = new Tile[] { t1, t2 };
    }

    private void UndoLastSwap()
    {
        _lastSwap[0].Selected = false;
        SwapTiles(_lastSwap[0], _lastSwap[1]);

        _lastSwap = null;
    }

    private void AddEventsToTile(Tile tile)
    {
        tile.OnMouseClick += SelectTile;
        tile.OnMouseRelease += DeselectTile;
        tile.OnMouseHover += AddHoverEffect;
        tile.OnMouseHover += DragTile;
        tile.OnMouseExitHover += RemoveHoverEffect;
        tile.AfterMove += CheckForMatches;
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

    private void CheckForMatches()
    {
        _currMovingTiles--;

        if (_currMovingTiles == 0)
        {
            IEnumerable<Tile> matches = GetHorizontalMatches().Union(GetVerticalMatches());
            _tilesToDestroy += matches.Count();

            if (_tilesToDestroy > 0)
            {
                RemoveTiles(matches);
            }
            else if (_state == BoardState.SwappingTiles && _lastSwap != null)
            {
                UndoLastSwap();
            }
            else
            {
                _state = BoardState.InPlay;
            }
        }
    }

    private void RemoveTiles(IEnumerable<Tile> matches)
    {
        _state = BoardState.RemovingTiles;

        foreach (Tile tile in matches)
        {
            tile.Disappear();
        }
    }

    private void DropTiles()
    {
        while (_tilesToDestroy > 0)
        {
            _tilesToDestroy--;
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
                    _currMovingTiles++;

                    _tiles[row, col] = null;
                }
            }

            DropNewTiles(emptyTiles);
        }
    }

    private void DropNewTiles(Queue<Vector2Int> emptyTiles)
    {
        int numberOfNewTiles = emptyTiles.Count;

        while (emptyTiles.Count > 0)
        {
            Vector2Int position = emptyTiles.Dequeue();

            _tiles[position.y, position.x] = CreateTile(position.y, position.x, numberOfNewTiles);
            _tiles[position.y, position.x].MoveToPosition(
                GetTileWorldPosition(position.y, position.x),
                fallDurationPerTile * numberOfNewTiles);
            _currMovingTiles++;
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
        List<TileType> availableTypes = new List<TileType>(_types);

        for (int i = Mathf.Max(0, row - 1); i <= Mathf.Min(rows - 1, row + 1); i++)
        {
            for (int j = Mathf.Max(0, col - 1); j <= Mathf.Min(columns - 1, col + 1); j++)
            {
                if (_tiles[row, col].Equals(_tiles[i, j]) || _tiles[row, col].IsAdjecentTo(_tiles[i, j]))
                {
                    availableTypes.Remove(_tiles[i, j].Type);
                }
            }
        }

        return availableTypes[Random.Range(0, availableTypes.Count)];
    }
}