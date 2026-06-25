using System;

/// <summary>
/// 全局事件总线，系统改数据后广播，UI 订阅刷新。
/// </summary>
public static class GameEventBus
{
    public static event Action OnInventoryChanged;
    public static event Action OnGoldChanged;
    public static event Action OnStatChanged;
    public static event Action OnCompanionHired;
    public static event Action OnCompanionDismissed;
    public static event Action OnCompanionOrderChanged;
    public static event Action OnTradeCompleted;
    public static event Action OnGameSaved;
    public static event Action OnGameLoaded;
    public static event Action OnPlayerDied;
    public static event Action<bool> OnPauseChanged;
    public static event Action OnSettingsChanged;

    public static void RaiseInventoryChanged() => OnInventoryChanged?.Invoke();
    public static void RaiseGoldChanged() => OnGoldChanged?.Invoke();
    public static void RaiseStatChanged() => OnStatChanged?.Invoke();
    public static void RaiseCompanionHired() => OnCompanionHired?.Invoke();
    public static void RaiseCompanionDismissed() => OnCompanionDismissed?.Invoke();
    public static void RaiseCompanionOrderChanged() => OnCompanionOrderChanged?.Invoke();
    public static void RaiseTradeCompleted() => OnTradeCompleted?.Invoke();
    public static void RaiseGameSaved() => OnGameSaved?.Invoke();
    public static void RaiseGameLoaded() => OnGameLoaded?.Invoke();
    public static void RaisePlayerDied() => OnPlayerDied?.Invoke();
    public static void RaisePauseChanged(bool paused) => OnPauseChanged?.Invoke(paused);
    public static void RaiseSettingsChanged() => OnSettingsChanged?.Invoke();
}
