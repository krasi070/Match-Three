using UnityEngine;

public class Tile : MonoBehaviour
{
    public Vector2Int Coordinates { get; set; }

    private const string hoverEffectName = "HoverEffect";

    private void OnMouseOver()
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
        if (transform.GetChild(0).name == hoverEffectName)
        {
            Destroy(transform.GetChild(0).gameObject);
        }
    }
}