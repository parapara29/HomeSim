using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Loads a Unity scene when this object's collider is clicked or tapped.
/// </summary>
public class LoadSceneOnClick : MonoBehaviour
{
    [SerializeField] string sceneName = "Start";

    void OnMouseUpAsButton()
    {
        SceneManager.LoadScene(sceneName);
    }
}
