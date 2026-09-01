using UnityEngine;

/// <summary>
/// HUD прототипа на IMGUI. Сознательно одноразовый: задача Фазы 1 — видеть цифры
/// и жать кнопки без сборки UI-иерархии в редакторе. В Фазе 2 заменяется на uGUI
/// вместе с экранами смерти и магазина, поэтому здесь нет ни стиля, ни анимаций.
/// </summary>
public class PrototypeHud : MonoBehaviour
{
    /// Макет рисуется в этой виртуальной высоте и масштабируется под экран.
    private const float ReferenceHeight = 1280f;

    public GameRunner runner;
    public EnemySpawner spawner;
    public UpgradeSystem upgrades;
    public MetaProgression meta;

    /// Витрина перманентных улучшений — накладка над экраном выбора,
    /// а не отдельное состояние игры: забег при этом не начат и ничего не тикает.
    private bool _showShop;

    /// Превью для экрана выбора. Кешируем: CreateDefault создаёт ScriptableObject,
    /// а OnGUI вызывается по несколько раз за кадр.
    private static readonly PathogenType[] SelectableTypes =
    {
        PathogenType.Virus, PathogenType.Bacteria, PathogenType.Fungus, PathogenType.Parasite
    };

    private static readonly string[] SelectableHints =
    {
        "Вспышка — убитые враги заражаются и бьют своих",
        "Биоплёнка — щит поглощает один удар, затем восстанавливается",
        "Споры — попадания оставляют тлеющие зоны урона",
        "Прятки — смертельный удар превращается в 2с невидимости"
    };

    private PathogenData[] _previews;

    /// Отладочный тумблер: начать забег сразу с босс-уровня, не проходя биом.
    private bool _startAtBoss;

    private GUIStyle _title;
    private GUIStyle _body;
    private GUIStyle _button;
    private GUIStyle _upgradeButton;
    private bool _stylesReady;

    private float _width;
    private float _height;

    private void OnGUI()
    {
        if (runner == null)
        {
            return;
        }

        float scale = Screen.height / ReferenceHeight;
        _width = Screen.width / scale;
        _height = ReferenceHeight;

        Matrix4x4 previous = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

        EnsureStyles();

        switch (runner.State)
        {
            case GameState.PathogenSelect:
                if (_showShop)
                {
                    DrawShop();
                }
                else
                {
                    DrawPathogenSelect();
                }
                break;
            case GameState.Playing:
                DrawCombatHud();
                break;
            case GameState.UpgradeChoice:
                DrawCombatHud();
                DrawUpgradeChoice();
                break;
            case GameState.GameOver:
                DrawGameOver();
                break;
        }

        GUI.matrix = previous;
    }

    private void EnsureStyles()
    {
        if (_stylesReady)
        {
            return;
        }

        _title = new GUIStyle(GUI.skin.label)
        {
            fontSize = 40,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };

        _body = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            wordWrap = true
        };

        _button = new GUIStyle(GUI.skin.button) { fontSize = 30 };

        _upgradeButton = new GUIStyle(GUI.skin.button)
        {
            fontSize = 26,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            padding = new RectOffset(16, 16, 12, 12)
        };

