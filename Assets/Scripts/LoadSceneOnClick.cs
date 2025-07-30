using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneOnClick : MonoBehaviour
{
    [SerializeField] string sceneName = "Start";
    bool loading;

    void OnMouseUpAsButton()
    {
        if (loading || SceneManager.GetActiveScene().name == sceneName)
            return;

        Time.timeScale = 1f;                   // make sure we’re not paused
        loading = true;
        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
    }
}
