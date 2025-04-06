using UnityEngine;
using UnityEngine.UI;

public enum SpinType { OffSpin, LegSpin }

public class BallThrower : MonoBehaviour
{
    public static BallThrower instance;

    [Header("Ball Setup")]
    public GameObject ballPrefab;
    public Transform leftArmSpawnPoint;
    public Transform rightArmSpawnPoint;
    public Transform bounceTarget;

    [Header("Throw Settings")]
    public float throwPowerMultiplier = 1.5f;
    public float throwDelay = 0.5f;

    [Header("Swing / Spin Settings")]
    public bool isSpin = false;
    public bool isLeftArm = false;
    public SpinType spinType = SpinType.OffSpin;
    [Range(0f, 1.5f)] public float swingStrength = 0f; // Signed swing strength
    [Range(0f, 1.5f)] public float spinStrength = 1f;
    [Range(0f, 1f)] public float accuracy = 1f;

    [Header("No Ball")]
    public Text noBallText;
    public float noBallThreshold = 0.9f;

    [Header("UI References")]
    public Slider speedSlider;
    public Button switchSideButton;
    public Button spinToggleButton;
    public Button spinTypeToggleButton;
    public Text modeText;
    public Text spinTypeText;

    [HideInInspector] public GameObject currentBall;
    [HideInInspector] public bool ballHasBounced = false;

    private float autoSpeedValue = 0f;
    private bool isSpeedSelecting = false;
    private bool isIncreasingSpeed = true;
    private bool isWaitingToThrow = false;

    private enum ThrowState { Aiming, SpeedSelect, ReadyToThrow }
    private ThrowState currentState = ThrowState.Aiming;

    void Awake() => instance = this;

    void Start()
    {
        UpdateBowlingSide();
        UpdateModeText();
        UpdateSpinTypeText();

        if (noBallText != null)
            noBallText.gameObject.SetActive(false);

        switchSideButton?.onClick.AddListener(SwitchSide);
        spinToggleButton?.onClick.AddListener(ToggleSpin);
        spinTypeToggleButton?.onClick.AddListener(ToggleSpinType);
    }

