using DG.Tweening;
using UnityEngine;

public static class UITweenEffects
{
    public static Tweener MoveTo(this RectTransform target, Vector2 targetPos, float duration, Ease ease = Ease.OutCubic)
    {
        return target.DOAnchorPos(targetPos, duration).SetEase(ease);
    }

    public static Tweener MoveFromOffeset(this RectTransform target, Vector2 startOffset, float duration, Ease ease = Ease.OutCubic)
    {
        Vector2 targetPos = target.anchoredPosition + startOffset;

        return target.DOAnchorPos(targetPos, duration).SetEase(ease);
    }

    public static Tweener ScaleTo(this RectTransform target, Vector3 targetScale, float duration, Ease ease = Ease.OutCubic)
    {
        return target.DOScale(targetScale, duration).SetEase(ease);
    }

    public static Tweener PunchSacle(this RectTransform target, float puchPower, float duration, int virato = 6, float elasicity = 0.5f)
    {
        Vector3 puchScale = Vector3.one * puchPower;

        return target.DOPunchScale(puchScale, duration, virato, elasicity);
    }

    public static Tweener FadeTo(this CanvasGroup target, float targetAlpha, float duration, Ease ease = Ease.OutQuad)
    {
        return target.DOFade(targetAlpha, duration).SetEase(ease);
    }

    public static Sequence ShrinkAndGrow(this RectTransform target, float shrinkRatio, float shrinkDuration, float growDuration)
    {
        Vector3 originScale = target.localScale;
        Vector3 shrinkScale = originScale * shrinkRatio;

        Sequence se = DOTween.Sequence();

        se.Append(target.DOScale(shrinkScale, shrinkDuration).SetEase(Ease.InQuad)).Append(target.DOScale(originScale, growDuration).SetEase(Ease.OutCubic));

        return se;
    }

    public static Sequence MoveAndResize(this RectTransform target, Vector2 targetPos, Vector2 targetSize, float duration, Ease ease = Ease.InOutCubic)
    {
        Sequence se = DOTween.Sequence();

        se.Append(target.DOAnchorPos(targetPos, duration).SetEase(ease)).Join(target.DOSizeDelta(targetSize, duration).SetEase(ease));

        return se;
    }

    public static Sequence JumpTo(this RectTransform target, Vector2 targetPos, float jumpPower, int jumpCount, float duration, Ease ease = Ease.OutQuad)
    {
        return target.DOJumpAnchorPos(targetPos, jumpPower, jumpCount, duration).SetEase(ease);
    }


}
