using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Inspector에 등록된 UI Tween Step을 순서대로 실행한
/// </summary>
public class UITweenSequencePlayer : MonoBehaviour
{
    /// <summary>
    /// Tween을 Sequence에 연결하는 방식을 정의
    /// </summary>
    public enum StepMode
    {
        Append,
        Join
    }

    /// <summary>
    /// Step에서 실행할 UI 효과를 정의
    /// </summary>
    public enum EffectType
    {
        Scale,
        Fade,
        Delay,
        Callback
    }

    /// <summary>
    /// Step이 사용할 대상을 정의
    /// </summary>
    public enum TargetType
    {
        Direct,
        TargetA,
        TargetB
    }

    /// <summary>
    /// Sequence에 등록할 단일 Tween Step 설정을 보관
    /// </summary>
    [Serializable]
    public sealed class TweenStep
    {
        [SerializeField]
        private StepMode stepMode;
        [SerializeField]
        private EffectType effectType;
        [SerializeField]
        private TargetType targetType;

        [SerializeField]
        private RectTransform directTarget;

        [SerializeField]
        private bool useStartValue;
        [SerializeField]
        private Vector3 startScale = Vector3.one;
        [SerializeField]
        private float startAlpha = 0f;

        [SerializeField]
        private Vector3 targetScale = Vector3.one;
        [SerializeField]
        private float targetAlpha = 1f;
        [SerializeField]
        private float duration = 0.1f;
        [SerializeField]
        private Ease ease = Ease.OutCubic;

        [SerializeField]
        private UnityEvent callBack;

        public StepMode Mode => stepMode;
        public EffectType EffectType => effectType;
        public TargetType TargetType => targetType;
        public RectTransform DirectTarget => directTarget;

        public bool UseStartValue => useStartValue;
        public Vector3 StartScale => startScale;
        public float StartAlpha => startAlpha;

        public Vector3 TargetScale => targetScale;
        public float TargetAlpha => targetAlpha;
        public float Duration => duration;
        public Ease Ease => ease;
        public UnityEvent Callback => callBack;
    }

    [SerializeField]
    private List<TweenStep> steps;
    [SerializeField]
    private bool ignoreTimeScale = true;
    [SerializeField]
    private UnityEvent OnStarted;
    [SerializeField]
    private UnityEvent OnCompleted;

    private RectTransform targetA;
    private RectTransform targetB;
    private Sequence activeSequence;

    public bool IsPlaying => activeSequence != null && activeSequence.IsActive() && activeSequence.IsPlaying();

    /// <summary>
    /// 오브젝트가 제거될 때 실행 중인 Sequence를 정리
    /// </summary>
    private void OnDestroy()
    {
        Stop();
    }

