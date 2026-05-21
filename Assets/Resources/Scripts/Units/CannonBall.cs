using UnityEngine;

public class CannonBall : MonoBehaviour
{
    private Vector3 startPosition;
    private Vector3 endPosition;
    private float speed;
    private System.Action<Vector3> onArrival; // passe la position d'impact
    private bool arrived = false;
    private float progress = 0f;
    [SerializeField] private float arcHeight = 3f;
    private Transform target;
    private Transform launcher;

    public static CannonBall Spawn(GameObject prefab, Vector3 from, Transform target, float speed, System.Action<Vector3> onArrival, Transform launcher = null)
    {
        GameObject go = Instantiate(prefab, from, Quaternion.identity);
        CannonBall ball = go.GetComponent<CannonBall>();
        if (ball == null)
            ball = go.AddComponent<CannonBall>();

        ball.startPosition = from;
        ball.endPosition = target.position; // position figée au moment du tir
        ball.speed = speed;
        ball.onArrival = onArrival;
        ball.target = target;
        ball.launcher = launcher;

        return ball;
    }

    void Update()
    {
        if (arrived) return;

        if (launcher == null)
        {
            Destroy(gameObject);
            return;
        }

        if (IsTargetDead())
        {
            Destroy(gameObject);
            return;
        }

        float distance = Vector3.Distance(startPosition, endPosition);
        progress += speed / distance * Time.deltaTime;
        progress = Mathf.Clamp01(progress);

        Vector3 linearPos = Vector3.Lerp(startPosition, endPosition, progress);
        float height = Mathf.Sin(progress * Mathf.PI) * arcHeight;
        Vector3 newPos = new Vector3(linearPos.x, linearPos.y + height, linearPos.z);

        if (newPos != transform.position)
            transform.LookAt(newPos + (newPos - transform.position));

        transform.position = newPos;

        if (progress >= 1f)
        {
            arrived = true;
            onArrival?.Invoke(endPosition);
            Destroy(gameObject);
        }
    }

    private bool IsTargetDead()
    {
        if (target == null)
            return true;

        UnitInstance unit = target.GetComponent<UnitInstance>();
        if (unit != null)
            return unit.currentHealth <= 0;

        StructureInstance structure = target.GetComponent<StructureInstance>();
        if (structure != null)
            return structure.currentHealth <= 0;

        return false;
    }
}