using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class ScoreTracker : MonoBehaviour
{
    private Text _scoreField;

    private void Start()
    {
        _scoreField = GetComponent<Text>();
    }

    public void UpdateScore(int score)
    {
        _scoreField.text = $"Score: {score}";
    }
}