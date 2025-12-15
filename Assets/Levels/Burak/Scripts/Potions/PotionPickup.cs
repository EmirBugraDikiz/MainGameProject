using UnityEngine;

public enum PotionType
{
    DoubleJump,
    Kronos
}

public class PotionPickup : MonoBehaviour
{
    [Header("Settings")]
    public PotionType potionType;
    public float destroyDelay = 0.1f;

    private bool isCollected = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        if (other.CompareTag("Player"))
        {
            PlayerAbilitiesController abilities = other.GetComponent<PlayerAbilitiesController>();

            if (abilities != null)
            {
                isCollected = true;

                switch (potionType)
                {
                    case PotionType.DoubleJump:
                        abilities.GrantDoubleJump();
                        break;

                    case PotionType.Kronos:
                        abilities.GrantKronos();
                        break;
                }

                Destroy(gameObject, destroyDelay);
            }
        }
    }
}
