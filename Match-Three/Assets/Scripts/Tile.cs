﻿using System.Collections;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public event System.Action<Tile> OnMouseHover;
    public event System.Action<Tile> OnMouseClick;
    public event System.Action<Tile> OnMouseRelease;
    public event System.Action<Tile> OnMouseExitHover;
    public event System.Action AfterMove;
    public event System.Action AfterDisappear;
    public event System.Action AfterAppear;

    private bool _shaking;
    private TileType _type;

    public bool Selected { get; set; }

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

    public bool Equals(Tile tile)
    {
        return (Position - tile.Position).sqrMagnitude == 0;
    }

    public bool IsAdjecentTo(Tile tile)
    {
        return (Position - tile.Position).sqrMagnitude == 1;
    }

    public void MoveToPosition(Vector3 target, float duration)
    {
        StartCoroutine(Move(target, duration));
    }

    public void Appear()
    {
        StartCoroutine(Appear(0.35f));
    }

    public void Disappear(bool destroy)
    {
        StartCoroutine(Disappear(0.35f, destroy));
    }

    public void StartShaking(float amount)
    {
        _shaking = true;
        StartCoroutine(Shake(amount));
    }

    public void StopShaking()
    {
        _shaking = false;
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

    private IEnumerator Appear(float duration)
    {
        float timer = 0;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, timer / duration);

            yield return null;
        }

        AfterAppear?.Invoke();
    }

    private IEnumerator Disappear(float duration, bool destroy)
    {
        float timer = duration;

        while (timer > 0)
        {
            timer -= Time.deltaTime;
            transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, timer / duration);

            yield return null;
        }

        if (destroy)
        {
            AfterDisappear?.Invoke();
            Destroy(gameObject);
        }
    }

    private IEnumerator Shake(float amount)
    {
        float speed = 3f;
        Vector3 startingPos = transform.position;

        while (_shaking)
        {
            float x = startingPos.x + Time.deltaTime * speed * amount * Random.Range(-1, 2);
            float y = startingPos.y + Time.deltaTime * speed * amount * Random.Range(-1, 2);
            transform.position = new Vector3(x, y);

            yield return null;
        }

        transform.position = startingPos;
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