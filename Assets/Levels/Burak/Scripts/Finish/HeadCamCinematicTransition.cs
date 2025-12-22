using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class HeadCamCinematicTransitionAAA : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Child camera. Boş bırakılırsa childlardan bulur.")]
    public Camera targetCamera;

    [Tooltip("Collision için kamera pozisyonunu hesaplarken referans alınacak origin (genelde HeadCamPivot).")]
    public Transform collisionOrigin;

    [Header("Timings")]
    public float totalAnimDuration = 6.19f;
    public float transitionDuration = 1.20f;

    [Header("Pivot Local Start (HeadCamPivot)")]
    public Vector3 startLocalPos = new Vector3(-1.916904f, -2.167924f, -0.02589143f);
    public Vector3 startLocalRotEuler = new Vector3(-57.012f, 177.066f, 9.34f);

    [Header("Pivot Local End (HeadCamPivot)")]
    public Vector3 endLocalPos = new Vector3(2.678758f, -2.005471f, 3.904014f);
    public Vector3 endLocalRotEuler = new Vector3(-67.527f, 78.962f, -55.315f);

    [Header("Mouth-safe Offset (fix the 'inside mouth' issue)")]
    [Tooltip("Başlangıçta ağza girmesin diye küçük local offset. Genelde Z + veririz.")]
    public Vector3 startSafetyOffset = new Vector3(0f, 0.06f, 0.12f);

    [Tooltip("Bitişte de istersen ufak offset.")]
    public Vector3 endSafetyOffset = Vector3.zero;

    [Header("Cinematic Motion")]
    [Range(0.5f, 2.5f)]
    public float overshoot = 1.15f;

    [Tooltip("Pozisyonda mikro elde kamera hissi")]
    public float posNoiseAmount = 0.012f;

    [Tooltip("Rotasyonda mikro elde kamera hissi (derece)")]
    public float rotNoiseAmount = 0.30f;

    [Tooltip("Noise frekansı")]
    public float noiseFrequency = 2.2f;

    [Header("Smoothing (AAA feel)")]
    [Tooltip("Pozisyon yumuşatma. Küçük değer daha ağır sinema.")]
    public float posSmoothTime = 0.06f;

    [Tooltip("Rotasyon yumuşatma. Küçük değer daha ağır sinema.")]
    public float rotSmoothTime = 0.06f;

    [Header("Collision")]
    [Tooltip("Collision layer mask (Character + Environment). Default: Everything")]
    public LayerMask collisionMask = ~0;

    [Tooltip("Kamera collision yarıçapı (küçük küre). 0.12-0.18 güzel.")]
    public float collisionRadius = 0.14f;

    [Tooltip("Duvara/meshe sıfır yapışmasın")]
    public float collisionPadding = 0.03f;

    [Tooltip("Collision çözümü için max itme mesafesi")]
    public float maxCollisionPush = 1.0f;

    [Tooltip("Kamera kendi karakter colliderına çarpmasın istiyorsan true yapıp ignore list kullan.")]
    public bool ignoreSelfColliders = false;

    [Tooltip("ignoreSelfColliders true ise, karakter root (mixamorig:Hips) ver.")]
    public Transform selfRoot;

    [Header("FOV (Cinematic Punch)")]
    public bool animateFOV = true;
    public float baseFOV = 60f;
    public float fovDuringMove = 52f;     // hafif zoom-in sinema
    public float fovEnd = 58f;            // finalde biraz geri aç

    [Header("Play/Stop Safety")]
    public bool forceStartOnPlay = true;

    Vector3 _initialLocalPos;
    Quaternion _initialLocalRot;
    Vector3 _posVel;
    float _rotVel;

    Collider[] _selfColliders;

    void Awake()
    {
        _initialLocalPos = transform.localPosition;
        _initialLocalRot = transform.localRotation;

        if (!targetCamera) targetCamera = GetComponentInChildren<Camera>(true);
        if (!collisionOrigin) collisionOrigin = transform;

        if (targetCamera && animateFOV)
            targetCamera.fieldOfView = baseFOV;

        if (ignoreSelfColliders && selfRoot)
            _selfColliders = selfRoot.GetComponentsInChildren<Collider>(true);
    }

    void OnEnable()
    {
        if (forceStartOnPlay)
        {
            transform.localPosition = startLocalPos + startSafetyOffset;
            transform.localRotation = Quaternion.Euler(startLocalRotEuler);
            _posVel = Vector3.zero;
            _rotVel = 0f;

            if (targetCamera && animateFOV)
                targetCamera.fieldOfView = baseFOV;
        }

        StartCoroutine(Run());
    }

    void OnDisable()
    {
        // editorde poz sapıtmasın
        transform.localPosition = _initialLocalPos;
        transform.localRotation = _initialLocalRot;
    }

    IEnumerator Run()
    {
        float startTransitionAt = Mathf.Max(0f, totalAnimDuration - transitionDuration);
        yield return new WaitForSeconds(startTransitionAt);

        Vector3 fromPos = startLocalPos + startSafetyOffset;
        Quaternion fromRot = Quaternion.Euler(startLocalRotEuler);

        Vector3 toPos = endLocalPos + endSafetyOffset;
        Quaternion toRot = Quaternion.Euler(endLocalRotEuler);

        float t = 0f;
        float seed = Random.Range(0f, 9999f);

        // smoothing state
        Vector3 smoothPos = transform.localPosition;
        Quaternion smoothRot = transform.localRotation;

        while (t < transitionDuration)
        {
            float x = Mathf.Clamp01(t / transitionDuration);
            float eased = EaseInOutBack(x, overshoot);

            // base (unclamped for overshoot effect)
            Vector3 desiredLocalPos = Vector3.LerpUnclamped(fromPos, toPos, eased);
            Quaternion desiredLocalRot = Quaternion.SlerpUnclamped(fromRot, toRot, eased);

            // micro noise (cinematic handheld)
            float time = Time.time * noiseFrequency;
            float n1 = Mathf.PerlinNoise(seed, time) - 0.5f;
            float n2 = Mathf.PerlinNoise(seed + 17.7f, time * 1.07f) - 0.5f;
            float n3 = Mathf.PerlinNoise(seed + 55.5f, time * 1.13f) - 0.5f;

            desiredLocalPos += new Vector3(n1, n2, n3) * posNoiseAmount;
            desiredLocalRot = desiredLocalRot * Quaternion.Euler(new Vector3(n2, n3, n1) * rotNoiseAmount);

            // SmoothDamp position (AAA weight)
            smoothPos = Vector3.SmoothDamp(smoothPos, desiredLocalPos, ref _posVel, posSmoothTime);

            // Smooth rotation (damped)
            smoothRot = SmoothDampQuaternion(smoothRot, desiredLocalRot, ref _rotVel, rotSmoothTime);

            // Apply to pivot first
            transform.localPosition = smoothPos;
            transform.localRotation = smoothRot;

            // Collision solve on CAMERA (not pivot) to avoid entering mouth/mesh
            if (targetCamera)
            {
                ResolveCameraCollision();
            }

            // FOV punch
            if (targetCamera && animateFOV)
            {
                // hareket sırasında zoom-in, sona doğru açıl
                float fov = Mathf.Lerp(fovDuringMove, fovEnd, x);
                targetCamera.fieldOfView = Mathf.Lerp(baseFOV, fov, 0.85f);
            }

            t += Time.deltaTime;
            yield return null;
        }

        // Final snap (pivot)
        transform.localPosition = endLocalPos + endSafetyOffset;
        transform.localRotation = Quaternion.Euler(endLocalRotEuler);

        if (targetCamera)
        {
            ResolveCameraCollision();
            if (animateFOV)
                targetCamera.fieldOfView = fovEnd;
        }
    }

    void ResolveCameraCollision()
    {
        // Kamera child olduğu için local pozunu collision ile “geri çekiyoruz”
        // Pivot dünyada nerede -> kamera hedef dünya pozisyonu -> arada collider varsa kamerayı dışarı al

        Transform camT = targetCamera.transform;

        Vector3 origin = collisionOrigin.position;
        Vector3 targetPos = camT.position;

        Vector3 dir = (targetPos - origin);
        float dist = dir.magnitude;

        if (dist < 0.0001f) return;

        dir /= dist;

        // Self collider ignore (opsiyonel)
        bool shouldIgnore = ignoreSelfColliders && _selfColliders != null && _selfColliders.Length > 0;

        // SphereCast
        RaycastHit hit;
        bool blocked = Physics.SphereCast(origin, collisionRadius, dir, out hit, dist + collisionPadding, collisionMask, QueryTriggerInteraction.Ignore);

        if (blocked)
        {
            if (shouldIgnore && IsSelfCollider(hit.collider))
            {
                // self collider’a çarptıysa, onu sayma diye ikinci bir cast deneyelim (basit çözüm)
                // biraz origin’i ileri alıp tekrar cast
                Vector3 origin2 = origin + dir * 0.05f;
                blocked = Physics.SphereCast(origin2, collisionRadius, dir, out hit, dist + collisionPadding, collisionMask, QueryTriggerInteraction.Ignore);
                if (blocked && IsSelfCollider(hit.collider)) return;
            }

            // Kamerayı çarpma noktasının biraz önüne çek
            float safeDist = Mathf.Max(0f, hit.distance - collisionPadding);
            safeDist = Mathf.Clamp(safeDist, 0f, dist + maxCollisionPush);

            Vector3 safeWorldPos = origin + dir * safeDist;

            // World->Local çevirip kameranın local posunu güncelle
            Transform parent = camT.parent;
            if (parent)
            {
                Vector3 safeLocal = parent.InverseTransformPoint(safeWorldPos);
                camT.localPosition = Vector3.Lerp(camT.localPosition, safeLocal, 0.85f);
            }
            else
            {
                camT.position = Vector3.Lerp(camT.position, safeWorldPos, 0.85f);
            }
        }
    }

    bool IsSelfCollider(Collider c)
    {
        if (_selfColliders == null) return false;
        for (int i = 0; i < _selfColliders.Length; i++)
            if (_selfColliders[i] == c) return true;
        return false;
    }

    static float EaseInOutBack(float x, float s)
    {
        float c1 = s;
        float c2 = c1 * 1.525f;

        if (x < 0.5f)
        {
            float t = 2f * x;
            return 0.5f * (t * t * ((c2 + 1f) * t - c2));
        }
        else
        {
            float t = 2f * x - 2f;
            return 0.5f * (t * t * ((c2 + 1f) * t + c2) + 2f);
        }
    }

    // Quaternion smooth damp (basit ama efektif)
    static Quaternion SmoothDampQuaternion(Quaternion current, Quaternion target, ref float vel, float smoothTime)
    {
        float t = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.0001f, smoothTime));
        // vel parametresi placeholder: stabil his için min damping gibi
        vel = Mathf.Lerp(vel, 0f, t);
        return Quaternion.Slerp(current, target, t);
    }
}
