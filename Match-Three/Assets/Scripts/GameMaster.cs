using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GameMaster : MonoBehaviour
{
    public float timeToExit;

    private Image _fadeOutImage;

    private void Start()
    {
        _fadeOutImage = GetComponentInChildren<Image>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            StartCoroutine(Exiting());
        }
    }

    private IEnumerator Exiting()
    {
        float timer = 0f;

        while (Input.GetKey(KeyCode.Escape))
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, timer / timeToExit);
            _fadeOutImage.color = new Color(_fadeOutImage.color.r, _fadeOutImage.color.g, _fadeOutImage.color.b, alpha);

            if (timer >= timeToExit)
            {
                Application.Quit();
            }

            yield return null;
        }

        _fadeOutImage.color = new Color(_fadeOutImage.color.r, _fadeOutImage.color.g, _fadeOutImage.color.b, 0f);
    }
}