using System;
using System.Collections;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public event Action<Tile> OnMouseHover;
    public event Action<Tile> OnMouseClick;
    public event Action<Tile> OnMouseRelease;
    public event Action<Tile> OnMouseExitHover;
    public event Action AfterMove;
    public event Action AfterDisappear;

    private TileType _type;

    public bool ToBeDestroyed { get; set; }

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

    public void Disappear()
    {
        StartCoroutine(Disappear(0.35f));
    }

    private IEnumerator Move(Vector3 target, float duration)
    {
        float currTime = 0f;
        Vector3 startPosition = transform.position;

        while ((transform.position - target).sqrMagnitude != 0)
        {
            currTime += Time.deltaTime;
            float interpolant = currTime / duration;
            transform.position = Vector3.Lerp(startPosition, target, interpolant);

            yield return null;
        }

        transform.position = new Vector3(transform.position.x, transform.position.y);
        AfterMove?.Invoke();
    }

    private IEnumerator Disappear(float duration)
    {
        float timer = duration;

        while (transform.localScale.x > 0.01f)
        {
            timer -= Time.deltaTime;
            float newScale = timer / duration;
            transform.localScale = new Vector3(newScale, newScale, newScale);

            yield return null;
        }

        AfterDisappear?.Invoke();
        Destroy(gameObject);
    }

    private void OnMouseDown()
    {
        OnMouseClick?.Invoke(this);
    }

    private void OnMouseUp()
    {
        OnMouseRelease?.Invoke(this);
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