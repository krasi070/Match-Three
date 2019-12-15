using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Points : MonoBehaviour
{
    private float _moveUpDuration = 0.5f;
    private float _fadeDuration = 0.1f;

    private void Awake()
    {
        StartCoroutine(MoveUp());
    }

    public void Init(string text)
    {
        Text pointsField = gameObject.AddComponent<Text>();
        pointsField.font = Resources.Load<Font>("Fonts/HVD_Comic_Serif_Pro");
        pointsField.fontSize = 1;
        pointsField.color = new Color32(50, 50, 50, 255);
        pointsField.alignment = TextAnchor.MiddleCenter;
        pointsField.horizontalOverflow = HorizontalWrapMode.Overflow;
        pointsField.verticalOverflow = VerticalWrapMode.Overflow;
        pointsField.text = text;

        Shadow pointsShadow = gameObject.AddComponent<Shadow>();
        pointsShadow.effectDistance = new Vector2(0.05f, -0.05f);
        pointsShadow.effectColor = new Color32(244, 244, 244, 255);
    }

    private IEnumerator MoveUp()
    {
        float timer = 0;
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + (Vector3.up / 2);

        while (timer < _moveUpDuration)
        {
            timer += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, endPos, timer / _moveUpDuration);

            yield return null;
        }

        StartCoroutine(Fade());
    }

    private IEnumerator Fade()
    {
        float timer = 0;
        Text textField = GetComponent<Text>();

        while (timer < _fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timer / _fadeDuration);
            textField.color = new Color(textField.color.r, textField.color.g, textField.color.b, alpha);

            yield return null;
        }

        Destroy(gameObject);
    }
}