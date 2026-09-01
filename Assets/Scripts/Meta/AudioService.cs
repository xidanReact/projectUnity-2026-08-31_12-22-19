using UnityEngine;

/// <summary>
/// Громкости из настроек. AudioMixer здесь не подходит: это ассет проекта,
/// его нельзя создать в рантайме, а сцена у нас собирается кодом. Поэтому общая
/// громкость идёт в AudioListener, а музыка и эффекты отдаются наружу
/// множителями — их будут читать источники звука, когда те появятся в Фазе 2.5.
/// </summary>
public static class AudioService
{
    public static float MasterVolume { get; private set; } = 1f;
    public static float MusicVolume { get; private set; } = 1f;
    public static float SfxVolume { get; private set; } = 1f;

    public static void Apply(GameSettings settings)
    {
        if (settings == null)
        {
            settings = new GameSettings();
        }

        MasterVolume = Mathf.Clamp01(settings.masterVolume);
        MusicVolume = Mathf.Clamp01(settings.musicVolume);
        SfxVolume = Mathf.Clamp01(settings.sfxVolume);

        AudioListener.volume = MasterVolume;
    }
}
