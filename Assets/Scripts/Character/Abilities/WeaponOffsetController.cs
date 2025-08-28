using UnityEngine;

public class WeaponOffsetController : MonoBehaviour
{
    public delegate void Recovered();
    public event Recovered onRecovered;

    [Header("Recovery Settings")]
    public float returnSpeed = 8f;       // how fast it decays
    public AnimationCurve returnCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    Quaternion _baseLocalRot;
    Vector3 _baseLocalPos;

    Quaternion _startRot;
    Vector3 _startPos;

    float _returnT;
    bool _recovering;

    void Awake()
    {
        _baseLocalRot = transform.localRotation;
        _baseLocalPos = transform.localPosition;
    }

    private void Start()
    {
        onRecovered += () => { Debug.Log("Recovered"); };
    }

    void LateUpdate()
    {
        if (!_recovering) return;
        _returnT += Time.deltaTime * returnSpeed;
        float t = Mathf.Clamp01(_returnT);
        float k = returnCurve.Evaluate(t);

        // Lerp/Slerp from the animation's final pose back to base
        transform.localRotation = Quaternion.Slerp(_startRot, _baseLocalRot, k);
        transform.localPosition = Vector3.Lerp(_startPos, _baseLocalPos, k);

        if (t >= 1f)
        {
            _recovering = false;
            transform.localRotation = _baseLocalRot;
            transform.localPosition = _baseLocalPos;
            onRecovered?.Invoke();
            Debug.Log("Recovered Invoke");
        }
    }

    /// <summary>
    /// Call this when the attack animation finishes.
    /// Starts recovery from the current animated pose.
    /// </summary>
    public void BeginRecovery()
    {
        _startRot = transform.localRotation;
        _startPos = transform.localPosition;

        _returnT = 0f;
        _recovering = true;
        Debug.Log("Begin Recovery");
    }
}