    /// <summary>
    /// Step 설정에 해당하는 RectTransform을 반환
    /// </summary>
    /// <param name="step">대상을 확인할 Tween Step</param>
    /// <returns>Step에 연결된 RectTransform</returns>
    private RectTransform ResolveTarget(TweenStep step)
    {
        RectTransform target;

        switch (step.TargetType)
        {
            case TargetType.Direct:
                target = step.DirectTarget;
                break;
            case TargetType.TargetA:
                target = targetA;
                break;
            case TargetType.TargetB:
                target = targetB;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (target == null)
            throw new InvalidOperationException("Tween Target이 없음");

        return target;
    }

    /// <summary>
    /// Fade Step 대상에 연결된 CanvasGroup을 반환
    /// </summary>
    /// <param name="step">Fade를 실행할 Tween Step</param>
    /// <returns>대상 RectTransform과 같은 오브젝트의 CanvasGroup</returns>
    private CanvasGroup ResolveCanvasGroup(TweenStep step)
    {
        RectTransform target = ResolveTarget(step);
        CanvasGroup group = target.GetComponent<CanvasGroup>();

        if (group == null)
            throw new InvalidOperationException($"{target.name}에 CanvasGroup이 없음");

        return group;
    }

    /// <summary>
    /// Scale Step 설정으로 Tween을 생성
    /// </summary>
    /// <param name="step">Scale 설정을 가진 Tween Step</param>
    /// <returns>생성된 Scale Tween</returns>
    private Tween CreateScaleTween(TweenStep step)
    {
        RectTransform target = ResolveTarget(step);

        if (step.UseStartValue)
            return target.DOScale(step.TargetScale, step.Duration).From(step.StartScale).SetEase(step.Ease);

        return target.ScaleTo(step.TargetScale, step.Duration, step.Ease);
    }

    /// <summary>
    /// Fade Step 설정으로 Tween을 생성
    /// </summary>
    /// <param name="step">Fade 설정을 가진 Tween Step</param>
    /// <returns>생성된 Fade Tween</returns>
    private Tween CreateFadeTween(TweenStep step)
    {
        CanvasGroup target = ResolveCanvasGroup(step);

        if (step.UseStartValue)
            return target.DOFade(step.TargetAlpha, step.Duration).From(step.StartAlpha).SetEase(step.Ease);

        return target.FadeTo(step.TargetAlpha, step.Duration, step.Ease);
    }

    /// <summary>
    /// 등록된 Step을 이용해 새로운 Sequence를 생성
    /// </summary>
    /// <param name="includeCallbacks">Callback Step 포함 여부 확인</param>
    /// <returns>생성된 Sequence</returns>
    private Sequence CreateSequence(bool includeCallbacks)
    {
        Sequence sequence = DOTween.Sequence();

        foreach (TweenStep step in steps)
        {
            switch (step.EffectType)
            {
                case EffectType.Scale:
                    {
                        Tween tween = CreateScaleTween(step);
                        AddTween(sequence, tween, step.Mode);
                        break;
                    }

                case EffectType.Fade:
                    {
                        Tween tween = CreateFadeTween(step);
                        AddTween(sequence, tween, step.Mode);
                        break;
                    }

                case EffectType.Delay:
                    sequence.AppendInterval(step.Duration);
                    break;

                case EffectType.Callback:
                    if (includeCallbacks)
                        sequence.AppendCallback(step.Callback.Invoke);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        return sequence;
    }

    /// <summary>
    /// Tween을 지정된 방식으로 Sequence에 연결
    /// </summary>
    /// <param name="sequence">Tween을 연결할 Sequence</param>
    /// <param name="tween">연결할 Tween</param>
    /// <param name="mode">Append 또는 Join 연결 방식</param>
    private void AddTween(Sequence sequence, Tween tween, StepMode mode)
    {
        switch (mode)
        {
            case StepMode.Append:
                sequence.Append(tween);
                break;
            case StepMode.Join:
                sequence.Join(tween);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

#if UNITY_EDITOR

    /// <summary>
    /// Unity Editor Preview에서 사용할 Sequence를 생성
    /// 런타임 UnityEvent와 Callback Step은 실행하지 않함
    /// </summary>
    /// <returns>Editor Preview용 Sequence</returns>
    public Sequence CreateEditorPreviewSequence()
    {
        if (steps.Count == 0)
            throw new InvalidOperationException("실행할 UITween Step이 없음");

        return CreateSequence(false);
    }

#endif

    /// <summary>
    /// 현재 실행 중인 Sequence를 완료 처리 없이 중단
    /// </summary>
    public void Stop()
    {
        if (activeSequence == null)
            return;

        if (activeSequence.IsActive())
            activeSequence.Kill();

        activeSequence = null;
    }

    /// <summary>
    /// TargetA와 TargetB를 런타임 대상으로 연결
    /// </summary>
    /// <param name="newTargetA">TargetA로 사용할 RectTransform</param>
    /// <param name="newTargetB">TargetB로 사용할 RectTransform</param>
    public void BindTargets(RectTransform newTargetA, RectTransform newTargetB)
    {
        targetA = newTargetA;
        targetB = newTargetB;
    }

    /// <summary>
    /// Inspector에 등록된 Step 순서대로 UI Sequence를 실행
    /// </summary>
    public void Play()
    {
        if (IsPlaying)
            return;

        if (steps.Count == 0)
            throw new InvalidOperationException("실행할 UITween Step이 없음");

        // Play Mode에서는 정상 실행
        OnStarted?.Invoke();

        // true이므로 Callback Step도 포함
        Sequence sequence = CreateSequence(true);

        activeSequence = sequence;

        sequence.SetUpdate(ignoreTimeScale).SetLink(gameObject).OnComplete(() =>
        {
            if (activeSequence == sequence)
                activeSequence = null;

            // Play Mode에서는 정상 실행
            OnCompleted?.Invoke();
        });
    }
}
