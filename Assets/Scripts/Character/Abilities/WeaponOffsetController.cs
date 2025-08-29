using UnityEngine;

public class WeaponOffsetController : MonoBehaviour
{
    public delegate void Recovered();
    public event Recovered onRecovered;

    [Header("Recovery Settings")]
    [Tooltip("Speed of return - Higher is faster")]
    public float returnSpeed = 8f;
    [Tooltip("When the curve reaches 1, the weapon is at its original position.")]
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

    void LateUpdate()
    {
        if (!_recovering) return;
        _returnT += Time.deltaTime * returnSpeed;
        float t = Mathf.Clamp01(_returnT);
        float k = returnCurve.Evaluate(t);

        transform.localRotation = Quaternion.Slerp(_startRot, _baseLocalRot, k);
        transform.localPosition = Vector3.Lerp(_startPos, _baseLocalPos, k);

        if (t >= 1f)
        {
            _recovering = false;
            transform.localRotation = _baseLocalRot;
            transform.localPosition = _baseLocalPos;
            onRecovered?.Invoke();
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
    }
}
