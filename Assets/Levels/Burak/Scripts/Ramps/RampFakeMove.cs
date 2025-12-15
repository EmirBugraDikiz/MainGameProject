using UnityEngine;

public class RampFakeMove : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Hareket edecek rampa objesi (boş bırakılırsa bu objenin kendisi kullanılır).")]
    public Transform ramp;

    [Header("Movement Settings")]
    [Tooltip("Rampanın dünya uzayındaki hedef Z pozisyonu.")]
    public float targetWorldZ = -1.6f;

    [Tooltip("Saniyedeki hareket hızı.")]
    public float moveSpeed = 10f;

    [Tooltip("Oyuncu tetikleyince sadece bir kez mi hareket etsin?")]
    public bool moveOnlyOnce = true;

    private Vector3 targetPos;
    private bool shouldMove = false;
    private bool alreadyMoved = false;

    void Start()
    {
        if (ramp == null)
            ramp = transform;

        // X ve Y aynı kalsın, sadece dünyanın Z'si değişsin
        Vector3 startPos = ramp.position;
        targetPos = new Vector3(startPos.x, startPos.y, targetWorldZ);
    }

    void Update()
    {
        if (!shouldMove) return;

        ramp.position = Vector3.MoveTowards(
            ramp.position,
            targetPos,
            moveSpeed * Time.deltaTime
        );

        // Hedefe ulaştıysa dur
        if ((ramp.position - targetPos).sqrMagnitude < 0.0001f)
        {
            ramp.position = targetPos;
            shouldMove = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (moveOnlyOnce && alreadyMoved)
            return;

        alreadyMoved = true;
        shouldMove = true;
    }
}
