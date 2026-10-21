using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Windows.Media.Imaging;
using MAI.Models;
using MAI.Services;

namespace MAI;

public class CriticalPointDisplayItem
{
    public string Alt1Name { get; set; } = string.Empty;
    public string Alt2Name { get; set; } = string.Empty;
    public string WeightTText { get; set; } = string.Empty;
    public string PriorityPText { get; set; } = string.Empty;
    public string ChangeTypeText { get; set; } = string.Empty;
    public string EventDescription { get; set; } = string.Empty;
}

public class SensitivityLegendItem
{
    public string Label { get; set; } = string.Empty;
    public Brush ColorBrush { get; set; } = Brushes.Blue;
}

public class SensitivityDynamicRankingDisplayItem
{
    public int Rank { get; set; }
    private string _rankText = string.Empty;
    public string RankText
    {
        get => string.IsNullOrEmpty(_rankText) ? $"#{Rank}" : _rankText;
        set => _rankText = value;
    }
    public string AlternativeId { get; set; } = string.Empty;
    public string AlternativeName { get; set; } = string.Empty;
    public double CurrentPriority { get; set; }
    public double OriginalPriority { get; set; }
    public double DeltaPriority => CurrentPriority - OriginalPriority;
    public string PriorityPercentText => ResultsWindow.Fmt($"{CurrentPriority * 100:F1}% ({CurrentPriority:F4})");
    public string DeltaText
    {
        get
        {
            string pp = LocalizationService.Get("Str_Sens_PP");
            return DeltaPriority switch
            {
                > 0.0005 => ResultsWindow.Fmt($"▲ +{DeltaPriority * 100:F1} {pp}"),
                < -0.0005 => ResultsWindow.Fmt($"▼ {DeltaPriority * 100:F1} {pp}"),
                _ => $"— 0.0 {pp}"
            };
        }
    }
    public Brush RankBackground { get; set; } = Brushes.Gray;
    public Brush DeltaBrush { get; set; } = Brushes.Gray;
    public bool IsWinner => Rank == 1;
}

public class AlternativeDisplayItem
{
    public int Rank { get; set; }
    public string RankText { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double GlobalPriority { get; set; }
    public double PriorityPercent { get; set; }
    public string PriorityPercentText { get; set; } = string.Empty;

    public Brush RankBackground { get; set; } = Brushes.Gray;
    public Brush RankForeground { get; set; } = Brushes.White;
    public Brush BarBrush { get; set; } = Brushes.Blue;
    public Brush BadgeBackground { get; set; } = Brushes.LightGray;
    public Brush BadgeForeground { get; set; } = Brushes.DarkSlateGray;
    public string BadgeText { get; set; } = string.Empty;
}

public class CriterionCardItem
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Weight { get; set; }
    public string WeightPercentText { get; set; } = string.Empty;
}

public class ConsistencyRowItem
{
    public string MatrixName { get; set; } = string.Empty;
    public int Dimension { get; set; }
    public string LambdaMax { get; set; } = string.Empty;
    public string CI { get; set; } = string.Empty;
    public string RITable { get; set; } = string.Empty;
    public string RIEmpirical { get; set; } = string.Empty;
    public string CRTable { get; set; } = string.Empty;
    public string CREmpirical { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Verdict { get; set; } = string.Empty;
}

public partial class ResultsWindow : Window
{
    internal static string Fmt(FormattableString fs) => fs.ToString(LocalizationService.ActiveCulture);

    private readonly AhpResult _result;
    private readonly List<MatrixResult> _allMatrixResults = new();

    private CriterionSensitivityResult? _currentSensitivityResult;
    private double _currentSensitivityT = 0.0;
    private bool _isUpdatingSensitivityUi = false;

    private static readonly Color[] AlternativeColors = new[]
    {
        Color.FromRgb(59, 130, 246),  // Blue (#3B82F6)
        Color.FromRgb(16, 185, 129), // Emerald (#10B981)
        Color.FromRgb(245, 158, 11), // Amber (#F59E0B)
        Color.FromRgb(239, 68, 68),  // Red (#EF4444)
        Color.FromRgb(139, 92, 246), // Purple (#8B5CF6)
        Color.FromRgb(236, 72, 153), // Pink (#EC4899)
        Color.FromRgb(20, 184, 166), // Teal (#14B8A6)
        Color.FromRgb(249, 115, 22), // Orange (#F97316)
        Color.FromRgb(99, 102, 241), // Indigo (#6366F1)
        Color.FromRgb(6, 182, 212)   // Cyan (#06B6D4)
    };

    private readonly List<Criterion>? _rawCriteria;
    private readonly List<Alternative>? _rawAlternatives;
    private readonly ComparisonMatrix? _rawCriteriaMatrix;
    private readonly Dictionary<string, ComparisonMatrix>? _rawAltMatrices;

    public ResultsWindow(
        AhpResult result,
        List<Criterion>? criteria,
        List<Alternative>? alternatives,
        ComparisonMatrix? criteriaMatrix,
        Dictionary<string, ComparisonMatrix>? altMatrices)
    {
        InitializeComponent();
        _result = result;
        _rawCriteria = criteria;
        _rawAlternatives = alternatives;
        _rawCriteriaMatrix = criteriaMatrix;
        _rawAltMatrices = altMatrices;

        if (_rawCriteria == null || _rawAlternatives == null || _rawCriteriaMatrix == null || _rawAltMatrices == null)
        {
            BtnCompareProfiles.IsEnabled = false;
            BtnCompareProfiles.ToolTip = LocalizationService.CurrentLanguage == AppLanguage.En
                ? "Comparison is available only from main window"
                : "Порівняння доступне лише з головного вікна";
        }

        InitHeaderAndBadges();
        InitRankingsList();
        InitCriteriaCards();
        InitRecommendationText();
        InitSynthesisDataGrid();
        InitConsistencyDataGrid();
        InitMatrixSelector();
        InitSensitivityAnalysis();

        ThemeService.ThemeChanged += OnThemeChanged;
        LocalizationService.LanguageChanged += OnLanguageChanged;
    }

    private void OnThemeChanged(AppTheme theme)
    {
        InitHeaderAndBadges();
        InitRankingsList();
        UpdateSensitivityDynamicState(_currentSensitivityT);
    }

