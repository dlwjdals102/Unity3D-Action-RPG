using System;

public static class UIInputLock
{
    private static int _openCount;
    public static bool IsOpen => _openCount > 0;
    public static event Action<bool> OnChanged;   // 0↔1 변할 때만 발행

    public static void Push()
    {
        _openCount++;
        if (_openCount == 1) OnChanged?.Invoke(true);
    }
    public static void Pop()
    {
        if (_openCount == 0) return;
        _openCount--;
        if (_openCount == 0) OnChanged?.Invoke(false);
    }
}