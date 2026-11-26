using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject pausePanel;

    void Start()
    {
        Time.timeScale = 1f;
        pausePanel.SetActive(false);
    }

    public void PauseGame()
    {
        pausePanel.SetActive(true);
        Time.timeScale = 0f;
        MysticAudio.SetPaused(true);
    }

    public void ResumeGame()
    {
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
        MysticAudio.SetPaused(false);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        MysticAudio.SetPaused(false);
        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        MysticAudio.SetPaused(false);
        SceneManager.LoadScene("MainMenu");
    }
}
