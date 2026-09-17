using UnityEngine;
using System.Collections;

/// <summary>
/// Procedural animation cho nhân vật Roblox-style.
/// Không cần animation clip – tất cả tính bằng sin/cos wave trên các xương.
/// </summary>
public class CharacterAnimator : MonoBehaviour
{
    // ────────────────────── Inspector ──────────────────────
    [Header("Run Settings")]
    public float runFrequency  = 2.8f;   // Tần số bước chạy (cycles/s)
    public float runLegAngle   = 30f;    // Góc swing chân
    public float runArmAngle   = 25f;    // Góc swing tay
    public float runLeanAngle  = 8f;     // Nghiêng người về phía trước khi chạy

    [Header("Idle Settings")]
    public float idleBobSpeed  = 1.0f;   // Tốc độ thở nhẹ
    public float idleBobAmount = 0.012f; // Độ nảy lên xuống

    [Header("Jump Settings")]
    public float jumpSquashY   = 0.85f;  // Squash khi cất đất
    public float jumpStretchY  = 1.12f;  // Stretch khi lơ lửng
    public float jumpLegSpread = 20f;    // Chân xòe khi nhảy

    [Header("Interact Settings")]
    public float interactDuration = 1.0f;  // Thời gian animation Interact
    public float interactLeanFwd  = 20f;   // Cúi người về phía trước

    [Header("Death Settings")]
    public float deathFallDuration = 0.6f; // Thời gian ngã

    [Header("Blend")]
    public float blendSpeed = 10f;

    // ────────────────────── Bones ──────────────────────
    Transform _spine, _spine001;
    Transform _upperArmL, _upperArmR;
    Transform _forearmL,  _forearmR;
    Transform _thighL,    _thighR;
    Transform _shinL,     _shinR;
    Transform _footL,     _footR;
    Transform _modelRoot;

    // Rest pose
    Quaternion _rSpine, _rSpine001;
    Quaternion _rUAL, _rUAR, _rFAL, _rFAR;
    Quaternion _rTHL, _rTHR, _rSHL, _rSHR;
    Quaternion _rFTL, _rFTR;
    Vector3    _modelRestLocalPos;
    Vector3    _modelRestLocalScale; // Scale gốc của model (không phải 1,1,1!)

    // ────────────────────── State ──────────────────────
    float _phase       = 0f;
    float _runBlend    = 0f;   // 0=idle, 1=run
    bool  _isDead      = false;
    bool  _isInteracting = false;
    bool  _wasGrounded = true;
    CharacterController _cc;

    // ═══════════════════════════════════════════════════
    void Awake()
    {
        _cc = GetComponent<CharacterController>();

        // Tìm model root
        _modelRoot = transform.Find("Char9_Model");
        if (_modelRoot == null) _modelRoot = transform;

        // Tự động tìm bones theo tên
        foreach (Transform t in _modelRoot.GetComponentsInChildren<Transform>())
        {
            switch (t.name)
            {
                case "spine":       _spine    = t; break;
                case "spine.001":   _spine001 = t; break;
                case "upper_arm.L": _upperArmL = t; break;
                case "upper_arm.R": _upperArmR = t; break;
                case "forearm.L":   _forearmL  = t; break;
                case "forearm.R":   _forearmR  = t; break;
                case "thigh.L":     _thighL    = t; break;
                case "thigh.R":     _thighR    = t; break;
                case "shin.L":      _shinL     = t; break;
                case "shin.R":      _shinR     = t; break;
                case "foot.L":      _footL     = t; break;
                case "foot.R":      _footR     = t; break;
            }
        }

        // Lưu rest pose
        SaveRestPose();
        _modelRestLocalPos  = _modelRoot.localPosition;
        _modelRestLocalScale = _modelRoot.localScale;
    }

