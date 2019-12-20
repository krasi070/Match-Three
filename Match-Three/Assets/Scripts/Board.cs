using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Board : MonoBehaviour
{
    public int rows;
    public int columns;
    public int numberOfTypes;
    public float swapDuration;
    public float fallDurationPerTile;

    public ScoreTracker scoreTracker;
    public Text levelTextField;
    public Timer timer;
    public GameObject noMoreMovesField;
    public GameObject timesUpField;

    private const string SelectEffectName = "SelectEffect";
    private const string HoverEffectName = "HoverEffect";
    private const int StartingThreeMatchPoints = 10;
    private const float StartingThreeMatchSeconds = 5f;

    private int _threeMatchPoints = 10;
    private float _threeMatchSeconds = 5f;

    private Tile _selectedTile;
    private Tile[] _lastSwap;
    private Tile[,] _tiles;

    private Queue<List<Tile>> _matchedTiles = new Queue<List<Tile>>();
    private List<Tile> _hints;

    private Canvas _canvas;

    private TileType[] _types;

    private float _tileHeight;
    private float _tileWidth;

    private BoardState _state;
    private int _tilesToDestroy;
    private int _currMovingTiles;

    private int _level;
    private int _points;
    private int _multiplier = 1;

    private void Start()
    {
        SetLevel(1);
        SetCanvas();
        SetTileTypes();
        SetTileDimensions();

        InitTiles();
        RemoveHorizontalMatchesAfterInit();
        RemoveVerticalMatchesAfterInit();
        MovesLeft();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && (_state == BoardState.NoMoreMoves || _state == BoardState.GameOver))
        {
            if (_state == BoardState.GameOver)
            {
                SetLevel(1);
                _points = 0;
                scoreTracker.UpdateScore(_points);
                _threeMatchPoints = StartingThreeMatchPoints;
                _threeMatchSeconds = StartingThreeMatchSeconds;
                timer.ResetTimeLeft();
            }

            StopShakingTiles();
            noMoreMovesField.SetActive(false);
            timesUpField.SetActive(false);
            ResetTiles();
            timer.Resume();
        }
    }

    public void GiveHint()
    {
        if (_state == BoardState.InPlay && _hints != null)
        {
            foreach (Tile tile in _hints)
            {
                tile.StartHighlight();
            }
        }
    }

    private void SetCanvas()
    {
        _canvas = transform.GetChild(0).GetComponent<Canvas>();
        AddEventsToTimer();
    }

    private void SetTileTypes()
    {
        _types = System.Enum.GetValues(typeof(TileType))
            .Cast<TileType>()
            .Take(numberOfTypes)
            .ToArray();
    }

    private void SetLevel(int level)
    {
        _level = level;
        levelTextField.text = _level.ToString().PadLeft(2, '0');
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
        Camera.main.transform.position = new Vector3(0f, -0.0625f * rows, Camera.main.transform.position.z);
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
        tile.OnMouseClick += RemoveAllHints;
        tile.OnMouseRelease += DeselectTile;
        tile.OnMouseHover += AddHoverEffect;
        tile.OnMouseHover += DragTile;
        tile.OnMouseExitHover += RemoveHoverEffect;
        tile.AfterMove += CheckForMatches;
        tile.AfterDisappear += DropTiles;
        tile.AfterAppear += () => { _state = _state != BoardState.NoMoreMoves ? BoardState.InPlay : BoardState.NoMoreMoves; };
    }

    private void AddEventsToTimer()
    {
        timer.ReachedZero += GameOver;
        timer.ReachedMax += GoToNextLevel;
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

    // This method does not need a parameter.
    // The parameter is there because the OnMouseClick event requests it.
    private void RemoveAllHints(Tile t)
    {
        if (_hints != null)
        {
            foreach (Tile tile in _hints)
            {
                tile.StopHighlight();
            }
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
                AddUpPoints();
                scoreTracker.UpdateScore(_points);
                RemoveTiles(matches);
            }
            else if (_state == BoardState.SwappingTiles && _lastSwap != null)
            {
                UndoLastSwap();
            }
            else
            {
                _state = BoardState.InPlay;
                _multiplier = 1;

                if (!MovesLeft())
                {
                    _state = BoardState.NoMoreMoves;
                    ShakeTiles();
                    noMoreMovesField.SetActive(true);
                    timer.Pause();
                }
            }
        }
    }

    private void RemoveTiles(IEnumerable<Tile> matches)
    {
        _state = BoardState.RemovingTiles;

        foreach (Tile tile in matches)
        {
            tile.Disappear(true);
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

    private void ResetTiles()
    {
        _state = BoardState.Resetting;

        foreach (Transform child in transform)
        {
            if (child.name != _canvas.name)
            {
                Destroy(child.gameObject);
            }
        }

        InitTiles();
        RemoveHorizontalMatchesAfterInit();
        RemoveVerticalMatchesAfterInit();
        ShowAllTiles();

        if (!MovesLeft())
        {
            _state = BoardState.NoMoreMoves;
            ShakeTiles();
            noMoreMovesField.SetActive(true);
            timer.Pause();
        }
    }

    private void ShowAllTiles()
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                _tiles[row, col].Appear();
            }
        }
    }

    private bool MovesLeft()
    {
        _hints = new List<Tile>();

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if (PossibleMatchInLine(_tiles[row, col]) ||
                    PossibleMatchXShape(_tiles[row, col]) ||
                    PossibleMatchLyingL(_tiles[row, col]) ||
                    PossibleMatchStandingL(_tiles[row, col]))
                {
                    _hints.Add(_tiles[row, col]);
                }
            }
        }

        return _hints.Count > 0;
    }

    private bool PossibleMatchInLine(Tile tile)
    {
        int row = tile.Position.y;
        int col = tile.Position.x;
        TileType currType = tile.Type;

        // (x) y x x
        if (col > 2 && _tiles[row, col - 2].Type == currType && _tiles[row, col - 3].Type == currType)
        {
            return true;
        }

        if (row < rows - 3 && _tiles[row + 2, col].Type == currType && _tiles[row + 3, col].Type == currType)
        {
            return true;
        }

        if (col < columns - 3 && _tiles[row, col + 2].Type == currType && _tiles[row, col + 3].Type == currType)
        {
            return true;
        }

        if (row > 2 && _tiles[row - 2, col].Type == currType && _tiles[row - 3, col].Type == currType)
        {
            return true;
        }

        return false;
    }

    private bool PossibleMatchXShape(Tile tile)
    {
        int row = tile.Position.y;
        int col = tile.Position.x;
        TileType currType = tile.Type;

        // z (x) a 
        // x  y  x
        if (col > 0 && col < columns - 1 && row < rows - 1 &&
            _tiles[row + 1, col - 1].Type == currType && _tiles[row + 1, col + 1].Type == currType)
        {
            return true;
        }

        if (row > 0 && row < rows - 1 && col < columns - 1 &&
            _tiles[row + 1, col + 1].Type == currType && _tiles[row - 1, col + 1].Type == currType)
        {
            return true;
        }

        if (col > 0 && col < columns - 1 && row > 0 &&
            _tiles[row - 1, col - 1].Type == currType && _tiles[row - 1, col + 1].Type == currType)
        {
            return true;
        }

        if (row > 0 && row < rows - 1 && col > 0 &&
            _tiles[row + 1, col - 1].Type == currType && _tiles[row - 1, col - 1].Type == currType)
        {
            return true;
        }

        return false;
    }

    private bool PossibleMatchLyingL(Tile tile)
    {
        int row = tile.Position.y;
        int col = tile.Position.x;
        TileType currType = tile.Type;

        // z y (x)
        // x x  a
        if (col > 1 && row < rows - 1 &&
            _tiles[row + 1, col - 2].Type == currType && _tiles[row + 1, col - 1].Type == currType)
        {
            return true;
        }

        if (col < columns - 2 && row < rows - 1 &&
            _tiles[row + 1, col + 1].Type == currType && _tiles[row + 1, col + 2].Type == currType)
        {
            return true;
        }

        if (col < columns - 2 && row > 0 &&
            _tiles[row - 1, col + 1].Type == currType && _tiles[row - 1, col + 2].Type == currType)
        {
            return true;
        }

        if (col > 1 && row > 0 &&
            _tiles[row - 1, col - 2].Type == currType && _tiles[row - 1, col - 1].Type == currType)
        {
            return true;
        }

        return false;
    }

    private bool PossibleMatchStandingL(Tile tile)
    {
        int row = tile.Position.y;
        int col = tile.Position.x;
        TileType currType = tile.Type;

        // y (x)
        // x  a
        // x  z
        if (row < rows - 2 && col > 0 &&
            _tiles[row + 1, col - 1].Type == currType && _tiles[row + 2, col - 1].Type == currType)
        {
            return true;
        }

        if (row < rows - 2 && col < columns - 1 &&
            _tiles[row + 1, col + 1].Type == currType && _tiles[row + 2, col + 1].Type == currType)
        {
            return true;
        }

        if (row > 1 && col < columns - 1 &&
            _tiles[row - 1, col + 1].Type == currType && _tiles[row - 2, col + 1].Type == currType)
        {
            return true;
        }

        if (row > 1 && col > 0 &&
            _tiles[row - 1, col - 1].Type == currType && _tiles[row - 2, col - 1].Type == currType)
        {
            return true;
        }

        return false;
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

                    _matchedTiles.Enqueue(new List<Tile>(matchedTiles.Skip(matchedTiles.Count - 3)));
                }
                else if (counter > 3)
                {
                    matchedTiles.Add(_tiles[row, col]);
                    _tiles[row, col].ToBeDestroyed = true;

                    _matchedTiles.Peek().Add(_tiles[row, col]);
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

                    _matchedTiles.Enqueue(new List<Tile>(matchedTiles.Skip(matchedTiles.Count - 3)));
                }
                else if (counter > 3)
                {
                    matchedTiles.Add(_tiles[row, col]);
                    _tiles[row, col].ToBeDestroyed = true;
                    _matchedTiles.Peek().Add(_tiles[row, col]);
                }
            }
        }

        return matchedTiles;
    }

    private void AddUpPoints()
    {
        while (_matchedTiles.Count > 0)
        {
            List<Tile> currMatch = _matchedTiles.Dequeue();
            int pointsToGive = (currMatch.Count - 2) * _threeMatchPoints * _multiplier;

            float secondsToAdd = (_threeMatchSeconds + currMatch.Count - 3) / _multiplier;
            timer.AddSeconds(secondsToAdd);

            Vector3 pointsTextPos = Vector3.Lerp(
                currMatch[0].transform.position, currMatch[currMatch.Count - 1].transform.position, 0.5f);

            GameObject pointsText = new GameObject(pointsToGive.ToString());
            pointsText.transform.SetParent(_canvas.transform);
            pointsText.transform.position = pointsTextPos + Vector3.back;
            pointsText.AddComponent<Points>().Init(pointsToGive.ToString());

            _points += pointsToGive;
            _multiplier++;
        }
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

    private void ShakeTiles()
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                _tiles[row, col].StartShaking(Mathf.Min(_tileHeight, _tileWidth) / 2);
            }
        }
    }

    private void StopShakingTiles()
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                _tiles[row, col].StopShaking();
            }
        }
    }

    private void GoToNextLevel()
    {
        SetLevel(_level + 1);
        _threeMatchPoints += 5;
        _threeMatchSeconds = Mathf.Max(_threeMatchSeconds - 0.25f, 1f);
        timer.ResetTimeLeft();
        timer.Resume();
    }

    private void GameOver()
    {
        _state = BoardState.GameOver;
        timesUpField.SetActive(true);
        ShakeTiles();
    }
}