    private void OnLanguageChanged(AppLanguage lang)
    {
        InitHeaderAndBadges();
        InitRankingsList();
        InitCriteriaCards();
        InitRecommendationText();
        InitSynthesisDataGrid();
        InitConsistencyDataGrid();
        InitMatrixSelector();
        int sensIdx = ComboSensitivityCriterion.SelectedIndex;
        InitSensitivityAnalysis();
        if (sensIdx >= 0 && sensIdx < ComboSensitivityCriterion.Items.Count)
        {
            ComboSensitivityCriterion.SelectedIndex = sensIdx;
        }
        else
        {
            UpdateSensitivityDynamicState(_currentSensitivityT);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        ThemeService.ThemeChanged -= OnThemeChanged;
        LocalizationService.LanguageChanged -= OnLanguageChanged;
        base.OnClosed(e);
    }

    private void InitHeaderAndBadges()
    {
        bool isEn = LocalizationService.CurrentLanguage == AppLanguage.En;
        if (_result.BestAlternative != null)
        {
            TxtWinnerName.Text = Fmt($"{_result.BestAlternative.Name} ({_result.BestAlternative.GlobalPriority:P1})");
        }
        else
        {
            TxtWinnerName.Text = isEn ? "Not determined" : "Не визначено";
        }

        string profileName = _result.Options?.ProfileName ?? (isEn ? "Custom profile" : "Користувацький профіль");

        TxtBannerSubtitle.Text = isEn
            ? Fmt($"Profile: {profileName} | Consistency threshold CRh ≤ {_result.HierarchyThreshold:P0}")
            : Fmt($"Профіль: {profileName} | Поріг узгодженості CRh ≤ {_result.HierarchyThreshold:P0}");

        TxtHierarchicalCR.Text = Fmt($"CRh = {_result.ActiveHierarchicalCR:P1} ({_result.ActiveHierarchicalCR:F3})");
        string weightDesc = _result.Options?.WeightMethod.ToString() ?? "Default";
        string lambdaDesc = _result.Options != null
            ? AhpCalculationOptions.GetLambdaMethodDescription(_result.Options.LambdaMethod)
            : "Default";
        string riLabel = _result.ActiveRiMode == RiCalculationMode.Empirical
            ? (isEn ? "empirical formula (1.98·(n-2)/n)" : "емпірична формула (1.98·(n-2)/n)")
            : (isEn ? "Saaty table" : "таблиця Сааті");
        TxtHierarchicalCR.ToolTip = isEn
            ? Fmt($"Profile: {profileName}\nWeight method: {weightDesc}\nλmax method: {lambdaDesc}\nRI calculation: {riLabel}\nConsistency threshold: {_result.HierarchyThreshold:P0}\nCRh = {_result.ActiveHierarchicalCR:P2}")
            : Fmt($"Профіль: {profileName}\nМетод ваг: {weightDesc}\nМетод λmax: {lambdaDesc}\nРозрахунок RI: {riLabel}\nПоріг узгодженості: {_result.HierarchyThreshold:P0}\nCRh = {_result.ActiveHierarchicalCR:P2}");

        if (_result.IsHierarchyConsistent)
        {
            BadgeConsistency.SetResourceReference(Border.BackgroundProperty, "PillConsistentBg");
            BadgeConsistency.SetResourceReference(Border.BorderBrushProperty, "PillConsistentBorder");
            TxtHierarchicalCR.SetResourceReference(TextBlock.ForegroundProperty, "PillConsistentFg");
        }
        else
        {
            BadgeConsistency.SetResourceReference(Border.BackgroundProperty, "PillInconsistentBg");
            BadgeConsistency.SetResourceReference(Border.BorderBrushProperty, "PillInconsistentBorder");
            TxtHierarchicalCR.SetResourceReference(TextBlock.ForegroundProperty, "PillInconsistentFg");
        }
    }

    private void InitRankingsList()
    {
        bool isDark = ThemeService.CurrentTheme == AppTheme.Dark;
        bool isEn = LocalizationService.CurrentLanguage == AppLanguage.En;
        var items = _result.RankedAlternatives.Select(a =>
        {
            double pct = a.GlobalPriority * 100.0;
            return new AlternativeDisplayItem
            {
                Rank = a.Rank,
                RankText = $"#{a.RankText}",
                Name = a.Name,
                Description = a.Description,
                GlobalPriority = a.GlobalPriority,
                PriorityPercent = pct,
                PriorityPercentText = Fmt($"{pct:F1}% ({a.GlobalPriority:F3})"),
                RankBackground = a.Rank switch
                {
                    1 => new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDark ? "#10B981" : "#059669")),
                    2 => new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDark ? "#3B82F6" : "#2563EB")),
                    3 => new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDark ? "#60A5FA" : "#3B82F6")),
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDark ? "#64748B" : "#94A3B8"))
                },
                RankForeground = Brushes.White,
                BarBrush = a.Rank switch
                {
                    1 => new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDark ? "#10B981" : "#059669")),
                    2 => new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDark ? "#3B82F6" : "#2563EB")),
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDark ? "#60A5FA" : "#60A5FA"))
                },
                BadgeBackground = a.Rank switch
                {
                    1 => new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDark ? "#064E3B" : "#DCFCE7")),
                    2 => new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDark ? "#1E3A8A" : "#DBEAFE")),
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDark ? "#334155" : "#F1F5F9"))
                },
                BadgeForeground = a.Rank switch
                {
                    1 => new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDark ? "#6EE7B7" : "#15803D")),
                    2 => new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDark ? "#93C5FD" : "#1D4ED8")),
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDark ? "#CBD5E1" : "#475569"))
                },
                BadgeText = a.RankText == "1"
                    ? (isEn ? "🏆 Optimum (1)" : "🏆 Оптимум (1)")
                    : (isEn ? $"Rank {a.RankText}" : $"{a.RankText} місце")
            };
        }).ToList();

        ListRankings.ItemsSource = items;
    }

    private void InitCriteriaCards()
    {
        var cards = _result.Criteria.Select(c => new CriterionCardItem
        {
            Name = c.Name,
            Description = c.Description,
            Weight = c.Weight,
            WeightPercentText = Fmt($"{c.Weight:P1} ({c.Weight:F3})")
        }).ToList();

        ListCriteriaCards.ItemsSource = cards;
    }

    private void InitRecommendationText()
    {
        bool isEn = LocalizationService.CurrentLanguage == AppLanguage.En;
        TxtRecommendation.Text = BuildRecommendationText(_result, isEn);
    }

    public static string BuildRecommendationText(AhpResult result, bool isEn)
    {
        if (result.RankedAlternatives.Count == 0)
        {
            return isEn ? "No data for recommendation." : "Немає даних для формування висновку.";
        }

        var rankGroups = result.RankedAlternatives
            .GroupBy(a => a.Rank)
            .OrderBy(g => g.Key)
            .ToList();

        var firstGroup = rankGroups[0].ToList();
        var secondGroup = rankGroups.Count > 1 ? rankGroups[1].ToList() : null;

        var sb = new StringBuilder();

        if (firstGroup.Count > 1)
        {
            string tiedNames = string.Join(", ", firstGroup.Select(a => isEn ? $"\"{a.Name}\"" : $"«{a.Name}»"));
            sb.AppendLine(isEn
                ? Fmt($"1. Tied leaders detected (joint 1st place): alternatives {tiedNames} received identical final global priority of {firstGroup[0].GlobalPriority:P2} ({firstGroup[0].GlobalPriority:F4}). Note: the decision model does not differentiate between these options under the current criteria. Consider introducing an additional criterion or refining pairwise comparisons.")
                : Fmt($"1. Виявлено рівність лідерів (спільне 1-ше місце): альтернативи {tiedNames} отримали однаковий підсумковий глобальний пріоритет {firstGroup[0].GlobalPriority:P2} ({firstGroup[0].GlobalPriority:F4}). Зауваження: метод аналізу ієрархій не виявив переваги між цими варіантами за заданими критеріями. Для прийняття остаточного рішення рекомендується ввести додатковий критерій або уточнити експертні судження."));

            if (secondGroup != null)
            {
                var runnerUp = secondGroup[0];
                double margin = firstGroup[0].GlobalPriority - runnerUp.GlobalPriority;
                string runnerUpNames = string.Join(", ", secondGroup.Select(a => isEn ? $"\"{a.Name}\"" : $"«{a.Name}»"));
                sb.AppendLine(isEn
                    ? Fmt($"2. Next ranking ({secondGroup[0].RankText}): alternative(s) {runnerUpNames} with priority {runnerUp.GlobalPriority:P2} (gap to leaders is {margin:P2}).")
                    : Fmt($"2. Наступне місце ({secondGroup[0].RankText}) посіли: {runnerUpNames} із пріоритетом {runnerUp.GlobalPriority:P2} (відставання від лідерів становить {margin:P2})."));
            }
        }
        else
        {
            var winner = firstGroup[0];
            sb.AppendLine(isEn
                ? Fmt($"1. The optimal alternative identified by the Analytic Hierarchy Process is \"{winner.Name}\" with a final global priority of {winner.GlobalPriority:P2} ({winner.GlobalPriority:F4}).")
                : Fmt($"1. Оптимальною альтернативою за методом аналізу ієрархій визначено «{winner.Name}» із підсумковим глобальним пріоритетом {winner.GlobalPriority:P2} ({winner.GlobalPriority:F4})."));

            if (secondGroup != null)
            {
                double margin = winner.GlobalPriority - secondGroup[0].GlobalPriority;
                if (secondGroup.Count > 1)
                {
                    string runnerUpNames = string.Join(", ", secondGroup.Select(a => isEn ? $"\"{a.Name}\"" : $"«{a.Name}»"));
                    sb.AppendLine(isEn
                        ? Fmt($"2. Runner-up rank ({secondGroup[0].RankText}) is shared by alternatives {runnerUpNames} with priority {secondGroup[0].GlobalPriority:P2} (gap is {margin:P2}).")
                        : Fmt($"2. Друге місце ({secondGroup[0].RankText}) розділили альтернативи {runnerUpNames} із пріоритетом {secondGroup[0].GlobalPriority:P2} (відрив становить {margin:P2})."));
                }
                else
                {
                    var runnerUp = secondGroup[0];
                    sb.AppendLine(isEn
                        ? Fmt($"2. Runner-up is alternative \"{runnerUp.Name}\" with priority {runnerUp.GlobalPriority:P2} (gap is {margin:P2}).")
                        : Fmt($"2. Друге місце посіла альтернатива «{runnerUp.Name}» з пріоритетом {runnerUp.GlobalPriority:P2} (відрив становить {margin:P2})."));
                }

                if (margin < 0.02)
                {
                    sb.AppendLine(isEn
                        ? "   Note: The margin between leaders is narrow (< 2%), indicating high sensitivity to fluctuations in judgments or market conditions."
                        : "   Зверніть увагу: відрив між лідерами є незначним (< 2%), що свідчить про високу чутливість розв'язку до коливань оцінок або ринкових цін.");
                }
            }
        }

        var topCriterion = result.Criteria.OrderByDescending(c => c.Weight).FirstOrDefault();
        if (topCriterion != null)
        {
            sb.AppendLine(isEn
                ? Fmt($"3. The most defining criterion in the model is \"{topCriterion.Name}\" with weight {topCriterion.Weight:P1}.")
                : Fmt($"3. Найбільш визначальним критерієм у моделі є «{topCriterion.Name}» із вагою {topCriterion.Weight:P1}."));
        }

        string activeMethodStr = result.ActiveRiMode == RiCalculationMode.Empirical
            ? (isEn ? "Saaty empirical formula" : "емпіричною формулою Сааті")
            : (isEn ? "Saaty standard table" : "табличними значеннями Сааті");
        string thresholdStr = Fmt($"{result.HierarchyThreshold:P0}");
        string passFailStr = result.IsHierarchyConsistent
            ? (isEn
                ? $"satisfying the consistency condition (CRh ≤ {thresholdStr}). Expert judgments are mutually consistent (mathematical consistency confirms logical coherence of judgments, but does not guarantee absolute truth)."
                : $"що задовольняє умову узгодженості (CRh ≤ {thresholdStr}). Судження експерта є взаємно узгодженими (зауважимо: математична узгодженість підтверджує несуперечливість логіки експерта, але не є гарантією абсолютної істинності суджень).")
            : (isEn
                ? $"exceeding acceptable threshold {thresholdStr} (CRh > {thresholdStr}). Review pairwise comparison matrices to resolve contradictory judgments."
                : $"що перевищує допустимий поріг {thresholdStr} (CRh > {thresholdStr}). Рекомендується переглянути матриці попарних порівнянь для усунення суперечностей в оцінках.");

        sb.AppendLine(isEn
            ? Fmt($"4. System hierarchy consistency (computed via: {activeMethodStr}): ratio CRh = {result.ActiveHierarchicalCR:P2} (table: {result.HierarchicalCRTable:P2}, empirical: {result.HierarchicalCREmpirical:P2}), {passFailStr}")
            : Fmt($"4. Ієрархічна узгодженість системи (розрахунок за: {activeMethodStr}): коефіцієнт CRh = {result.ActiveHierarchicalCR:P2} (табличний: {result.HierarchicalCRTable:P2}, емпіричний: {result.HierarchicalCREmpirical:P2}), {passFailStr}"));

        if (result.InconsistentMatrices.Count > 0)
        {
            sb.AppendLine(isEn
                ? $"   Warning: The following matrices violate their consistency threshold: {string.Join(", ", result.InconsistentMatrices.Select(m => $"\"{m}\""))}. It is recommended to adjust judgments in these matrices."
                : $"   Увага: виявлено матриці з порушенням власного порогу узгодженості: {string.Join(", ", result.InconsistentMatrices.Select(m => $"«{m}»"))}. Рекомендується скоригувати судження в цих матрицях.");
        }

        return sb.ToString();
    }

    private void InitSynthesisDataGrid()
    {
        bool isEn = LocalizationService.CurrentLanguage == AppLanguage.En;
        string colRank = isEn ? "Rank" : "Ранг";
        string colAlt = isEn ? "Alternative" : "Альтернатива";
        string colGlobal = isEn ? "Global Score (Pi)" : "Глобальний бал (Pi)";
        string colShare = isEn ? "Share (%)" : "Частка (%)";

        var dt = new DataTable();
        dt.Columns.Add(colRank, typeof(string));
        dt.Columns.Add(colAlt, typeof(string));

        var critColMap = new List<(string LijCol, string ScoreCol, Criterion Crit)>();

        foreach (var c in _result.Criteria)
        {
            string baseLij = $"{c.Name} (Lij)";
            string lijCol = baseLij;
            int counter = 2;
            while (dt.Columns.Contains(lijCol))
            {
                lijCol = $"{c.Name} ({counter++}) (Lij)";
            }
            dt.Columns.Add(lijCol, typeof(string));

            string baseScore = isEn ? $"{c.Name} (Contribution)" : $"{c.Name} (Внесок)";
            string scoreCol = baseScore;
            counter = 2;
            while (dt.Columns.Contains(scoreCol))
            {
                scoreCol = isEn ? $"{c.Name} ({counter++}) (Contribution)" : $"{c.Name} ({counter++}) (Внесок)";
            }
            dt.Columns.Add(scoreCol, typeof(string));

            critColMap.Add((lijCol, scoreCol, c));
        }

        dt.Columns.Add(colGlobal, typeof(string));
        dt.Columns.Add(colShare, typeof(string));

        foreach (var alt in _result.RankedAlternatives)
        {
            var row = dt.NewRow();
            row[colRank] = $"#{alt.RankText}";
            row[colAlt] = alt.Name;

            foreach (var item in critColMap)
            {
                double locPri = alt.LocalPriorities.TryGetValue(item.Crit.Id, out var lp) ? lp : 0.0;
                double wScore = alt.WeightedScores.TryGetValue(item.Crit.Id, out var ws) ? ws : 0.0;
                row[item.LijCol] = locPri.ToString("F3", LocalizationService.ActiveCulture);
                row[item.ScoreCol] = wScore.ToString("F3", LocalizationService.ActiveCulture);
            }

            row[colGlobal] = alt.GlobalPriority.ToString("F3", LocalizationService.ActiveCulture);
            row[colShare] = Fmt($"{alt.GlobalPriority * 100:F1}%");
            dt.Rows.Add(row);
        }

        GridSynthesis.ItemsSource = dt.DefaultView;
    }

    private void InitConsistencyDataGrid()
    {
        bool isEn = LocalizationService.CurrentLanguage == AppLanguage.En;
        var list = new List<ConsistencyRowItem>();
        string modeLabel = _result.ActiveRiMode == RiCalculationMode.Empirical
            ? (isEn ? "emp." : "емп.")
            : (isEn ? "tab." : "табл.");

        string statusOk = isEn ? "✅ Consistent" : "✅ Узгоджено";
        string statusFail = isEn ? "❌ Inconsistent" : "❌ Неузгоджено";

        // Матриця критеріїв
        var crCrit = _result.CriteriaMatrixResult;
        list.Add(new ConsistencyRowItem
        {
            MatrixName = isEn ? "Criteria Matrix (A)" : "Матриця критеріїв (A)",
            Dimension = crCrit.Size,
            LambdaMax = crCrit.LambdaMax.ToString("F4", LocalizationService.ActiveCulture),
            CI = crCrit.CI.ToString("F4", LocalizationService.ActiveCulture),
            RITable = crCrit.RITable.ToString("F2", LocalizationService.ActiveCulture),
            RIEmpirical = crCrit.RIEmpirical.ToString("F2", LocalizationService.ActiveCulture),
            CRTable = Fmt($"{crCrit.CRTable:P1} ({crCrit.CRTable:F3})"),
            CREmpirical = Fmt($"{crCrit.CREmpirical:P1} ({crCrit.CREmpirical:F3})"),
            Status = crCrit.IsConsistent ? statusOk : statusFail,
            Verdict = crCrit.IsConsistent ? Fmt($"CR <= {crCrit.Threshold:P0} ({modeLabel})") : Fmt($"CR > {crCrit.Threshold:P0} ({modeLabel})")
        });

        // Матриці альтернатив
        foreach (var kvp in _result.AlternativeMatrixResults)
        {
            var res = kvp.Value;
            var critObj = _result.Criteria.FirstOrDefault(c => c.Id == kvp.Key);
            string critTitle = critObj != null ? critObj.Name : kvp.Key;

            list.Add(new ConsistencyRowItem
            {
                MatrixName = isEn ? $"Alternatives: {critTitle}" : $"Альтернативи: {critTitle}",
                Dimension = res.Size,
                LambdaMax = res.LambdaMax.ToString("F4", LocalizationService.ActiveCulture),
                CI = res.CI.ToString("F4", LocalizationService.ActiveCulture),
                RITable = res.RITable.ToString("F2", LocalizationService.ActiveCulture),
                RIEmpirical = res.RIEmpirical.ToString("F2", LocalizationService.ActiveCulture),
                CRTable = Fmt($"{res.CRTable:P1} ({res.CRTable:F3})"),
                CREmpirical = Fmt($"{res.CREmpirical:P1} ({res.CREmpirical:F3})"),
                Status = res.IsConsistent ? statusOk : statusFail,
                Verdict = res.IsConsistent ? Fmt($"CR <= {res.Threshold:P0} ({modeLabel})") : Fmt($"CR > {res.Threshold:P0} ({modeLabel})")
            });
        }

        // Загальна композиційна узгодженість ієрархії
        list.Add(new ConsistencyRowItem
        {
            MatrixName = isEn ? "⭐ Total Hierarchy Composition (CRh)" : "⭐ Композиція всієї ієрархії (CRh)",
            Dimension = _result.RankedAlternatives.Count,
            LambdaMax = "-",
            CI = _result.HierarchicalCI.ToString("F4", LocalizationService.ActiveCulture),
            RITable = _result.HierarchicalRITable.ToString("F3", LocalizationService.ActiveCulture),
            RIEmpirical = _result.HierarchicalRIEmpirical.ToString("F3", LocalizationService.ActiveCulture),
            CRTable = Fmt($"{_result.HierarchicalCRTable:P1} ({_result.HierarchicalCRTable:F3})"),
            CREmpirical = Fmt($"{_result.HierarchicalCREmpirical:P1} ({_result.HierarchicalCREmpirical:F3})"),
            Status = _result.IsHierarchyConsistent ? statusOk : statusFail,
            Verdict = _result.IsHierarchyConsistent ? Fmt($"CRh <= {_result.HierarchyThreshold:P0} ({modeLabel})") : Fmt($"CRh > {_result.HierarchyThreshold:P0} ({modeLabel})")
        });

        GridConsistency.ItemsSource = list;
    }

    private void InitMatrixSelector()
    {
        bool isEn = LocalizationService.CurrentLanguage == AppLanguage.En;
        _allMatrixResults.Clear();
        ComboMatrixSelector.Items.Clear();

        _allMatrixResults.Add(_result.CriteriaMatrixResult);
        ComboMatrixSelector.Items.Add(isEn ? "Criteria Matrix" : "Матриця критеріїв");

        foreach (var c in _result.Criteria)
        {
            if (_result.AlternativeMatrixResults.TryGetValue(c.Id, out var mRes))
            {
                _allMatrixResults.Add(mRes);
                ComboMatrixSelector.Items.Add(isEn ? $"Alternatives for: {c.Name}" : $"Альтернативи за: {c.Name}");
            }
        }

        if (ComboMatrixSelector.Items.Count > 0)
        {
            ComboMatrixSelector.SelectedIndex = 0;
        }
    }

    private void ComboMatrixSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        int idx = ComboMatrixSelector.SelectedIndex;
        if (idx < 0 || idx >= _allMatrixResults.Count) return;

        bool isEn = LocalizationService.CurrentLanguage == AppLanguage.En;
        var mRes = _allMatrixResults[idx];
        TxtSelectedMatrixInfo.Text = Fmt($"{mRes.Name} | λmax = {mRes.LambdaMax:F4}, CI = {mRes.CI:F4}, CR = {mRes.ActiveCR:P2} ({mRes.ConsistencyMessage})");

        string colElement = isEn ? "Element" : "Елемент";
        string colWeight = isEn ? "Weight (wi)" : "Вага (wi)";
        string colPercent = isEn ? "Percent" : "Відсоток";

        // Показ нормалізованої матриці та ваг
        var dt = new DataTable();
        dt.Columns.Add(colElement, typeof(string));

        var colNames = new List<string>();
        for (int j = 0; j < mRes.Size; j++)
        {
            string baseHeader = j < mRes.Labels.Count ? mRes.Labels[j] : $"E{j + 1}";
            string uniqueHeader = baseHeader;
            int counter = 2;
            while (dt.Columns.Contains(uniqueHeader))
            {
                uniqueHeader = $"{baseHeader} ({counter++})";
            }
            colNames.Add(uniqueHeader);
            dt.Columns.Add(uniqueHeader, typeof(string));
        }

        dt.Columns.Add(colWeight, typeof(string));
        dt.Columns.Add(colPercent, typeof(string));

        for (int i = 0; i < mRes.Size; i++)
        {
            var row = dt.NewRow();
            row[colElement] = i < mRes.Labels.Count ? mRes.Labels[i] : $"E{i + 1}";

            for (int j = 0; j < mRes.Size; j++)
            {
                row[colNames[j]] = mRes.NormalizedMatrix[i, j].ToString("F4", LocalizationService.ActiveCulture);
            }

            row[colWeight] = mRes.Weights[i].ToString("F4", LocalizationService.ActiveCulture);
            row[colPercent] = Fmt($"{mRes.Weights[i] * 100:F2}%");
            dt.Rows.Add(row);
        }

        // Рядок сум стовпців
        var sumRow = dt.NewRow();
        sumRow[colElement] = isEn ? "Initial matrix column sum" : "Сума стовпця вихідної матриці";
        for (int j = 0; j < mRes.Size; j++)
        {
            sumRow[colNames[j]] = mRes.ColumnSums[j].ToString("F3", LocalizationService.ActiveCulture);
        }
        sumRow[colWeight] = (1.0).ToString("F4", LocalizationService.ActiveCulture);
        sumRow[colPercent] = Fmt($"{100.0:F2}%");
        dt.Rows.Add(sumRow);

        GridSelectedMatrix.ItemsSource = dt.DefaultView;
    }

    private string GenerateFullReportText()
    {
        return ProjectStorageService.GenerateFullReportText(_result, TxtRecommendation.Text);
    }

    private void BtnCopyReport_Click(object sender, RoutedEventArgs e)
    {
        bool isEn = LocalizationService.CurrentLanguage == AppLanguage.En;
        try
        {
            string report = GenerateFullReportText();
            Clipboard.SetText(report);
            MessageBox.Show(
                isEn ? "Report successfully copied to clipboard!" : "Звіт успішно скопійовано в буфер обміну!",
                isEn ? "Copy" : "Копіювання",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                (isEn ? "Copy error: " : "Помилка копіювання: ") + ex.Message,
                isEn ? "Error" : "Помилка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void BtnSaveReport_Click(object sender, RoutedEventArgs e)
    {
        bool isEn = LocalizationService.CurrentLanguage == AppLanguage.En;
        try
        {
            var sfd = new SaveFileDialog
            {
                Filter = isEn ? "Text File (*.txt)|*.txt|All Files (*.*)|*.*" : "Текстовий файл (*.txt)|*.txt|Усі файли (*.*)|*.*",
                FileName = $"AHP_Report_{DateTime.Now:yyyyMMdd_HHmm}.txt"
            };

            if (sfd.ShowDialog() == true)
            {
                File.WriteAllText(sfd.FileName, GenerateFullReportText(), Encoding.UTF8);
                MessageBox.Show(
                    (isEn ? "Report successfully saved to file:\n" : "Звіт успішно збережено у файл:\n") + sfd.FileName,
                    isEn ? "Save Report" : "Збереження звіту",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                (isEn ? "File saving error: " : "Помилка збереження файлу: ") + ex.Message,
                isEn ? "Error" : "Помилка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void DataGrid_AutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        if (e.Column is DataGridBoundColumn boundColumn)
        {
            // Bind using indexer syntax to prevent WPF PropertyPath from misinterpreting '/', '.', or '()'
            boundColumn.Binding = new Binding($"[{e.PropertyName}]");

            // Format numeric values with right alignment and cell padding
            if (e.PropertyName != "Ранг" && e.PropertyName != "Rank" &&
                e.PropertyName != "Альтернатива" && e.PropertyName != "Alternative" &&
                e.PropertyName != "Елемент" && e.PropertyName != "Element")
            {
                var style = new Style(typeof(TextBlock));
                style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));
                style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
                style.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(6, 0, 6, 0)));
                boundColumn.ElementStyle = style;
            }
        }
    }

    #region Sensitivity Analysis Tab

    private void InitSensitivityAnalysis()
    {
        bool isEn = LocalizationService.CurrentLanguage == AppLanguage.En;
        string weightLabel = isEn ? "weight" : "вага";
        ComboSensitivityCriterion.Items.Clear();
        foreach (var c in _result.Criteria)
        {
            ComboSensitivityCriterion.Items.Add(Fmt($"{c.Name} ({c.Id}, {weightLabel} {c.Weight:P1})"));
        }

        if (ComboSensitivityCriterion.Items.Count > 0)
        {
            ComboSensitivityCriterion.SelectedIndex = 0;
        }
    }

    private void ComboSensitivityCriterion_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        int idx = ComboSensitivityCriterion.SelectedIndex;
        if (idx < 0 || idx >= _result.Criteria.Count) return;

        bool isEn = LocalizationService.CurrentLanguage == AppLanguage.En;
        var selectedCrit = _result.Criteria[idx];
        _currentSensitivityResult = AhpCalculator.CalculateSensitivity(_result, selectedCrit.Id);
        _currentSensitivityT = _currentSensitivityResult.OriginalWeight;

        _isUpdatingSensitivityUi = true;
        try
        {
            TxtSensBaselineWeight.Text = isEn
                ? Fmt($"Baseline weight: {_currentSensitivityResult.OriginalWeight:P1}")
                : Fmt($"Базова вага: {_currentSensitivityResult.OriginalWeight:P1}");
            SliderSensitivityWeight.Value = _currentSensitivityResult.OriginalWeight * 100.0;
            TxtSensCurrentWeight.Text = isEn
                ? Fmt($"Current weight t = {_currentSensitivityT:P1}")
                : Fmt($"Поточна вага t = {_currentSensitivityT:P1}");
            TxtSensStabilityComment.Text = _currentSensitivityResult.StabilitySummary;

            // Інтервали лідерства
            ListSensLeadershipIntervals.ItemsSource = _currentSensitivityResult.LeadershipIntervals;

            // Таблиця критичних точок
            var points = _currentSensitivityResult.Intersections.Select(p => new CriticalPointDisplayItem
            {
                Alt1Name = p.Alt1Name,
                Alt2Name = p.Alt2Name,
                WeightTText = Fmt($"{p.WeightT:P1} ({p.WeightT:F3})"),
                PriorityPText = Fmt($"{p.PriorityP:P1} ({p.PriorityP:F3})"),
                ChangeTypeText = p.IsLeadershipChange
                    ? (isEn ? "⭐ Leader Change (#1)" : "⭐ Зміна лідера (#1)")
                    : (isEn ? "Rank Inversion" : "Перетин рангів"),
                EventDescription = p.EventDescription
            }).ToList();
            GridCriticalPoints.ItemsSource = points;

            // Легенда графіка
            var legendItems = _currentSensitivityResult.Lines.Select((l, i) => new SensitivityLegendItem
            {
                Label = $"{l.AlternativeId}: {l.AlternativeName}",
                ColorBrush = new SolidColorBrush(AlternativeColors[i % AlternativeColors.Length])
            }).ToList();
            ListSensLegend.ItemsSource = legendItems;
        }
        finally
        {
            _isUpdatingSensitivityUi = false;
        }

        UpdateSensitivityDynamicState(_currentSensitivityT);
    }

    private void SliderSensitivityWeight_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingSensitivityUi || _currentSensitivityResult == null) return;

        bool isEn = LocalizationService.CurrentLanguage == AppLanguage.En;
        _currentSensitivityT = SliderSensitivityWeight.Value / 100.0;
        TxtSensCurrentWeight.Text = isEn
            ? Fmt($"Current weight t = {_currentSensitivityT:P1}")
            : Fmt($"Поточна вага t = {_currentSensitivityT:P1}");

        UpdateSensitivityDynamicState(_currentSensitivityT);
    }

    private void BtnResetSensWeight_Click(object sender, RoutedEventArgs e)
    {
        if (_currentSensitivityResult == null) return;
        SliderSensitivityWeight.Value = _currentSensitivityResult.OriginalWeight * 100.0;
    }

    private void PlotBorder_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        RedrawSensitivityPlot(_currentSensitivityT);
    }

    private void UpdateSensitivityDynamicState(double t)
    {
        if (_currentSensitivityResult == null) return;

        bool isDark = ThemeService.CurrentTheme == AppTheme.Dark;
        bool isEn = LocalizationService.CurrentLanguage == AppLanguage.En;
        var sorted = _currentSensitivityResult.Lines
            .Select(l => new
            {
                Line = l,
                Priority = l.PriorityAt(t)
            })
            .OrderByDescending(x => x.Priority)
            .ToList();

        var dynamicList = sorted
            .Select(x =>
            {
                int rank = 1 + sorted.Count(other => other.Priority > x.Priority + 1e-9);
                bool isTied = sorted.Any(other => !ReferenceEquals(other, x) && Math.Abs(other.Priority - x.Priority) <= 1e-9);
                string rankText = isTied ? $"#{rank}=" : $"#{rank}";

                double delta = x.Priority - x.Line.OriginalPriority;
                Brush rankBg = rank switch
                {
                    1 => new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDark ? "#10B981" : "#059669")),
                    2 => new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDark ? "#3B82F6" : "#2563EB")),
                    3 => new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDark ? "#60A5FA" : "#3B82F6")),
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDark ? "#64748B" : "#94A3B8"))
                };

                Brush deltaBrush = delta switch
                {
                    > 0.0005 => new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDark ? "#34D399" : "#059669")),
                    < -0.0005 => new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDark ? "#F87171" : "#DC2626")),
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDark ? "#94A3B8" : "#64748B"))
                };

                return new SensitivityDynamicRankingDisplayItem
                {
                    Rank = rank,
                    RankText = rankText,
                    AlternativeId = x.Line.AlternativeId,
                    AlternativeName = x.Line.AlternativeName,
                    CurrentPriority = x.Priority,
                    OriginalPriority = x.Line.OriginalPriority,
                    RankBackground = rankBg,
                    DeltaBrush = deltaBrush
                };
            })
            .ToList();

        ListSensDynamicRankings.ItemsSource = dynamicList;

        if (dynamicList.Count > 0)
        {
            var top = dynamicList[0];
            TxtSensPlotLeader.Text = isEn
                ? Fmt($"Current leader: {top.AlternativeName} ({top.CurrentPriority:P1})")
                : Fmt($"Поточний лідер: {top.AlternativeName} ({top.CurrentPriority:P1})");
        }

        RedrawSensitivityPlot(t);
    }

    private void RedrawSensitivityPlot(double currentT)
    {
        if (CanvasSensitivityPlot == null || _currentSensitivityResult == null) return;

        CanvasSensitivityPlot.Children.Clear();

        double width = CanvasSensitivityPlot.ActualWidth;
        double height = CanvasSensitivityPlot.ActualHeight;

        if (width <= 50 || height <= 50) return;

        bool isDark = ThemeService.CurrentTheme == AppTheme.Dark;
        bool isEn = LocalizationService.CurrentLanguage == AppLanguage.En;
        var axisBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDark ? "#64748B" : "#94A3B8"));
        var gridBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDark ? "#334155" : "#E2E8F0"));
        var textBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDark ? "#94A3B8" : "#475569"));
        var baseLineBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDark ? "#34D399" : "#059669"));
        var currLineBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDark ? "#60A5FA" : "#2563EB"));

        double marginLeft = 48;
        double marginRight = 20;
        double marginTop = 20;
        double marginBottom = 32;

        double pw = width - marginLeft - marginRight;
        double ph = height - marginTop - marginBottom;

        if (pw <= 10 || ph <= 10) return;

        // Визначаємо максимальний пріоритет для шкали Y
        double maxPriorityVal = _currentSensitivityResult.Lines
            .SelectMany(l => new[] { l.Beta, l.Alpha + l.Beta })
            .DefaultIfEmpty(0.4)
            .Max();

        double maxY = Math.Max(0.40, Math.Ceiling(maxPriorityVal * 10.0) / 10.0);
        if (maxY > 1.0) maxY = 1.0;

        // Функції трансформації координат
        double ToX(double t) => marginLeft + Math.Clamp(t, 0.0, 1.0) * pw;
        double ToY(double p) => marginTop + (1.0 - Math.Clamp(p / maxY, 0.0, 1.0)) * ph;

        // 1. Горизонтальні лінії сітки та мітки осі Y
        int yDivisions = 5;
        for (int i = 0; i <= yDivisions; i++)
        {
            double pVal = (maxY / yDivisions) * i;
            double y = ToY(pVal);

            var gridLine = new System.Windows.Shapes.Line
            {
                X1 = marginLeft,
                Y1 = y,
                X2 = marginLeft + pw,
                Y2 = y,
                Stroke = gridBrush,
                StrokeThickness = 1,
                StrokeDashArray = i == 0 ? null : new DoubleCollection { 2, 2 }
            };
            CanvasSensitivityPlot.Children.Add(gridLine);

            var tb = new TextBlock
            {
                Text = $"{pVal * 100:F0}%",
                FontSize = 10,
                Foreground = textBrush,
                Width = 40,
                TextAlignment = TextAlignment.Right
            };
            Canvas.SetLeft(tb, marginLeft - 44);
            Canvas.SetTop(tb, y - 7);
            CanvasSensitivityPlot.Children.Add(tb);
        }

        // 2. Вертикальні лінії сітки та мітки осі X
        for (int i = 0; i <= 10; i += 2)
        {
            double tVal = i / 10.0;
            double x = ToX(tVal);

            var gridLine = new System.Windows.Shapes.Line
            {
                X1 = x,
                Y1 = marginTop,
                X2 = x,
                Y2 = marginTop + ph,
                Stroke = gridBrush,
                StrokeThickness = 1,
                StrokeDashArray = (i == 0 || i == 10) ? null : new DoubleCollection { 2, 2 }
            };
            CanvasSensitivityPlot.Children.Add(gridLine);

            var tb = new TextBlock
            {
                Text = $"{i * 10}%",
                FontSize = 10,
                Foreground = textBrush,
                Width = 36,
                TextAlignment = TextAlignment.Center
            };
            Canvas.SetLeft(tb, x - 18);
            Canvas.SetTop(tb, marginTop + ph + 6);
            CanvasSensitivityPlot.Children.Add(tb);
        }

        // 3. Основні осі графіка
        var xAxis = new System.Windows.Shapes.Line
        {
            X1 = marginLeft,
            Y1 = marginTop + ph,
            X2 = marginLeft + pw,
            Y2 = marginTop + ph,
            Stroke = axisBrush,
            StrokeThickness = 1.5
        };
        CanvasSensitivityPlot.Children.Add(xAxis);

        var yAxis = new System.Windows.Shapes.Line
        {
            X1 = marginLeft,
            Y1 = marginTop,
            X2 = marginLeft,
            Y2 = marginTop + ph,
            Stroke = axisBrush,
            StrokeThickness = 1.5
        };
        CanvasSensitivityPlot.Children.Add(yAxis);

        // 4. Вертикальна лінія базової ваги w_k^0
        double baseX = ToX(_currentSensitivityResult.OriginalWeight);
        var baseLine = new System.Windows.Shapes.Line
        {
            X1 = baseX,
            Y1 = marginTop,
            X2 = baseX,
            Y2 = marginTop + ph,
            Stroke = baseLineBrush,
            StrokeThickness = 1.8,
            StrokeDashArray = new DoubleCollection { 4, 3 },
            ToolTip = isEn
                ? Fmt($"Baseline criterion weight w_k^0 = {_currentSensitivityResult.OriginalWeight:P1}")
                : Fmt($"Базова вага критерію w_k^0 = {_currentSensitivityResult.OriginalWeight:P1}")
        };
        CanvasSensitivityPlot.Children.Add(baseLine);

        // 5. Вертикальна лінія поточної ваги t
        double currX = ToX(currentT);
        var currLine = new System.Windows.Shapes.Line
        {
            X1 = currX,
            Y1 = marginTop,
            X2 = currX,
            Y2 = marginTop + ph,
            Stroke = currLineBrush,
            StrokeThickness = 2.2,
            ToolTip = isEn
                ? Fmt($"Current weight t = {currentT:P1}")
                : Fmt($"Поточна вага t = {currentT:P1}")
        };
        CanvasSensitivityPlot.Children.Add(currLine);

        // 6. Лінії пріоритетів для кожної альтернативи
        for (int i = 0; i < _currentSensitivityResult.Lines.Count; i++)
        {
            var l = _currentSensitivityResult.Lines[i];
            var color = AlternativeColors[i % AlternativeColors.Length];
            var lineBrush = new SolidColorBrush(color);

            double x0 = ToX(0.0);
            double y0 = ToY(l.PriorityAt(0.0));
            double x1 = ToX(1.0);
            double y1 = ToY(l.PriorityAt(1.0));

            var sensLine = new System.Windows.Shapes.Line
            {
                X1 = x0,
                Y1 = y0,
                X2 = x1,
                Y2 = y1,
                Stroke = lineBrush,
                StrokeThickness = 2.5,
                ToolTip = isEn
                    ? Fmt($"{l.AlternativeId} ({l.AlternativeName})\nP(t) = {l.Alpha:+0.000;-0.000}·t + {l.Beta:F3}\nAt t={currentT:P1}: P={l.PriorityAt(currentT):P1}")
                    : Fmt($"{l.AlternativeId} ({l.AlternativeName})\nP(t) = {l.Alpha:+0.000;-0.000}·t + {l.Beta:F3}\nПри t={currentT:P1}: P={l.PriorityAt(currentT):P1}")
            };
            CanvasSensitivityPlot.Children.Add(sensLine);

            // Маркер поточної точки на лінії альтернативи
            double cy = ToY(l.PriorityAt(currentT));
            var dot = new System.Windows.Shapes.Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = lineBrush,
                Stroke = Brushes.White,
                StrokeThickness = 1.5,
                ToolTip = Fmt($"{l.AlternativeName}: P({currentT:P1}) = {l.PriorityAt(currentT):P1}")
            };
            Canvas.SetLeft(dot, currX - 4);
            Canvas.SetTop(dot, cy - 4);
            CanvasSensitivityPlot.Children.Add(dot);
        }

        // 7. Маркери критичних точок перетину
        foreach (var pt in _currentSensitivityResult.Intersections)
        {
            if (!pt.IsInDomain) continue;

            double ix = ToX(pt.WeightT);
            double iy = ToY(pt.PriorityP);

            if (pt.IsLeadershipChange)
            {
                var star = new System.Windows.Shapes.Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = Brushes.Gold,
                    Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B45309")),
                    StrokeThickness = 1.5,
                    ToolTip = isEn
                        ? Fmt($"⭐ Leader change point:\n{pt.Alt1Name} = {pt.Alt2Name}\nCritical weight t* = {pt.WeightT:P1}, P* = {pt.PriorityP:P1}")
                        : Fmt($"⭐ Точка зміни переможця:\n{pt.Alt1Name} = {pt.Alt2Name}\nКритична вага t* = {pt.WeightT:P1}, P* = {pt.PriorityP:P1}")
                };
                Canvas.SetLeft(star, ix - 5);
                Canvas.SetTop(star, iy - 5);
                CanvasSensitivityPlot.Children.Add(star);
            }
            else
            {
                var smallDot = new System.Windows.Shapes.Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = textBrush,
                    Opacity = 0.7,
                    ToolTip = isEn
                        ? Fmt($"Intersection point: {pt.Alt1Name} = {pt.Alt2Name} at t* = {pt.WeightT:P1}")
                        : Fmt($"Точка перетину: {pt.Alt1Name} = {pt.Alt2Name} при t* = {pt.WeightT:P1}")
                };
                Canvas.SetLeft(smallDot, ix - 3);
                Canvas.SetTop(smallDot, iy - 3);
                CanvasSensitivityPlot.Children.Add(smallDot);
            }
        }
    }

    #endregion

    private void BtnExport_Click(object sender, RoutedEventArgs e)
    {
        if (BtnExport.ContextMenu != null)
        {
            BtnExport.ContextMenu.PlacementTarget = BtnExport;
            BtnExport.ContextMenu.IsOpen = true;
        }
    }

    private void MenuExportCsv_Click(object sender, RoutedEventArgs e)
    {
        bool isEn = LocalizationService.CurrentLanguage == AppLanguage.En;
        var sfd = new SaveFileDialog
        {
            Title = isEn ? "Save report in CSV format..." : "Зберегти звіт у форматі CSV...",
            Filter = isEn ? "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*" : "Файли CSV (*.csv)|*.csv|Усі файли (*.*)|*.*",
            FileName = "ahp_report.csv"
        };
        if (sfd.ShowDialog(this) == true)
        {
            try
            {
                var tables = ReportTableBuilder.Build(_result);
                ReportExporter.SaveCsv(sfd.FileName, tables);
                TxtResultsStatus.Text = (isEn ? "Report successfully saved to CSV file: " : "Звіт успішно збережено у файл CSV: ") + System.IO.Path.GetFileName(sfd.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show((isEn ? "CSV export error: " : "Помилка експорту CSV: ") + ex.Message, isEn ? "Error" : "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void MenuExportMarkdown_Click(object sender, RoutedEventArgs e)
    {
        bool isEn = LocalizationService.CurrentLanguage == AppLanguage.En;
        var sfd = new SaveFileDialog
        {
            Title = isEn ? "Save report in Markdown format..." : "Зберегти звіт у форматі Markdown...",
            Filter = isEn ? "Markdown Files (*.md)|*.md|All Files (*.*)|*.*" : "Файли Markdown (*.md)|*.md|Усі файли (*.*)|*.*",
            FileName = "ahp_report.md"
        };
        if (sfd.ShowDialog(this) == true)
        {
            try
            {
                var tables = ReportTableBuilder.Build(_result);
                ReportExporter.SaveMarkdown(sfd.FileName, tables);
                TxtResultsStatus.Text = (isEn ? "Report successfully saved to Markdown file: " : "Звіт успішно збережено у файл Markdown: ") + System.IO.Path.GetFileName(sfd.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show((isEn ? "Markdown export error: " : "Помилка експорту Markdown: ") + ex.Message, isEn ? "Error" : "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void MenuExportHtml_Click(object sender, RoutedEventArgs e)
    {
        bool isEn = LocalizationService.CurrentLanguage == AppLanguage.En;
        var sfd = new SaveFileDialog
        {
            Title = isEn ? "Save report in HTML (Word) format..." : "Зберегти звіт у форматі HTML (Word)...",
            Filter = isEn ? "HTML Files (*.html)|*.html|Word Document (*.htm)|*.htm|All Files (*.*)|*.*" : "Файли HTML (*.html)|*.html|Документ Word (*.htm)|*.htm|Усі файли (*.*)|*.*",
            FileName = "ahp_report.html"
        };
        if (sfd.ShowDialog(this) == true)
        {
            try
            {
                var tables = ReportTableBuilder.Build(_result);
                ReportExporter.SaveHtml(sfd.FileName, tables);
                TxtResultsStatus.Text = (isEn ? "Report successfully saved to HTML file: " : "Звіт успішно збережено у файл HTML: ") + System.IO.Path.GetFileName(sfd.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show((isEn ? "HTML export error: " : "Помилка експорту HTML: ") + ex.Message, isEn ? "Error" : "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void MenuExportRankingPng_Click(object sender, RoutedEventArgs e)
    {
        bool isEn = LocalizationService.CurrentLanguage == AppLanguage.En;
        var sfd = new SaveFileDialog
        {
            Title = isEn ? "Save ranking chart as PNG..." : "Зберегти діаграму рейтингу в PNG...",
            Filter = isEn ? "PNG Image (*.png)|*.png" : "Зображення PNG (*.png)|*.png",
            FileName = "ranking.png"
        };
        if (sfd.ShowDialog(this) == true)
        {
            bool wasDark = ThemeService.CurrentTheme == AppTheme.Dark;
            if (wasDark)
            {
                ThemeService.ApplyTheme(AppTheme.Light);
                UpdateLayout();
            }
            try
            {
                ExportElementToPng(CardRankingContainer, sfd.FileName, 192.0);
                TxtResultsStatus.Text = (isEn ? "Ranking chart saved: " : "Діаграму рейтингу збережено: ") + System.IO.Path.GetFileName(sfd.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show((isEn ? "Error exporting chart to PNG: " : "Помилка експорту діаграми в PNG: ") + ex.Message, isEn ? "Error" : "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (wasDark)
                {
                    ThemeService.ApplyTheme(AppTheme.Dark);
                    UpdateLayout();
                }
            }
        }
    }

    private void MenuExportSensitivityPng_Click(object sender, RoutedEventArgs e)
    {
        bool isEn = LocalizationService.CurrentLanguage == AppLanguage.En;
        string rawCritName = _currentSensitivityResult?.Criterion?.Name ?? _currentSensitivityResult?.Criterion?.Id ?? "criterion";
        string safeCritName = string.Concat(rawCritName.Split(System.IO.Path.GetInvalidFileNameChars())).Trim();
        if (string.IsNullOrEmpty(safeCritName)) safeCritName = "criterion";

        var sfd = new SaveFileDialog
        {
            Title = isEn ? "Save sensitivity plot as PNG..." : "Зберегти графік чутливості в PNG...",
            Filter = isEn ? "PNG Image (*.png)|*.png" : "Зображення PNG (*.png)|*.png",
            FileName = $"sensitivity_{safeCritName}.png"
        };
        if (sfd.ShowDialog(this) == true)
        {
            bool wasDark = ThemeService.CurrentTheme == AppTheme.Dark;
            if (wasDark)
            {
                ThemeService.ApplyTheme(AppTheme.Light);
                UpdateLayout();
                RedrawSensitivityPlot(_currentSensitivityT);
                UpdateLayout();
            }
            try
            {
                if (SensitivityPlotContainer.ActualWidth <= 1 || CanvasSensitivityPlot.ActualWidth <= 50)
                {
                    var measureSize = new Size(760, 460);
                    SensitivityPlotContainer.Measure(measureSize);
                    SensitivityPlotContainer.Arrange(new Rect(measureSize));
                    SensitivityPlotContainer.UpdateLayout();
                    RedrawSensitivityPlot(_currentSensitivityT);
                    SensitivityPlotContainer.UpdateLayout();
                }

                ExportElementToPng(SensitivityPlotContainer, sfd.FileName, 192.0);
                TxtResultsStatus.Text = (isEn ? "Sensitivity plot saved: " : "Графік чутливості збережено: ") + System.IO.Path.GetFileName(sfd.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show((isEn ? "Error exporting sensitivity plot to PNG: " : "Помилка експорту графіка чутливості в PNG: ") + ex.Message, isEn ? "Error" : "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (wasDark)
                {
                    ThemeService.ApplyTheme(AppTheme.Dark);
                    UpdateLayout();
                    RedrawSensitivityPlot(_currentSensitivityT);
                    UpdateLayout();
                }
            }
        }
    }

    private static void ExportElementToPng(FrameworkElement element, string filePath, double dpi = 192.0)
    {
        if (element.ActualWidth <= 1 || element.ActualHeight <= 1)
        {
            var defaultSize = new Size(800, 500);
            element.Measure(defaultSize);
            element.Arrange(new Rect(defaultSize));
            element.UpdateLayout();
        }

        double elemWidth = element.ActualWidth > 1 ? element.ActualWidth : element.DesiredSize.Width;
        double elemHeight = element.ActualHeight > 1 ? element.ActualHeight : element.DesiredSize.Height;

        double scale = dpi / 96.0;
        int width = (int)Math.Max(1, Math.Ceiling(elemWidth * scale));
        int height = (int)Math.Max(1, Math.Ceiling(elemHeight * scale));

        var rtb = new RenderTargetBitmap(width, height, dpi, dpi, PixelFormats.Pbgra32);

        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, elemWidth, elemHeight));
            var brush = new VisualBrush(element);
            dc.DrawRectangle(brush, null, new Rect(0, 0, elemWidth, elemHeight));
        }

        rtb.Render(dv);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rtb));
        using var stream = File.Create(filePath);
        encoder.Save(stream);
    }

    private void BtnCompareProfiles_Click(object sender, RoutedEventArgs e)
    {
        bool isEn = LocalizationService.CurrentLanguage == AppLanguage.En;
        if (_rawCriteria == null || _rawAlternatives == null || _rawCriteriaMatrix == null || _rawAltMatrices == null)
        {
            MessageBox.Show(
                isEn ? "Data for profile comparison is not available." : "Дані для порівняння профілів недоступні.",
                isEn ? "Information" : "Інформація",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        try
        {
            var win = new ProfileComparisonWindow(_rawCriteria, _rawAlternatives, _rawCriteriaMatrix, _rawAltMatrices, _result.Options)
            {
                Owner = this
            };
            win.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                (isEn ? "Error opening profile comparison: " : "Помилка відкриття порівняння профілів: ") + ex.Message,
                isEn ? "Error" : "Помилка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
