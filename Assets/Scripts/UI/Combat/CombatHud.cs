using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Боевой HUD: здоровье, счётчики, полоски сегментов босса.
///
/// Строки собираются в StringBuilder и перестраиваются только при смене
/// значений. В GameHud три интерполированные строки собирались каждый кадр —
/// около трёх аллокаций на кадр в бою, ровно там, где на экране сотни объектов.
/// </summary>
public class CombatHud : UiScreen
{
    /// Сколько полосок сегментов босса держать наготове.
    private const int MaxBossBars = 8;

    private readonly GameRunner _runner;
    private readonly EnemySpawner _spawner;
    private readonly StringBuilder _builder = new StringBuilder(128);

    private readonly List<Image> _bossFills = new List<Image>();
    private readonly List<Image> _bossBackgrounds = new List<Image>();
    private readonly List<Text> _bossLabels = new List<Text>();

    private Image _healthFill;
    private Text _healthText;
    private Text _infoText;
    private Text _abilityText;
    private Text _mutationsText;
    private Text _bossName;

    // Последние показанные значения — чтобы не пересобирать строки впустую.
    private int _shownHealth = -1;
    private int _shownKills = -1;
    private int _shownThreats = -1;
    private string _shownAbility = string.Empty;

    public CombatHud(GameRunner runner, EnemySpawner spawner)
    {
        _runner = runner;
        _spawner = spawner;
    }

    protected override void OnBuild()
    {
        _healthFill = UiFactory.CreateBar("Health", Root, new Color(0.35f, 0.85f, 0.45f), out Image background);
        UiFactory.TopAnchored(background.rectTransform, 22f, UiFactory.ContentWidth, 38f);

        _healthText = UiFactory.CreateText("HealthText", background.transform, string.Empty, 22,
            TextAnchor.MiddleCenter);
        UiFactory.Stretch(_healthText.rectTransform);

        _infoText = UiFactory.CreateText("Info", Root, string.Empty, 22);
        UiFactory.TopAnchored(_infoText.rectTransform, 70f, UiFactory.ContentWidth, 110f);

        _bossName = UiFactory.CreateText("BossName", Root, string.Empty, 24,
            TextAnchor.MiddleLeft, FontStyle.Bold);
        UiFactory.TopAnchored(_bossName.rectTransform, 186f, UiFactory.ContentWidth, 32f);

        for (int i = 0; i < MaxBossBars; i++)
        {
            Image fill = UiFactory.CreateBar($"BossBar{i}", Root, Color.white, out Image barBackground);
            UiFactory.TopAnchored(barBackground.rectTransform, 222f + i * 34f, UiFactory.ContentWidth, 28f);

            Text label = UiFactory.CreateText($"BossBarLabel{i}", barBackground.transform, string.Empty, 19,
                TextAnchor.MiddleLeft);
            UiFactory.StretchWithPadding(label.rectTransform, 10f, 0f);
            label.color = new Color(0.08f, 0.08f, 0.10f);

            _bossFills.Add(fill);
            _bossBackgrounds.Add(barBackground);
            _bossLabels.Add(label);
            barBackground.gameObject.SetActive(false);
        }

        _abilityText = UiFactory.CreateText("Ability", Root, string.Empty, 22);
        UiFactory.TopAnchored(_abilityText.rectTransform, UiFactory.ReferenceResolution.y - 128f,
            UiFactory.ContentWidth, 50f);

        _mutationsText = UiFactory.CreateText("Mutations", Root, string.Empty, 20);
        UiFactory.TopAnchored(_mutationsText.rectTransform, UiFactory.ReferenceResolution.y - 76f,
            UiFactory.ContentWidth, 50f);
        _mutationsText.color = new Color(0.85f, 0.55f, 0.90f);
    }

    protected override void OnShow()
    {
        // Сброс кэша: значения прошлого узла не должны подавлять первое обновление.
        _shownHealth = -1;
        _shownKills = -1;
        _shownThreats = -1;
        _shownAbility = string.Empty;

        RefreshMutations();
    }

    protected override void OnTick()
    {
        PlayerController player = _runner.Player;
        if (player == null)
        {
            return;
        }

        RefreshHealth(player);
        RefreshInfo();
        RefreshAbility(player);
        RefreshBossBars();
    }

    private void RefreshHealth(PlayerController player)
    {
        float normalized = player.Health.Normalized;
        _healthFill.fillAmount = normalized;
        _healthFill.color = Color.Lerp(new Color(0.85f, 0.20f, 0.20f), new Color(0.35f, 0.85f, 0.45f), normalized);

        int current = Mathf.CeilToInt(player.Health.Current);
        if (current == _shownHealth)
        {
            return;
        }

        _shownHealth = current;
        _builder.Clear();
        _builder.Append(current).Append(" / ").Append(Mathf.CeilToInt(player.Health.Max));
        _healthText.text = _builder.ToString();
    }

    private void RefreshInfo()
    {
        int kills = _spawner != null ? _spawner.Kills : 0;
        int threats = Battlefield.ThreatCount;

        if (kills == _shownKills && threats == _shownThreats)
        {
            return;
        }

        _shownKills = kills;
        _shownThreats = threats;

        _builder.Clear();
        _builder.Append(_runner.CurrentNode != null ? _runner.CurrentNode.DisplayName : "-").Append('\n');
        _builder.Append("Убито: ").Append(kills).Append('\n');
        _builder.Append("На поле: ").Append(threats);
        _infoText.text = _builder.ToString();
    }

    private void RefreshAbility(PlayerController player)
    {
        string status = player.Ability != null ? player.Ability.StatusLine : string.Empty;
        if (status == _shownAbility)
        {
            return;
        }

        _shownAbility = status;
        _abilityText.text = status;
    }

    private void RefreshMutations()
    {
        PlayerStats stats = _runner.Stats;
        _mutationsText.text = stats != null && stats.TakenMutations.Count > 0
            ? "Мутации: " + string.Join(", ", stats.TakenMutations)
            : string.Empty;
    }

    /// <summary>
    /// Полоска на сегмент, а не одна общая: игрок должен видеть, что цель
    /// составная и что каждый снятый сегмент убирает конкретную атаку.
    /// </summary>
    private void RefreshBossBars()
    {
        Boss boss = _spawner != null ? _spawner.ActiveBoss : null;

        if (boss == null)
        {
            if (_bossName.text.Length > 0)
            {
                _bossName.text = string.Empty;
                for (int i = 0; i < _bossBackgrounds.Count; i++)
                {
                    _bossBackgrounds[i].gameObject.SetActive(false);
                }
            }
            return;
        }

        _bossName.text = boss.Data.bossName;
        IReadOnlyList<BossSegment> segments = boss.Segments;

        for (int i = 0; i < _bossBackgrounds.Count; i++)
        {
            bool has = i < segments.Count && segments[i] != null;
            _bossBackgrounds[i].gameObject.SetActive(has);
            if (!has)
            {
                continue;
            }

            BossSegment segment = segments[i];
            float fraction = segment.Health.Normalized;

            _bossFills[i].fillAmount = fraction;
            _bossFills[i].color = segment.Definition.color;
            _bossLabels[i].text = fraction > 0f
                ? segment.Definition.segmentName
                : segment.Definition.segmentName + " — уничтожен";
        }
    }

    /// <summary>Позвать после взятия мутации, чтобы строка обновилась вне тика.</summary>
    public void NotifyMutationsChanged() => RefreshMutations();
}
