using UnityEngine;
using UnityEngine.SceneManagement;

public class ReplayIntro : MonoBehaviour
{
    public void Replay()
    {
        PlayerPrefs.SetInt("IntroSeen", 0);
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
