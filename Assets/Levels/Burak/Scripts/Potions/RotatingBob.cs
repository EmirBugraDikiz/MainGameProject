using UnityEngine;

public class RotatingBob : MonoBehaviour
{
    public float rotateSpeed = 50f;
    public float bobAmount = 0.15f;
    public float bobSpeed = 2f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        // Dünya ekseninde dön
        transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime, Space.World);

        // Yukarı-aşağı sallanma
        float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobAmount;
        transform.localPosition = new Vector3(startPos.x, newY, startPos.z);
    }
}
