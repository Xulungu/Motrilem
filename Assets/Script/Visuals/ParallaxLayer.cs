using UnityEngine;

// Couche de décor qui suit la caméra plus ou moins vite (effet de profondeur).
// factor = 1 : la couche reste collée à la caméra (ciel lointain) ; 0 : elle ne bouge pas.
[DefaultExecutionOrder(100)]
public class ParallaxLayer : MonoBehaviour
{
    public float factorX = 0.5f;
    public float factorY = 0.3f;
    [Tooltip("La couche reste centrée verticalement sur la caméra (on ne voit jamais son bord)")]
    public bool followCameraY = false;
    public float offsetY = 0f;

    private Transform cam;
    private Vector3 startPos;
    private Vector3 camStart;

    void Start()
    {
        if (Camera.main != null) cam = Camera.main.transform;
        startPos = transform.position;
        if (cam != null) camStart = cam.position;
    }

    void LateUpdate()
    {
        if (cam == null) return;
        Vector3 d = cam.position - camStart;
        float y = followCameraY ? cam.position.y + offsetY : startPos.y + d.y * factorY;
        transform.position = new Vector3(startPos.x + d.x * factorX, y, startPos.z);
    }
}
