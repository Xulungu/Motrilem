using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public GameObject player;
    public float timeOffset;
    public Vector3 posOffset;

    [Header("Regarder en haut / en bas")]
    [Tooltip("Décalage vertical de la caméra quand le joueur regarde en haut ou en bas")]
    public float lookDistance = 4f;
    [Tooltip("Vitesse à laquelle la caméra se décale (unités par seconde)")]
    public float lookSpeed = 9f;
    private float lookTarget, look;

    [Header("Cinématiques")]
    public float zoomSpeed = 2f;

    private Vector3 velocity;
    private Vector3 smoothedPosition;
    private bool hasFocus;
    private Vector3 focusPoint;

    private Camera cam;
    private float baseSize;
    private float targetSize;

    private float shakeIntensity;
    private float shakeTime;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam != null) baseSize = targetSize = cam.orthographicSize;
        smoothedPosition = transform.position;
    }

    // LateUpdate : la caméra bouge après le joueur, ce qui évite les tremblements
    void LateUpdate()
    {
        Vector3 target;
        if (hasFocus) target = new Vector3(focusPoint.x, focusPoint.y, posOffset.z);
        else if (player != null) target = player.transform.position + posOffset;
        else return;

        // Le joueur regarde en haut / en bas : la caméra glisse dans cette direction
        look = Mathf.MoveTowards(look, hasFocus ? 0f : lookTarget * lookDistance, Time.deltaTime * lookSpeed);
        target.y += look;

        smoothedPosition = Vector3.SmoothDamp(smoothedPosition, target, ref velocity, hasFocus ? timeOffset * 2.5f : timeOffset);

        Vector3 shake = Vector3.zero;
        if (shakeTime > 0f)
        {
            shakeTime -= Time.deltaTime;
            shake = (Vector3)(Random.insideUnitCircle * shakeIntensity);
        }
        transform.position = smoothedPosition + shake;

        if (cam != null)
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, Time.deltaTime * zoomSpeed);
    }

    /// <summary>Place immédiatement la caméra sur le joueur (après un chargement, une téléportation...).</summary>
    public void SnapToTarget()
    {
        if (player == null) return;
        smoothedPosition = player.transform.position + posOffset;
        velocity = Vector3.zero;
        transform.position = smoothedPosition;
    }

    // ------------------------------------------------------------------ cinématiques
    /// <summary>La caméra regarde un point précis au lieu du joueur.</summary>
    public void SetFocus(Vector3 point)
    {
        hasFocus = true;
        focusPoint = point;
    }

    public void ClearFocus() { hasFocus = false; }

    /// <summary>1 = regarder en haut, -1 = en bas, 0 = revenir sur le joueur.</summary>
    public void SetLook(float direction) { lookTarget = Mathf.Clamp(direction, -1f, 1f); }

    public void SetZoom(float orthographicSize) { targetSize = orthographicSize; }

    public void ResetZoom() { targetSize = baseSize; }

    /// <summary>Fait trembler la caméra (intensité en unités, durée en secondes).</summary>
    public void Shake(float intensity, float duration)
    {
        if (shakeTime <= 0f || intensity >= shakeIntensity) shakeIntensity = intensity;
        shakeTime = Mathf.Max(shakeTime, duration);
    }
}
