using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GameMaster : MonoBehaviour
{
    public GameObject titleImage;
    public GameObject instructionsText;
    public GameObject board;
    public GameObject ui;

    public float timeToExit;
    public Image fadeOutImage;

    private enum GameState
    {
        InTitleScreen,
        InPlay
    }

    private GameState _state;

    private void Start()
    {
        _state = GameState.InTitleScreen;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && _state == GameState.InTitleScreen)
        {
            titleImage.SetActive(false);
            instructionsText.SetActive(false);
            board.SetActive(true);
            ui.SetActive(true);
        }

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
            fadeOutImage.color = new Color(fadeOutImage.color.r, fadeOutImage.color.g, fadeOutImage.color.b, alpha);

            if (timer >= timeToExit)
            {
                Application.Quit();
            }

            yield return null;
        }

        fadeOutImage.color = new Color(fadeOutImage.color.r, fadeOutImage.color.g, fadeOutImage.color.b, 0f);
    }
}