using UnityEngine;

public class SlidePlatform : MonoBehaviour
{
    public Transform targetPos;   // Platformun kayacağı hedef nokta (sağa kaymış hali)
    public float moveTime = 0.3f; // Kaç saniyede kayacak (hızlı olsun ki troll olsun)

    bool isMoving = false;
    Vector3 startPos;
    float t = 0f;

    void Start()
    {
        startPos = transform.position;
    }

    public void Activate()
    {
        if (isMoving) return;
        isMoving = true;
        t = 0f;
    }

    void Update()
    {
        if (!isMoving) return;

        t += Time.deltaTime / moveTime;
        transform.position = Vector3.Lerp(startPos, targetPos.position, t);

        if (t >= 1f)
        {
            isMoving = false;
        }
    }
}