    void Update()
    {
        switch (currentState)
        {
            case ThrowState.Aiming:
                HandleTargetMovement();
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    currentState = ThrowState.SpeedSelect;
                    isSpeedSelecting = true;
                    autoSpeedValue = 0f;
                    isIncreasingSpeed = true;
                }
                break;

            case ThrowState.SpeedSelect:
                if (!isWaitingToThrow)
                {
                    AnimateSpeedSlider();
                    if (Input.GetKeyDown(KeyCode.Return))
                    {
                        isSpeedSelecting = false;
                        isWaitingToThrow = true;
                        Invoke(nameof(FinalizeThrow), throwDelay);
                    }
                }
                break;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            autoSpeedValue = speedSlider.value;
            ThrowBall();
            ResetState();
        }
    }

    void FixedUpdate()
    {
        if (currentBall != null && !ballHasBounced && !isSpin)
        {
            Rigidbody rb = currentBall.GetComponent<Rigidbody>();
            Vector3 velocity = rb.velocity.normalized;
            Vector3 swingDir = Vector3.Cross(velocity, Vector3.up).normalized;
            swingDir *= isLeftArm ? -1f : 1f;

            Vector3 randomOffset = new Vector3(Random.Range(-0.2f, 0.2f), 0f, 0f);
            Vector3 randomizedSwingDir = (swingDir + randomOffset).normalized;
            float swingVariation = Random.Range(1.0f, 1.4f);

            rb.AddForce(randomizedSwingDir * Mathf.Abs(swingStrength) * swingVariation * 4f, ForceMode.Force);
        }
    }

    void HandleTargetMovement()
    {
        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) move += Vector3.forward;
        if (Input.GetKey(KeyCode.S)) move += Vector3.back;
        if (Input.GetKey(KeyCode.A)) move += Vector3.left;
        if (Input.GetKey(KeyCode.D)) move += Vector3.right;

        bounceTarget.position += move * Time.deltaTime * 5f;
    }

    void AnimateSpeedSlider()
    {
        float speed = 0.7f;
        autoSpeedValue += (isIncreasingSpeed ? 1 : -1) * Time.deltaTime * speed;
        autoSpeedValue = Mathf.Clamp01(autoSpeedValue);
        speedSlider.value = autoSpeedValue;

        if (autoSpeedValue >= 1f || autoSpeedValue <= 0f)
            isIncreasingSpeed = !isIncreasingSpeed;
    }

    void FinalizeThrow()
    {
        currentState = ThrowState.ReadyToThrow;
        ThrowBall();
        ResetState();
        isWaitingToThrow = false;
    }

    void ResetState()
    {
        currentState = ThrowState.Aiming;
        speedSlider.value = 0f;
        autoSpeedValue = 0f;
    }

    void SwitchSide()
    {
        isLeftArm = !isLeftArm;
        UpdateBowlingSide();
        UpdateModeText();
    }

    void ToggleSpin()
    {
        isSpin = !isSpin;
        UpdateModeText();
    }

    void ToggleSpinType()
    {
        spinType = (spinType == SpinType.OffSpin) ? SpinType.LegSpin : SpinType.OffSpin;
        UpdateSpinTypeText();
    }

    void UpdateModeText()
    {
        if (modeText != null)
        {
            string side = isLeftArm ? "Left Arm" : "Right Arm";
            string type = isSpin ? "Spin" : "Swing";
            modeText.text = $"{side} - {type}";
        }
    }

    void UpdateSpinTypeText()
    {
        if (spinTypeText != null)
            spinTypeText.text = spinType == SpinType.OffSpin ? "Off-Spin" : "Leg-Spin";
    }

    void UpdateBowlingSide() { }

    void ThrowBall()
    {
        if (autoSpeedValue >= noBallThreshold)
        {
            if (noBallText != null)
            {
                noBallText.gameObject.SetActive(true);
                noBallText.text = "NO BALL!";
                Invoke(nameof(HideNoBall), 2f);
            }
        }
        else if (noBallText != null)
        {
            noBallText.gameObject.SetActive(false);
        }

        Transform spawnPoint = isLeftArm ? leftArmSpawnPoint : rightArmSpawnPoint;
        GameObject ball = Instantiate(ballPrefab, spawnPoint.position, Quaternion.identity);
        currentBall = ball;
        ballHasBounced = false;

        Rigidbody rb = ball.GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.angularDrag = 0.05f;

        // === SWING COMPENSATION FOR ACCURACY ===
        Vector3 baseDir = (bounceTarget.position - spawnPoint.position).normalized;
        Vector3 swingDir = Vector3.Cross(baseDir, Vector3.up).normalized;
        swingDir *= isLeftArm ? -1f : 1f;

        Vector3 adjustedTarget = bounceTarget.position - swingDir * swingStrength;

        Vector3 targetPoint = adjustedTarget + transform.forward * 3f;
        float travelTime = Mathf.Lerp(0.3f, 0.7f, autoSpeedValue);
        Vector3 velocity = CalculateLaunchVelocity(spawnPoint.position, targetPoint, travelTime);

        if (isSpin)
        {
            velocity.y += 2f;
            rb.drag = 0.8f;

            Vector3 spinAxis = (spinType == SpinType.OffSpin) ? transform.right : -transform.right;
            float torqueAmount = spinStrength * 50f * Random.Range(0.8f, 1.2f);
            rb.AddTorque(spinAxis * torqueAmount, ForceMode.Impulse);
        }
        else
        {
            rb.drag = 0.1f;
        }

        velocity *= throwPowerMultiplier;
        rb.velocity = velocity;
    }

    void HideNoBall()
    {
        if (noBallText != null)
            noBallText.gameObject.SetActive(false);
    }

    Vector3 CalculateLaunchVelocity(Vector3 start, Vector3 end, float time)
    {
        Vector3 velocity = (end - start) / time;
        velocity.y = (end.y - start.y - 0.5f * Physics.gravity.y * time * time) / time;
        return velocity;
    }
}
