using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour
{
    public bool timer = false; // Flag to start or stop the timer
    public TextMeshProUGUI timerTxt; // UI element for displaying elapsed time

    private float elapsedTime; // Tracks the elapsed time in seconds

    public static Timer instance; // Singleton instance of the Timer

    void Start()
    {
        // Ensure only one Timer instance exists in the scene
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this;
        }
    }

    void Update()
    {
        // Update the timer if it's running
        if (timer)
        {
            elapsedTime += Time.deltaTime; // Increment elapsed time

            // Calculate minutes and seconds
            int mins = Mathf.FloorToInt(elapsedTime / 60);
            int secs = Mathf.FloorToInt(elapsedTime % 60);

            // Update the UI with the formatted time
            timerTxt.text = $"Time: {mins:00}:{secs:00}";
        }
    }

    // Public method to get the total elapsed time
    public float getElapsedTime()
    {
        return elapsedTime;
    }
}