    void SaveRestPose()
    {
        if (_spine)    _rSpine    = _spine.localRotation;
        if (_spine001) _rSpine001 = _spine001.localRotation;
        if (_upperArmL) _rUAL = _upperArmL.localRotation;
        if (_upperArmR) _rUAR = _upperArmR.localRotation;
        if (_forearmL)  _rFAL = _forearmL.localRotation;
        if (_forearmR)  _rFAR = _forearmR.localRotation;
        if (_thighL)    _rTHL = _thighL.localRotation;
        if (_thighR)    _rTHR = _thighR.localRotation;
        if (_shinL)     _rSHL = _shinL.localRotation;
        if (_shinR)     _rSHR = _shinR.localRotation;
        if (_footL)     _rFTL = _footL.localRotation;
        if (_footR)     _rFTR = _footR.localRotation;
    }

    // ═══════════════════════════════════════════════════
    void Update()
    {
        if (_isDead || _isInteracting) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        float speed = Mathf.Clamp01(new Vector2(h, v).magnitude);

        bool grounded = _cc != null && _cc.isGrounded;

        // Interact key
        if (Input.GetKeyDown(KeyCode.E))
            StartCoroutine(InteractRoutine());

        // Phase tăng theo speed
        _phase += Time.deltaTime * runFrequency * Mathf.PI * 2f * Mathf.Max(speed, 0.3f);

        // Blend run/idle
        _runBlend = Mathf.Lerp(_runBlend, grounded ? speed : 0f, blendSpeed * Time.deltaTime);

        // Jump
        if (!grounded)
            ApplyAirPose();
        else
            ApplyGroundPose(_runBlend);

        // Landing squash (khi vừa chạm đất)
        if (grounded && !_wasGrounded)
            StartCoroutine(LandSquash());

        _wasGrounded = grounded;
    }

    // ────────── Locomotion (Idle + Run blend) ──────────
    void ApplyGroundPose(float blend)
    {
        float sin = Mathf.Sin(_phase);
        float cos = Mathf.Cos(_phase);

        // === Chân (thigh) ===
        float legAngle = blend * runLegAngle;
        ApplyBone(_thighL, _rTHL, Vector3.right,  sin * legAngle);
        ApplyBone(_thighR, _rTHR, Vector3.right, -sin * legAngle);

        // === Bắp chân (shin) – hơi cong khi bước ===
        float shinBend = blend * runLegAngle * 0.5f * Mathf.Abs(sin);
        ApplyBone(_shinL, _rSHL, Vector3.right, shinBend);
        ApplyBone(_shinR, _rSHR, Vector3.right, shinBend);

        // === Tay (upper arm) – ngược pha với chân cùng bên ===
        float armAngle = blend * runArmAngle;
        ApplyBone(_upperArmL, _rUAL, Vector3.right, -sin * armAngle);
        ApplyBone(_upperArmR, _rUAR, Vector3.right,  sin * armAngle);

        // === Người nghiêng về phía trước khi chạy ===
        float lean = blend * runLeanAngle;
        ApplyBone(_spine, _rSpine, Vector3.right, lean);

        // === Idle body bob (thở) ===
        float idleSin = Mathf.Sin(Time.time * idleBobSpeed * Mathf.PI * 2f);
        float bobBlend = 1f - blend; // chỉ bob khi đứng yên
        float bobY = idleSin * idleBobAmount * bobBlend;
        if (_modelRoot != null)
            _modelRoot.localPosition = _modelRestLocalPos + new Vector3(0f, bobY, 0f);
    }

    // ────────── Air (Jump / Fall) ──────────
    void ApplyAirPose()
    {
        // Chân xòe nhẹ ra
        ApplyBone(_thighL, _rTHL, Vector3.right,  jumpLegSpread * 0.5f);
        ApplyBone(_thighR, _rTHR, Vector3.right,  jumpLegSpread * 0.5f);
        ApplyBone(_shinL,  _rSHL, Vector3.right,  jumpLegSpread);
        ApplyBone(_shinR,  _rSHR, Vector3.right,  jumpLegSpread);

        // Tay giơ lên nhẹ
        ApplyBone(_upperArmL, _rUAL, Vector3.right, -15f);
        ApplyBone(_upperArmR, _rUAR, Vector3.right, -15f);
    }

