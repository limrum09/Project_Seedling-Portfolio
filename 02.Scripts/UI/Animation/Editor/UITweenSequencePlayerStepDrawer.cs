using UnityEditor;
using UnityEngine;

/// <summary>
/// UITweenSequencePlayer의 TweenStep을 EffectType에 맞춰 표시
/// </summary>
[CustomPropertyDrawer(typeof(UITweenSequencePlayer.TweenStep))]
public sealed class UITweenSequencePlayerStepDrawer : PropertyDrawer
{
    /// <summary>
    /// TweenStep의 현재 설정에 필요한 필드만 Inspector에 표시
    /// </summary>
    /// <param name="position">Property를 그릴 영역</param>
    /// <param name="property">표시할 TweenStep Property</param>
    /// <param name="label">TweenStep의 기본 Label</param>
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        Rect currentRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

        property.isExpanded = EditorGUI.Foldout(currentRect, property.isExpanded, GetStepLabel(property), true);

        if (!property.isExpanded)
        {
            EditorGUI.EndProperty();
            return;
        }

        EditorGUI.indentLevel++;
        currentRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        SerializedProperty effectType = property.FindPropertyRelative("effectType");
        DrawProperty(ref currentRect, effectType);

        UITweenSequencePlayer.EffectType currentEffect = (UITweenSequencePlayer.EffectType)effectType.enumValueIndex;

        switch (currentEffect)
        {
            case UITweenSequencePlayer.EffectType.Scale:
                DrawTargetProperties(ref currentRect, property);
                DrawScaleProperties(ref currentRect, property);
                break;

            case UITweenSequencePlayer.EffectType.Fade:
                DrawTargetProperties(ref currentRect, property);
                DrawFadeProperties(ref currentRect, property);
                break;

            case UITweenSequencePlayer.EffectType.Delay:
                DrawProperty(ref currentRect, property.FindPropertyRelative("duration"));
                break;

            case UITweenSequencePlayer.EffectType.Callback:
                DrawProperty(ref currentRect, property.FindPropertyRelative("callBack"));
                break;
        }

