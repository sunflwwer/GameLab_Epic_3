using UnityEngine;
using System.Collections;

public class StepThresholdRiseMover : MonoBehaviour
{
    [Header("Source (스텝 공급자)")]
    [SerializeField, Tooltip("스텝 값을 제공하는 AttackStepYDriver")]
    private AttackStepYDriver sourceDriver;

    [Header("Follower (이동 대상)")]
    [SerializeField, Tooltip("올라갈 대상(비우면 자기 자신)")]
    private Transform target;

    [Header("지연 & 이동")]
    [SerializeField, Tooltip("트리거 후 대기 시간(초)")]
    private float delaySeconds = 1.0f;

    [SerializeField, Tooltip("목표 Y까지 올라가는 데 걸리는 시간(초)")]
    private float riseDuration = 0.8f;

    [SerializeField, Tooltip("이징 커브 (x: 0~1 진행도, y: 0~1 보간비)")]
    private AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("좌표/목표 설정")]
    [SerializeField, Tooltip("로컬 좌표 사용 여부 (false면 월드 좌표)")]
    private bool useLocalPosition = true;

    public enum TargetMode { AbsoluteY, RelativeFromCurrent }
    [SerializeField] private TargetMode targetMode = TargetMode.AbsoluteY;

    [SerializeField, Tooltip("AbsoluteY: 이 값으로 Y를 맞춤 / RelativeFromCurrent: 현재 위치 + 이 값 만큼")]
    private float targetYValue = 5f;

    [Header("기타")]
    [SerializeField, Tooltip("진행 중이면 새로 트리거 시 이전 Tween을 중단하고 다시 시작")]
    private bool restartIfRetriggered = true;

    private Coroutine _routine;

    private void Awake()
    {
        if (target == null) target = transform;
    }

    private void OnEnable()
    {
        if (sourceDriver == null)
        {
            Debug.LogWarning($"{nameof(StepThresholdRiseMover)}: sourceDriver가 비어 있습니다.");
            return;
        }

        // 임계치 도달 이벤트 구독
        sourceDriver.ThresholdReached += OnThresholdReached;
    }

    private void OnDisable()
    {
        if (sourceDriver != null)
            sourceDriver.ThresholdReached -= OnThresholdReached;
    }

    private void OnThresholdReached()
    {
        if (_routine != null && !restartIfRetriggered) return;

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(RiseFlow());
    }

    private IEnumerator RiseFlow()
    {
        // 1) 지연
        if (delaySeconds > 0f)
            yield return new WaitForSeconds(delaySeconds);

        // 2) 시작/목표 Y 계산
        float startY = useLocalPosition ? target.localPosition.y : target.position.y;
        float goalY = (targetMode == TargetMode.AbsoluteY) ? targetYValue : (startY + targetYValue);

        // 3) Tween
        float dur = Mathf.Max(0.0001f, riseDuration);
        float elapsed = 0f;
        while (elapsed < dur)
        {
            float alpha = Mathf.Clamp01(elapsed / dur);
            float k = ease != null ? Mathf.Clamp01(ease.Evaluate(alpha)) : alpha;
            float y = Mathf.Lerp(startY, goalY, k);
            ApplyY(y);

            elapsed += Time.deltaTime;
            yield return null;
        }

        ApplyY(goalY);
        _routine = null;
    }

    private void ApplyY(float y)
    {
        if (useLocalPosition)
        {
            var p = target.localPosition;
            p.y = y;
            target.localPosition = p;
        }
        else
        {
            var p = target.position;
            p.y = y;
            target.position = p;
        }
    }
}
