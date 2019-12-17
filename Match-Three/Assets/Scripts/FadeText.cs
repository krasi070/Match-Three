using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class FadeText : MonoBehaviour
{
    public float fadeSpeed;

    private Text _text;

    private void Awake()
    {
        _text = GetComponent<Text>();
    }

    private void OnEnable()
    {
        StartCoroutine(Fade());
    }

    private IEnumerator Fade()
    {
        while (true)
        {
            float alpha = Mathf.Abs(Mathf.Sin(Time.time * fadeSpeed));
            _text.color = new Color(_text.color.r, _text.color.g, _text.color.b, alpha);

            yield return null;
        }
    }
}