using UnityEngine;
using System.Collections;

public class CannonBall : MonoBehaviour
{
    private Transform target;
    private float speed;
    private System.Action onArrival;
    private bool arrived = false;

    public static CannonBall Spawn(GameObject prefab, Vector3 from, Transform target, float speed, System.Action onArrival)
    {
        GameObject go = Instantiate(prefab, from, Quaternion.identity);

        // Récupère le composant existant OU en ajoute un, jamais les deux
        CannonBall ball = go.GetComponent<CannonBall>();
        if (ball == null)
            ball = go.AddComponent<CannonBall>();

        ball.target = target;
        ball.speed = speed;
        ball.onArrival = onArrival;

        return ball;
    }

    void Update()
    {
        if (arrived) return;

        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 destination = target.position;
        transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
        transform.LookAt(destination);

        float dist = Vector3.Distance(transform.position, destination);
        if (dist < 0.3f)
        {
            arrived = true;       // bloque le Update immédiatement
            onArrival?.Invoke();  // dégâts appliqués une seule fois
            Destroy(gameObject);
        }
    }
}