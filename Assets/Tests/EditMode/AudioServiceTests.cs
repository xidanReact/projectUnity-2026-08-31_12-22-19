using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Проводка звука. Звуков в проекте ещё нет — проверяется ровно то, что
/// сохранённые значения доезжают до движка и не выходят за границы.
/// </summary>
public class AudioServiceTests
{
    [TearDown]
    public void TearDown()
    {
        AudioService.Apply(new GameSettings());
    }

    [Test]
    public void Apply_КладётОбщуюГромкостьВAudioListener()
    {
        AudioService.Apply(new GameSettings { masterVolume = 0.25f });

        Assert.AreEqual(0.25f, AudioListener.volume, 0.001f);
        Assert.AreEqual(0.25f, AudioService.MasterVolume, 0.001f);
    }

    [Test]
    public void Apply_ЗапоминаетГромкостиМузыкиИЭффектов()
    {
        AudioService.Apply(new GameSettings { musicVolume = 0.4f, sfxVolume = 0.7f });

        Assert.AreEqual(0.4f, AudioService.MusicVolume, 0.001f);
        Assert.AreEqual(0.7f, AudioService.SfxVolume, 0.001f);
    }

    [Test]
    public void Apply_ЗажимаетЗначенияВДиапазон()
    {
        AudioService.Apply(new GameSettings { masterVolume = 5f, musicVolume = -3f });

        Assert.AreEqual(1f, AudioService.MasterVolume, 0.001f);
        Assert.AreEqual(0f, AudioService.MusicVolume, 0.001f);
    }

    [Test]
    public void Apply_НаNullНеПадаетИСтавитЗначенияПоУмолчанию()
    {
        Assert.DoesNotThrow(() => AudioService.Apply(null));
        Assert.AreEqual(1f, AudioService.MasterVolume, 0.001f);
    }
}
