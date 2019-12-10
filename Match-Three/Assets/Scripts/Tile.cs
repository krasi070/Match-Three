using System;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public event Action<Tile> OnMouseClick;

    public Vector2Int Coordinates { get; set; }

    private const string selectEffectName = "SelectEffect";
    private const string hoverEffectName = "HoverEffect";

    private void OnMouseDown()
    {
        if (transform.childCount > 0)
        {
            GameObject child = transform.GetChild(0).gameObject;
            child.name = selectEffectName;

            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
            renderer.color = Color.white;
        }

        OnMouseClick?.Invoke(this);
    }

    private void OnMouseEnter()
    {
        if (transform.childCount == 0)
        {
            GameObject hoverEffect = new GameObject(hoverEffectName);
            SpriteRenderer renderer = hoverEffect.AddComponent<SpriteRenderer>();
            renderer.sprite = Resources.Load<Sprite>("Sprites/select");
            renderer.color = new Color(1, 1, 1, 0.5f);

            hoverEffect.transform.position = transform.position + Vector3.forward;
            hoverEffect.transform.parent = transform;
        }
    }

    private void OnMouseExit()
    {
        if (transform.childCount > 0 && transform.GetChild(0).name == hoverEffectName)
        {
            Destroy(transform.GetChild(0).gameObject);
        }
    }
}