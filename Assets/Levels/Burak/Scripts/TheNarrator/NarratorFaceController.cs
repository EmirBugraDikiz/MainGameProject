using UnityEngine;

public class NarratorFaceController : MonoBehaviour
{
    [Header("Assign these 3 materials")]
    public Material idleMat;
    public Material talkMat;
    public Material angryMat;

    [Header("Talk animation")]
    public bool animateWhenTalking = true;
    public float mouthSpeed = 12f;
    public Vector2 mouthRange = new Vector2(0.15f, 0.6f);

    [Header("Glitch pulse while talking")]
    public bool glitchPulseWhenTalking = true;
    public float glitchPulseSpeed = 6f;
    public float glitchPulseAmount = 0.15f;

    [Header("Debug")]
    public bool enableTestKeys = false; // editor test için

    private Renderer rend;
    private Material runtimeMat;

    private enum Mode { Idle, Talk, Angry }
    private Mode mode = Mode.Idle;

    static readonly int MouthOpenID = Shader.PropertyToID("_MouthOpen");
    static readonly int GlitchAmountID = Shader.PropertyToID("_GlitchAmount");
    static readonly int FlickerID = Shader.PropertyToID("_Flicker");
    static readonly int StepEdgeID = Shader.PropertyToID("_StepEdge");

    float baseGlitch;
    float baseFlicker;
    float baseStepEdge;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        SetIdle();
    }

    void Update()
    {
        if (enableTestKeys)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) SetIdle();
            if (Input.GetKeyDown(KeyCode.Alpha2)) SetTalk();
            if (Input.GetKeyDown(KeyCode.Alpha3)) SetAngry();
        }

        if (runtimeMat == null) return;

        if (mode == Mode.Talk && animateWhenTalking)
        {
            float t = (Mathf.Sin(Time.time * mouthSpeed) + 1f) * 0.5f;
            float mouth = Mathf.Lerp(mouthRange.x, mouthRange.y, t);
            runtimeMat.SetFloat(MouthOpenID, mouth);

            if (glitchPulseWhenTalking)
            {
                float p = (Mathf.Sin(Time.time * glitchPulseSpeed) + 1f) * 0.5f;
                runtimeMat.SetFloat(GlitchAmountID, baseGlitch + p * glitchPulseAmount);
                runtimeMat.SetFloat(FlickerID, baseFlicker + p * 0.08f);
            }
        }

        if (mode == Mode.Angry)
        {
            float p = (Mathf.Sin(Time.time * 8f) + 1f) * 0.5f;
            float edge = Mathf.Lerp(0.2f, baseStepEdge, 1f - p * 0.7f);
            runtimeMat.SetFloat(StepEdgeID, edge);
        }
    }

    void Apply(Material preset)
    {
        if (rend == null || preset == null) return;

        runtimeMat = new Material(preset);
        rend.material = runtimeMat;

        baseGlitch = runtimeMat.HasProperty(GlitchAmountID) ? runtimeMat.GetFloat(GlitchAmountID) : 0f;
        baseFlicker = runtimeMat.HasProperty(FlickerID) ? runtimeMat.GetFloat(FlickerID) : 0f;
        baseStepEdge = runtimeMat.HasProperty(StepEdgeID) ? runtimeMat.GetFloat(StepEdgeID) : 0.7f;
    }

    public void SetIdle()
    {
        mode = Mode.Idle;
        Apply(idleMat);
        runtimeMat?.SetFloat(MouthOpenID, 0f);
    }

    public void SetTalk()
    {
        mode = Mode.Talk;
        Apply(talkMat);
    }

    public void SetAngry()
    {
        mode = Mode.Angry;
        Apply(angryMat);
        runtimeMat?.SetFloat(MouthOpenID, 0.25f);
    }
}
