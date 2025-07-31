using UnityEngine;

public class FogController : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] float minFogDensity = 0.01f;
    [SerializeField] float maxFogDensity = 0.05f;
    [SerializeField] float heightScale = 100f;
    [SerializeField] Color fogColor = new Color(0.65f, 0.75f, 0.85f);

    void Start()
    {
        RenderSettings.fog = true;
        RenderSettings.fogColor = fogColor;
    }

    void Update()
    {
        if (player == null) return;

        float t = Mathf.Clamp01(player.position.y / heightScale);
        RenderSettings.fogDensity = Mathf.Lerp(minFogDensity, maxFogDensity, t);
    }
}