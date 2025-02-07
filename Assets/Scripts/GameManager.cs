using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public GameObject obstacles;

    public void LoadTrack1FSM()
    {
        SceneManager.LoadScene(1);
    }
    
    public void LoadTrack1Fuzzy()
    {
        SceneManager.LoadScene(2);
    }
    
    public void LoadTrack2FSM()
    {
        SceneManager.LoadScene(3);
    }
    
    public void LoadTrack2Fuzzy()
    {
        SceneManager.LoadScene(4);
    }
    
    public void LoadTrack3FSM()
    {
        SceneManager.LoadScene(5);
    }
    
    public void LoadTrack3Fuzzy()
    {
        SceneManager.LoadScene(6);
    }
    
    public void LoadMainMenu()
    {
        SceneManager.LoadScene(0);
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void Obstacles()
    {
        obstacles.SetActive(!obstacles.activeInHierarchy);
    }
}
