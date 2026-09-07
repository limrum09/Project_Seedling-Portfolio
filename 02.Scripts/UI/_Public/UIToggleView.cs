using System;
using UnityEngine;
using UnityEngine.UI;

public interface IUISelectionHandler<T>
{
    void Select(T value);
}

public class UIToggleView : MonoBehaviour
{
    [SerializeField]
    private Toggle toggle;

    private Action OnSelectAction;

    private void Awake()
    {
        toggle.onValueChanged.AddListener(OnValueChanged);
    }

    private void OnDestroy()
    {
        toggle.onValueChanged.RemoveListener(OnValueChanged);
        Unbind();
    }

    private void OnValueChanged(bool isOn)
    {
        if (!isOn)
            return;

        OnSelectAction?.Invoke();
    }

    public void Bind<T>(T value, IUISelectionHandler<T> handler)
    {
        OnSelectAction = () => handler.Select(value);
    }

    public void SetIsOnWithoutNotify(bool isOn)
    {
        toggle.SetIsOnWithoutNotify(isOn);
    }

    public void Select()
    {
        if (toggle.isOn)
        {
            OnSelectAction?.Invoke();
            return;
        }

        toggle.isOn = true;
    }

    public void Unbind()
    {
        OnSelectAction = null;
    }
}
