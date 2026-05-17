using UnityEngine;
using System.Collections;

public class CannonBall : MonoBehaviour
{
    private Vector3 startPosition;
    private Vector3 endPosition;
    private float speed;
    private System.Action onArrival;
    private bool arrived = false;
    private float progress = 0f;

    [SerializeField] private float arcHeight = 3f; // hauteur de la cloche

    public static CannonBall Spawn(GameObject prefab, Vector3 from, Transform target, float speed, System.Action onArrival)
    {
        GameObject go = Instantiate(prefab, from, Quaternion.identity);
        CannonBall ball = go.GetComponent<CannonBall>();
        if (ball == null)
            ball = go.AddComponent<CannonBall>();

        ball.startPosition = from;
        ball.endPosition = target.position;
        ball.speed = speed;
        ball.onArrival = onArrival;

        return ball;
    }

    void Update()
    {
        if (arrived) return;

        // Avance le progress de 0 à 1
        float distance = Vector3.Distance(startPosition, endPosition);
        progress += (speed / distance) * Time.deltaTime;
        progress = Mathf.Clamp01(progress);

        // Position linéaire entre start et end
        Vector3 linearPos = Vector3.Lerp(startPosition, endPosition, progress);

        // Hauteur parabolique : sin(progress * PI) donne une cloche entre 0 et 1
        float height = Mathf.Sin(progress * Mathf.PI) * arcHeight;

        Vector3 newPos = new Vector3(linearPos.x, linearPos.y + height, linearPos.z);

        // Orientation vers la prochaine position
        if (newPos != transform.position)
            transform.LookAt(newPos + (newPos - transform.position));

        transform.position = newPos;

        if (progress >= 1f)
        {
            arrived = true;
            onArrival?.Invoke();
            Destroy(gameObject);
        }
    }
}