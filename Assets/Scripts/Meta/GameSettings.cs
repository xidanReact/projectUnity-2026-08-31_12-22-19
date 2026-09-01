using System;

/// <summary>
/// Пользовательские настройки. Лежат в сейве, а не в PlayerPrefs: в Фазе 4
/// прогресс переезжает на бэкенд целиком, и настройки должны поехать вместе с ним.
/// </summary>
[Serializable]
public class GameSettings
{
    public float masterVolume = 1f;
    public float musicVolume = 1f;
    public float sfxVolume = 1f;
    public string playerName = string.Empty;
}