        _stylesReady = true;
    }

    // --- Экраны ---

    private void DrawPathogenSelect()
    {
        GUI.Label(new Rect(0f, 96f, _width, 60f), "ПАТОГЕН vs ИММУННАЯ СИСТЕМА", _title);

        if (meta != null && meta.Progress != null)
        {
            GUI.Label(new Rect(40f, 150f, _width - 240f, 40f),
                $"Биомасса: {meta.Progress.biomass}", _body);

            if (GUI.Button(new Rect(_width - 240f, 144f, 200f, 52f), "Улучшения", _body))
            {
                _showShop = true;
            }
        }
        GUI.Label(new Rect(40f, 206f, _width - 80f, 80f),
            "Биом «Кровоток»\nВыберите патоген:", _body);

        if (_previews == null)
        {
            _previews = new PathogenData[SelectableTypes.Length];
            for (int i = 0; i < SelectableTypes.Length; i++)
            {
                _previews[i] = PathogenData.CreateDefault(SelectableTypes[i]);
            }
        }

        float buttonHeight = 110f;
        float y = 320f;

        for (int i = 0; i < SelectableTypes.Length; i++)
        {
            PathogenData preview = _previews[i];
            var area = new Rect(40f, y, _width - 80f, buttonHeight);

            Color previousColor = GUI.backgroundColor;
            GUI.backgroundColor = preview.bodyColor;
            bool pressed = GUI.Button(area, string.Empty, _button);
            GUI.backgroundColor = previousColor;

            GUI.Label(new Rect(area.x + 20f, area.y + 12f, area.width - 40f, 36f), preview.pathogenName, _title);
            GUI.Label(new Rect(area.x + 20f, area.y + 54f, area.width - 40f, 48f), SelectableHints[i], _body);

            if (pressed)
            {
                runner.StartRun(preview, _startAtBoss ? runner.FirstBossLevelIndex : 0);
            }

            y += buttonHeight + 20f;
        }

        // Отдельная кнопка, а не GUI.Toggle: тумблер стилем label
        // рисуется как простая строка текста — по нему не видно, что он вообще нажимается.
        var debugArea = new Rect(40f, y + 16f, _width - 80f, 70f);

        Color previousBackground = GUI.backgroundColor;
        GUI.backgroundColor = _startAtBoss ? new Color(0.90f, 0.45f, 0.45f) : new Color(0.45f, 0.45f, 0.50f);

        if (GUI.Button(debugArea, _startAtBoss
                ? "СТАРТ С БОССА: ВКЛ\nнажмите патогена выше"
                : "СТАРТ С БОССА: ВЫКЛ\nнажмите, чтобы пропустить биом", _upgradeButton))
        {
            _startAtBoss = !_startAtBoss;
        }

        GUI.backgroundColor = previousBackground;

        GUI.Label(new Rect(40f, y + 96f, _width - 80f, 100f),
            "Управление: тяните палец (или ЛКМ) влево-вправо. Атака автоматическая.", _body);
    }

    /// <summary>
    /// Витрина перманентных улучшений. В Фазе 2 это ещё заглушка без визуального
    /// стиля — важно только, что трата валюты и её эффект реально проверяются на плейтесте.
    /// </summary>
    private void DrawShop()
    {
        if (meta == null || meta.Progress == null)
        {
            _showShop = false;
            return;
        }

        GUI.Label(new Rect(0f, 96f, _width, 60f), "ПЕРМАНЕНТНЫЕ УЛУЧШЕНИЯ", _title);
        GUI.Label(new Rect(40f, 156f, _width - 80f, 40f), $"Биомасса: {meta.Progress.biomass}", _body);

        var upgradeList = meta.Upgrades;
        float y = 210f;
        const float rowHeight = 104f;

        for (int i = 0; i < upgradeList.Count; i++)
        {
            PermanentUpgrade upgrade = upgradeList[i];
            int level = meta.LevelOf(upgrade);
            bool maxed = upgrade.IsMaxed(level);
            bool affordable = meta.CanAfford(upgrade);

            string label = maxed
                ? $"{upgrade.Title}  [{level}/{upgrade.MaxLevel}]\nмаксимум"
                : $"{upgrade.Title}  [{level}/{upgrade.MaxLevel}]\n{upgrade.PerLevelDescription} · цена {upgrade.CostForNextLevel(level)}";

            Color previousBackground = GUI.backgroundColor;
            if (maxed)
            {
                GUI.backgroundColor = new Color(0.40f, 0.42f, 0.45f);
            }
            else if (!affordable)
            {
                // Недоступное должно быть видно, а не скрыто: это цель следующего забега.
                GUI.backgroundColor = new Color(0.55f, 0.40f, 0.40f);
            }
            else
            {
                GUI.backgroundColor = new Color(0.45f, 0.75f, 0.50f);
            }

            bool pressed = GUI.Button(new Rect(40f, y, _width - 80f, rowHeight - 12f), label, _upgradeButton);
            GUI.backgroundColor = previousBackground;

            if (pressed && !maxed)
            {
                meta.TryPurchase(upgrade);
            }

            y += rowHeight;
        }

        if (GUI.Button(new Rect(40f, y + 12f, _width - 80f, 84f), "Назад", _button))
        {
            _showShop = false;
        }

        // Нужно именно для плейтестов Фазы 2: без сброса невозможно посмотреть,
        // как игра выглядит для человека, который запустил её впервые.
        if (GUI.Button(new Rect(40f, y + 168f, _width - 80f, 64f), "Сбросить прогресс (отладка)", _body))
        {
            meta.ResetProgress();
        }

        GUI.Label(new Rect(40f, y + 108f, _width - 80f, 60f),
            $"Забегов: {meta.Progress.totalRuns} · всего убито: {meta.Progress.totalKills} · боссов: {meta.Progress.bossesDefeated}",
            _body);
    }

    private void DrawCombatHud()
    {
        PlayerController player = runner.Player;
        if (player == null)
        {
            return;
        }

        DrawHealthBar(new Rect(24f, 24f, _width - 48f, 34f), player.Health);

        string level = runner.CurrentLevel != null ? runner.CurrentLevel.levelName : "-";
        GUI.Label(new Rect(24f, 66f, _width - 48f, 120f),
            $"Уровень {runner.LevelNumber + 1} · {level}\n" +
            $"Убито за уровень: {FindKills()} · всего: {runner.TotalKills}\n" +
            $"На поле: {Battlefield.ThreatCount}",
            _body);

        DrawBossPanel();

        PathogenAbility ability = player.Ability;
        if (ability != null)
        {
            GUI.Label(new Rect(24f, _height - 120f, _width - 48f, 70f), ability.StatusLine, _body);
        }

        if (runner.Stats != null && runner.Stats.TakenMutations.Count > 0)
        {
            GUI.Label(new Rect(24f, _height - 74f, _width - 48f, 60f),
                "Мутации: " + string.Join(", ", runner.Stats.TakenMutations), _body);
        }
    }

    /// <summary>
    /// Полоска на сегмент, а не одна общая: игрок должен видеть, что цель составная
    /// и что каждый снятый сегмент убирает конкретную атаку.
    /// </summary>
    private void DrawBossPanel()
    {
        Boss boss = spawner != null ? spawner.ActiveBoss : null;
        if (boss == null)
        {
            return;
        }

        float y = 200f;
        GUI.Label(new Rect(24f, y, _width - 48f, 34f), boss.Data.bossName, _body);
        y += 34f;

        var segments = boss.Segments;
        for (int i = 0; i < segments.Count; i++)
        {
            BossSegment segment = segments[i];
            if (segment == null)
            {
                continue;
            }

            var bar = new Rect(24f, y, _width - 48f, 26f);
            Color previous = GUI.color;

            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(bar, Texture2D.whiteTexture);

            float fraction = segment.Health.Normalized;
            if (fraction > 0f)
            {
                GUI.color = segment.Definition.color;
                GUI.DrawTexture(new Rect(bar.x + 2f, bar.y + 2f, (bar.width - 4f) * fraction, bar.height - 4f),
                    Texture2D.whiteTexture);
            }

            GUI.color = previous;
            GUI.Label(new Rect(bar.x + 10f, bar.y - 2f, bar.width, bar.height),
                fraction > 0f ? segment.Definition.segmentName : $"{segment.Definition.segmentName} — уничтожен", _body);

            y += 30f;
        }
    }

    private void DrawUpgradeChoice()
    {
        // Затемнение под панелью выбора — иначе на плейсхолдерах не читается, что игра на паузе.
        Color previous = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.72f);
        GUI.DrawTexture(new Rect(0f, 0f, _width, _height), Texture2D.whiteTexture);
        GUI.color = previous;

        GUI.Label(new Rect(0f, 220f, _width, 60f), "УРОВЕНЬ ЗАЧИЩЕН", _title);
        GUI.Label(new Rect(40f, 285f, _width - 80f, 40f), "Выберите одно улучшение:", _body);

        var choices = runner.PendingUpgrades;
        float buttonHeight = 130f;
        float y = 350f;

        for (int i = 0; i < choices.Count; i++)
        {
            UpgradeDefinition upgrade = choices[i];
            int taken = upgrades != null ? upgrades.TakenCount(upgrade.Id) : 0;

            string label = upgrade.IsMutation
                ? $"МУТАЦИЯ · {upgrade.Title}\n{upgrade.Description}"
                : taken > 0
                    ? $"{upgrade.Title}  (взято {taken})\n{upgrade.Description}"
                    : $"{upgrade.Title}\n{upgrade.Description}";

            // Мутация должна читаться как другой класс выбора, а не как очередные +15%.
            Color previousBackground = GUI.backgroundColor;
            if (upgrade.IsMutation)
            {
                GUI.backgroundColor = new Color(0.85f, 0.45f, 0.90f);
            }

            bool pressed = GUI.Button(new Rect(40f, y, _width - 80f, buttonHeight), label, _upgradeButton);
            GUI.backgroundColor = previousBackground;

            if (pressed)
            {
                runner.ChooseUpgrade(upgrade);
                return;
            }

            y += buttonHeight + 18f;
        }
    }

    private void DrawGameOver()
    {
        Color previous = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.8f);
        GUI.DrawTexture(new Rect(0f, 0f, _width, _height), Texture2D.whiteTexture);
        GUI.color = previous;

        GUI.Label(new Rect(0f, 320f, _width, 60f), "ПАТОГЕН УНИЧТОЖЕН", _title);
        GUI.Label(new Rect(40f, 400f, _width - 80f, 160f),
            $"Патоген: {(runner.Stats != null ? runner.Stats.Source.pathogenName : "-")}\n" +
            $"Пройдено уровней: {runner.LevelNumber}\n" +
            $"Всего убито: {runner.TotalKills}\n" +
            (meta != null && meta.Progress != null
                ? $"Получено биомассы: {meta.LastRunReward} (всего {meta.Progress.biomass})"
                : ""),
            _body);

        if (GUI.Button(new Rect(40f, 600f, _width - 80f, 100f), "Заново", _button))
        {
            runner.RestartToSelect();
        }
    }

    // --- Куски ---

    private void DrawHealthBar(Rect area, Health health)
    {
        Color previous = GUI.color;

        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(area, Texture2D.whiteTexture);

        var fill = new Rect(area.x + 2f, area.y + 2f, (area.width - 4f) * health.Normalized, area.height - 4f);
        GUI.color = Color.Lerp(new Color(0.85f, 0.20f, 0.20f), new Color(0.35f, 0.85f, 0.45f), health.Normalized);
        GUI.DrawTexture(fill, Texture2D.whiteTexture);

        GUI.color = previous;
        GUI.Label(new Rect(area.x + 10f, area.y + 2f, area.width, area.height),
            $"{Mathf.CeilToInt(health.Current)} / {Mathf.CeilToInt(health.Max)}", _body);
    }

    private int FindKills()
    {
        return spawner != null ? spawner.Kills : 0;
    }
}
