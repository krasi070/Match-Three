using System;
using System.Collections;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public event Action<Tile> OnMouseHover;
    public event Action<Tile> OnMouseClick;
    public event Action<Tile> OnMouseExitHover;
    public event Action AfterSwap;

    private TileType _type;

    public Vector2Int Position { get; set; }

    public TileType Type
    {
        get
        {
            return _type;
        }
        set
        {
            name = value.ToString();

            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            renderer.sprite = Resources.Load<Sprite>($"Sprites/{value.ToString().ToLower()}");

            _type = value;
        }
    }

    public void Init(Vector2Int position, TileType type)
    {
        Position = position;
        Type = type;
    }

    public void MoveToPosition(Vector3 target, float duration)
    {
        StartCoroutine(Move(target, duration));
    }

    private IEnumerator Move(Vector3 target, float duration)
    {
        float currTime = 0f;

        while ((transform.position - target).sqrMagnitude != 0)
        {
            currTime += Time.deltaTime;
            float interpolant = currTime / duration;
            transform.position = Vector3.Lerp(transform.position, target, interpolant);

            yield return null;
        }

        transform.position = new Vector3(transform.position.x, transform.position.y, 0f);
        AfterSwap?.Invoke();
    }

    private void OnMouseDown()
    {
        OnMouseClick?.Invoke(this);
    }

    private void OnMouseOver()
    {
        OnMouseHover?.Invoke(this);
    }

    private void OnMouseExit()
    {
        OnMouseExitHover?.Invoke(this);
    }
}