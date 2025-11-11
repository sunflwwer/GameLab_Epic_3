using System;
using UnityEngine;

public class AttackStepYDriver : MonoBehaviour
{
    [Header("Target & Steps")]
    [SerializeField, Tooltip("Y를 올릴 대상(지정하지 않으면 자기 자신)")]
    private Transform target;             // 비워두면 this.transform 사용
    [SerializeField, Tooltip("공격 5회에 도달하는 총 스텝 수")]
    private int maxSteps = 5;             // 5로 유지

    [Header("Threshold Event")]
    [SerializeField, Tooltip("이 값 이상이 되는 순간 ThresholdReached 이벤트 발사")]
    private int eventThreshold = 5;

    [Header("Y Range")]
    [SerializeField, Tooltip("Y값 최소/최대")]
    private float minY = 0f;
    [SerializeField] private float maxY = 5f;

    [Header("Smoothing / Decay (옵션)")]
    [SerializeField, Tooltip("Y 이동을 부드럽게 보간")]
    private bool smoothLerp = true;
    [SerializeField, Tooltip("보간 속도")]
    private float lerpSpeed = 10f;
    [SerializeField, Tooltip("시간이 지나면 단계가 서서히 줄어드는지 여부")]
    private bool enableDecay = false;
    [SerializeField, Tooltip("초당 떨어지는 단계량(예: 0.5면 2초에 1스텝 감소)")]
    private float decayPerSecond = 0.0f;
    [SerializeField, Tooltip("최소 단계로 리셋되는 시간(초), 0이면 미사용")]
    private float autoResetAfter = 0f;

    // 외부에서 구독할 이벤트: 스텝이 eventThreshold 이상으로 '처음' 진입할 때 발생
    public event Action ThresholdReached;

    private float currentSteps = 0f;  // 실수로 들고가서 부드럽게 다룰 수 있음
    private float lastAttackTime = 0f;
    private bool thresholdFired = false; // 한 번 발사 후, 다시 임계치 아래로 떨어져야 재발사 가능

    private void Awake()
    {
        if (target == null) target = transform;
        eventThreshold = Mathf.Clamp(eventThreshold, 1, Mathf.Max(1, maxSteps));
    }

    private void Update()
    {
        // Decay(옵션): 시간이 흐르면 단계가 천천히 감소
        if (enableDecay && currentSteps > 0f && decayPerSecond > 0f)
        {
            currentSteps = Mathf.Max(0f, currentSteps - decayPerSecond * Time.deltaTime);
        }

        // 일정 시간 입력 없으면 0으로 자동 리셋(옵션)
        if (autoResetAfter > 0f && Time.time - lastAttackTime >= autoResetAfter)
        {
            currentSteps = 0f;
        }

        // Threshold 재무장(임계치 아래로 내려왔을 때만 다시 쏠 수 있게)
        if (currentSteps < eventThreshold)
        {
            thresholdFired = false;
        }

        // 타깃 Y 보간
        float t = Mathf.Clamp01(currentSteps / Mathf.Max(1, maxSteps));
        float targetY = Mathf.Lerp(minY, maxY, t);

        Vector3 pos = target.localPosition;
        if (smoothLerp)
            pos.y = Mathf.Lerp(pos.y, targetY, 1f - Mathf.Exp(-lerpSpeed * Time.deltaTime));
        else
            pos.y = targetY;

        target.localPosition = pos;
    }

    // 외부(UmbrellaWaterDrop)에서 호출할 수 있는 public 메서드
    public void IncrementStep()
    {
        lastAttackTime = Time.time;
        currentSteps = Mathf.Min(maxSteps, currentSteps + 1f);

        // 임계치 도달 순간 이벤트 발사(최초 1회)
        if (!thresholdFired && currentSteps >= eventThreshold)
        {
            thresholdFired = true;
            ThresholdReached?.Invoke();
        }
    }

    // 필요 시 외부에서 초기화할 수 있도록 공개 메서드
    public void ResetProgress()
    {
        currentSteps = 0f;
        lastAttackTime = 0f;
        thresholdFired = false;
    }

    // 현재 단계나 보간된 Y를 확인하고 싶을 때
    public int GetRoundedSteps() => Mathf.RoundToInt(currentSteps);
    public float Get01() => Mathf.Clamp01(currentSteps / Mathf.Max(1, maxSteps));
}