    // ────────── Land squash ──────────
    IEnumerator LandSquash()
    {
        float t = 0f;
        float dur = 0.18f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;
            // Squash rồi bounce về – nhân với scale GỐC, không phải 1
            float scaleY = Mathf.Lerp(jumpSquashY, 1f, p < 0.5f ? p * 2f : (p - 0.5f) * 2f);
            if (_modelRoot != null)
                _modelRoot.localScale = new Vector3(
                    _modelRestLocalScale.x,
                    _modelRestLocalScale.y * scaleY,
                    _modelRestLocalScale.z);
            yield return null;
        }
        if (_modelRoot != null)
            _modelRoot.localScale = _modelRestLocalScale; // Trả về đúng scale gốc
    }

    // ────────── Interact (phím E) ──────────
    IEnumerator InteractRoutine()
    {
        _isInteracting = true;
        float t = 0f;
        float half = interactDuration * 0.5f;

        // Phase 1: Cúi người + giơ tay phải
        while (t < half)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / half);
            ApplyBone(_spine,    _rSpine,   Vector3.right,  interactLeanFwd * p);
            ApplyBone(_spine001, _rSpine001,Vector3.right,  interactLeanFwd * 0.5f * p);
            ApplyBone(_upperArmR, _rUAR,   Vector3.right, -60f * p);
            ApplyBone(_forearmR,  _rFAR,   Vector3.right, -30f * p);
            yield return null;
        }

        // Phase 2: Trở về
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / half);
            ApplyBone(_spine,    _rSpine,    Vector3.right,  interactLeanFwd * (1f - p));
            ApplyBone(_spine001, _rSpine001, Vector3.right,  interactLeanFwd * 0.5f * (1f - p));
            ApplyBone(_upperArmR, _rUAR,    Vector3.right, -60f * (1f - p));
            ApplyBone(_forearmR,  _rFAR,    Vector3.right, -30f * (1f - p));
            yield return null;
        }

        // Reset về rest
        ResetAllBones();
        _isInteracting = false;
    }

    // ────────── Death ──────────
    /// <summary>Gọi từ bên ngoài khi nhân vật chết.</summary>
    public void TriggerDeath()
    {
        if (_isDead) return;
        _isDead = true;
        StartCoroutine(DeathRoutine());
    }

    IEnumerator DeathRoutine()
    {
        float t = 0f;
        while (t < deathFallDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / deathFallDuration);

            // Ngã ngang về phía sau
            ApplyBone(_spine,    _rSpine,    Vector3.right, -80f * p);
            ApplyBone(_spine001, _rSpine001, Vector3.right, -20f * p);

            // Tay duỗi ra
            ApplyBone(_upperArmL, _rUAL, Vector3.right, -45f * p);
            ApplyBone(_upperArmR, _rUAR, Vector3.right, -45f * p);

            // Chân thả lỏng
            ApplyBone(_thighL, _rTHL, Vector3.right,  15f * p);
            ApplyBone(_thighR, _rTHR, Vector3.right, -15f * p);

            yield return null;
        }
        // Giữ nguyên pose death
    }

    // ────────── Utility ──────────

    /// <summary>Áp dụng offset góc xoay (degree) lên local rotation gốc của xương.</summary>
    static void ApplyBone(Transform bone, Quaternion restRot, Vector3 localAxis, float angleDeg)
    {
        if (bone == null) return;
        bone.localRotation = restRot * Quaternion.AngleAxis(angleDeg, localAxis);
    }

    void ResetAllBones()
    {
        if (_spine)     _spine.localRotation    = _rSpine;
        if (_spine001)  _spine001.localRotation = _rSpine001;
        if (_upperArmL) _upperArmL.localRotation = _rUAL;
        if (_upperArmR) _upperArmR.localRotation = _rUAR;
        if (_forearmL)  _forearmL.localRotation  = _rFAL;
        if (_forearmR)  _forearmR.localRotation  = _rFAR;
        if (_thighL)    _thighL.localRotation    = _rTHL;
        if (_thighR)    _thighR.localRotation    = _rTHR;
        if (_shinL)     _shinL.localRotation     = _rSHL;
        if (_shinR)     _shinR.localRotation     = _rSHR;
        if (_footL)     _footL.localRotation     = _rFTL;
        if (_footR)     _footR.localRotation     = _rFTR;
    }
}