        EditorGUI.indentLevel--;
        EditorGUI.EndProperty();
    }

    /// <summary>
    /// 현재 표시되는 TweenStep 필드에 필요한 전체 높이를 계산
    /// </summary>
    /// <param name="property">높이를 계산할 TweenStep Property</param>
    /// <param name="label">TweenStep의 기본 Label</param>
    /// <returns>Inspector에서 사용할 Property 높이.</returns>
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight;

        if (!property.isExpanded)
            return height;

        SerializedProperty effectType = property.FindPropertyRelative("effectType");
        AddPropertyHeight(ref height, effectType);

        UITweenSequencePlayer.EffectType currentEffect = (UITweenSequencePlayer.EffectType)effectType.enumValueIndex;

        switch (currentEffect)
        {
            case UITweenSequencePlayer.EffectType.Scale:
                AddTargetPropertyHeights(ref height, property);
                AddScalePropertyHeights(ref height, property);
                break;

            case UITweenSequencePlayer.EffectType.Fade:
                AddTargetPropertyHeights(ref height, property);
                AddFadePropertyHeights(ref height, property);
                break;

            case UITweenSequencePlayer.EffectType.Delay:
                AddPropertyHeight(ref height, property.FindPropertyRelative("duration"));
                break;

            case UITweenSequencePlayer.EffectType.Callback:
                AddPropertyHeight(ref height, property.FindPropertyRelative("callBack"));
                break;
        }

        return height;
    }

    /// <summary>
    /// Scale과 Fade가 공통으로 사용하는 Mode와 Target 필드를 표시
    /// </summary>
    /// <param name="currentRect">다음 필드를 그릴 영역</param>
    /// <param name="property">TweenStep Property</param>
    private void DrawTargetProperties(ref Rect currentRect, SerializedProperty property)
    {
        DrawProperty(ref currentRect, property.FindPropertyRelative("stepMode"));

        SerializedProperty targetType = property.FindPropertyRelative("targetType");
        DrawProperty(ref currentRect, targetType);

        if (targetType.enumValueIndex == (int)UITweenSequencePlayer.TargetType.Direct)
            DrawProperty(ref currentRect, property.FindPropertyRelative("directTarget"));
    }

    /// <summary>
    /// Scale 효과에 필요한 필드를 표시
    /// </summary>
    /// <param name="currentRect">다음 필드를 그릴 영역</param>
    /// <param name="property">TweenStep Property</param>
    private void DrawScaleProperties(ref Rect currentRect, SerializedProperty property)
    {
        SerializedProperty useStartValue = property.FindPropertyRelative("useStartValue");
        DrawProperty(ref currentRect, useStartValue);

        if (useStartValue.boolValue)
            DrawProperty(ref currentRect, property.FindPropertyRelative("startScale"));

        DrawProperty(ref currentRect, property.FindPropertyRelative("targetScale"));
        DrawProperty(ref currentRect, property.FindPropertyRelative("duration"));
        DrawProperty(ref currentRect, property.FindPropertyRelative("ease"));
    }

    /// <summary>
    /// Fade 효과에 필요한 필드를 표시
    /// </summary>
    /// <param name="currentRect">다음 필드를 그릴 영역</param>
    /// <param name="property">TweenStep Property</param>
    private void DrawFadeProperties(ref Rect currentRect, SerializedProperty property)
    {
        SerializedProperty useStartValue = property.FindPropertyRelative("useStartValue");
        DrawProperty(ref currentRect, useStartValue);

        if (useStartValue.boolValue)
            DrawProperty(ref currentRect, property.FindPropertyRelative("startAlpha"));

        DrawProperty(ref currentRect, property.FindPropertyRelative("targetAlpha"));
        DrawProperty(ref currentRect, property.FindPropertyRelative("duration"));
        DrawProperty(ref currentRect, property.FindPropertyRelative("ease"));
    }

    /// <summary>
    /// Scale과 Fade가 공통으로 사용하는 필드 높이를 더함
    /// </summary>
    /// <param name="height">현재까지 계산된 높이</param>
    /// <param name="property">TweenStep Property</param>
    private void AddTargetPropertyHeights(ref float height, SerializedProperty property)
    {
        AddPropertyHeight(ref height, property.FindPropertyRelative("stepMode"));

        SerializedProperty targetType = property.FindPropertyRelative("targetType");
        AddPropertyHeight(ref height, targetType);

        if (targetType.enumValueIndex == (int)UITweenSequencePlayer.TargetType.Direct)
            AddPropertyHeight(ref height, property.FindPropertyRelative("directTarget"));
    }

    /// <summary>
    /// Scale 효과에 필요한 필드 높이를 더함
    /// </summary>
    /// <param name="height">현재까지 계산된 높이</param>
    /// <param name="property">TweenStep Property</param>
    private void AddScalePropertyHeights(ref float height, SerializedProperty property)
    {
        SerializedProperty useStartValue = property.FindPropertyRelative("useStartValue");
        AddPropertyHeight(ref height, useStartValue);

        if (useStartValue.boolValue)
            AddPropertyHeight(ref height, property.FindPropertyRelative("startScale"));

        AddPropertyHeight(ref height, property.FindPropertyRelative("targetScale"));
        AddPropertyHeight(ref height, property.FindPropertyRelative("duration"));
        AddPropertyHeight(ref height, property.FindPropertyRelative("ease"));
    }

    /// <summary>
    /// Fade 효과에 필요한 필드 높이를 더함
    /// </summary>
    /// <param name="height">현재까지 계산된 높이</param>
    /// <param name="property">TweenStep Property</param>
    private void AddFadePropertyHeights(ref float height, SerializedProperty property)
    {
        SerializedProperty useStartValue = property.FindPropertyRelative("useStartValue");
        AddPropertyHeight(ref height, useStartValue);

        if (useStartValue.boolValue)
            AddPropertyHeight(ref height, property.FindPropertyRelative("startAlpha"));

        AddPropertyHeight(ref height, property.FindPropertyRelative("targetAlpha"));
        AddPropertyHeight(ref height, property.FindPropertyRelative("duration"));
        AddPropertyHeight(ref height, property.FindPropertyRelative("ease"));
    }

    /// <summary>
    /// SerializedProperty를 현재 위치에 표시하고 다음 위치로 이동
    /// </summary>
    /// <param name="currentRect">현재 필드를 그릴 영역</param>
    /// <param name="property">표시할 SerializedProperty</param>
    private void DrawProperty(ref Rect currentRect, SerializedProperty property)
    {
        float propertyHeight = EditorGUI.GetPropertyHeight(property, true);
        currentRect.height = propertyHeight;

        EditorGUI.PropertyField(currentRect, property, true);

        currentRect.y += propertyHeight + EditorGUIUtility.standardVerticalSpacing;
    }

    /// <summary>
    /// SerializedProperty 하나의 높이와 여백을 전체 높이에 더한다.
    /// </summary>
    /// <param name="height">현재까지 계산된 높이</param>
    /// <param name="property">높이를 더할 SerializedProperty</param>
    private void AddPropertyHeight(ref float height, SerializedProperty property)
    {
        height += EditorGUIUtility.standardVerticalSpacing;
        height += EditorGUI.GetPropertyHeight(property, true);
    }

    /// <summary>
    /// 배열 Element Label을 읽기 쉬운 Step Label로 변환
    /// </summary>
    /// <param name="property">Label을 만들 TweenStep Property</param>
    /// <returns>Step 번호가 포함된 Label</returns>
    private GUIContent GetStepLabel(SerializedProperty property)
    {
        string propertyPath = property.propertyPath;
        int startIndex = propertyPath.LastIndexOf('[') + 1;
        int endIndex = propertyPath.LastIndexOf(']');

        if (startIndex <= 0 || endIndex <= startIndex)
            return new GUIContent(property.displayName);

        string stepIndex = propertyPath.Substring(startIndex, endIndex - startIndex);
        return new GUIContent($"Step {stepIndex}");
    }
}
