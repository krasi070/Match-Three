using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class Timer : MonoBehaviour
{
    public float maxTime;

    public event System.Action ReachedZero;
    public event System.Action ReachedMax;

    private Slider _slider;
    private float _timeLeft;
    private bool _paused;

    private void Start()
    {
        _slider = GetComponent<Slider>();

        ResetTimeLeft();
        Resume();
    }

    /// <summary>
    /// Add seconds to the timer. Fractions of seconds are accepted.
    /// </summary>
    /// <param name="seconds">The seconds.</param>
    public void AddSeconds(float seconds)
    {
        _timeLeft += seconds;
    }

    /// <summary>
    /// Pauses the timer.
    /// </summary>
    public void Pause()
    {
        _paused = true;
    }
    
    /// <summary>
    /// Starts or resumes the timer.
    /// </summary>
    public void Resume()
    {
        _paused = false;
        StartCoroutine(StartTimer());
    }

    /// <summary>
    /// Reset the timer.
    /// </summary>
    public void ResetTimeLeft()
    {
        _timeLeft = maxTime / 2;
    }

    private IEnumerator StartTimer()
    {
        while (_timeLeft > 0 && _timeLeft < maxTime && !_paused)
        {
            _timeLeft -= Time.deltaTime;
            _slider.value = Mathf.Lerp(0f, 1f, _timeLeft / maxTime);

            yield return null;
        }

        if (_timeLeft <= 0f)
        {
            ReachedZero?.Invoke();
        }
        else if (_timeLeft >= maxTime)
        {
            ReachedMax?.Invoke();
        }
    }
}