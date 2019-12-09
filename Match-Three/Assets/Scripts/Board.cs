using UnityEngine;

public class Board : MonoBehaviour
{
    public int rows;
    public int columns;

    private TileType[] _types = new TileType[]
    {
        TileType.Apple,
        TileType.Broccoli,
        TileType.Coconut,
        TileType.Loaf,
        TileType.MilkCarton,
        TileType.Orange
    };

    private void Start()
    {
        InitTiles();
    }

    private void InitTiles()
    {
        float height = 1f;
        float width = 1f;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                TileType randomType = _types[Random.Range(0, _types.Length)];
                GameObject tile = new GameObject(randomType.ToString().ToLower());

                SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
                renderer.sprite = Resources.Load<Sprite>($"Sprites/{randomType.ToString().ToLower()}");

                height = renderer.bounds.size.y;
                width = renderer.bounds.size.x;

                float startX = -width * (rows - 1) / 2;
                float startY = -height * (columns - 1) / 2;
                tile.transform.position = new Vector3(startX + col * width, startY + row * height, 0);

                tile.transform.parent = transform;
            }
        }

        Camera.main.orthographicSize = rows * (height / 2) * 1.1f;
    }
}