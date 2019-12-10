using UnityEngine;

public class Board : MonoBehaviour
{
    public int rows;
    public int columns;
    public float swapDuration;

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

    private bool _swapping;

    private void Start()
    {
        InitTiles();
    }

    private void InitTiles()
    {
        float height = 1f;
        float width = 1f;

        _tiles = new Tile[rows, columns];
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                TileType randomType = _types[Random.Range(0, _types.Length)];
                GameObject tile = new GameObject();

                _tiles[row, col] = tile.AddComponent<Tile>();
                _tiles[row, col].Init(new Vector2Int(col, row), randomType);
                AddEventsToTile(_tiles[row, col]);

                SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
                renderer.sprite = Resources.Load<Sprite>($"Sprites/{randomType.ToString().ToLower()}");

                height = renderer.bounds.size.y;
                width = renderer.bounds.size.x;

                BoxCollider2D collider = tile.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(width - width * 0.05f, height - height * 0.05f);

                float startX = -width * (rows - 1) / 2;
                float startY = -height * (columns - 1) / 2;
                tile.transform.position = new Vector3(startX + col * width, startY + row * height, 0);

                tile.transform.parent = transform;
            }
        }

        Camera.main.orthographicSize = rows * (height / 2) * 1.1f;
    }

    private void SelectTile(Tile tile)
    {
        if (!_swapping)
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
        _swapping = true;

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

    private void SetSwappingToOff()
    {
        _swapping = false;
    }

    private void AddEventsToTile(Tile tile)
    {
        tile.OnMouseClick += SelectTile;
        tile.OnMouseHover += AddHoverEffect;
        tile.OnMouseExitHover += RemoveHoverEffect;
        tile.AfterSwap += SetSwappingToOff;
    }

    private void AddHoverEffect(Tile tile)
    {
        if (tile.transform.childCount == 0 && !_swapping)
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
}