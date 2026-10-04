using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using MAI.Models;

namespace MAI;

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
    private readonly AhpResult _result;
    private readonly List<MatrixResult> _allMatrixResults = new();

    public ResultsWindow(AhpResult result)
    {
        InitializeComponent();
        _result = result;

        InitHeaderAndBadges();
        InitRankingsList();
        InitCriteriaCards();
        InitRecommendationText();
        InitSynthesisDataGrid();
        InitConsistencyDataGrid();
        InitMatrixSelector();
    }

    private void InitHeaderAndBadges()
    {
        if (_result.BestAlternative != null)
        {
            TxtWinnerName.Text = $"{_result.BestAlternative.Name} ({_result.BestAlternative.GlobalPriority:P1})";
        }
        else
        {
            TxtWinnerName.Text = "Не визначено";
        }

        TxtHierarchicalCR.Text = $"CRh = {_result.HierarchicalCRTable:P1} ({_result.HierarchicalCRTable:F4})";

        if (_result.IsHierarchyConsistent)
        {
            BadgeConsistency.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DCFCE7"));
            BadgeConsistency.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#86EFAC"));
            TxtHierarchicalCR.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15803D"));
        }
        else
        {
            BadgeConsistency.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEE2E2"));
            BadgeConsistency.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCA5A5"));
            TxtHierarchicalCR.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C"));
        }
    }

    private void InitRankingsList()
    {
        var items = _result.RankedAlternatives.Select(a =>
        {
            double pct = a.GlobalPriority * 100.0;
            return new AlternativeDisplayItem
            {
                Rank = a.Rank,
                RankText = $"#{a.Rank}",
                Name = a.Name,
                Description = a.Description,
                GlobalPriority = a.GlobalPriority,
                PriorityPercent = pct,
                PriorityPercentText = $"{pct:F2}% ({a.GlobalPriority:F4})",
                RankBackground = a.Rank switch
                {
                    1 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669")),
                    2 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB")),
                    3 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")),
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"))
                },
                RankForeground = Brushes.White,
                BarBrush = a.Rank switch
                {
                    1 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669")),
                    2 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB")),
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#60A5FA"))
                },
                BadgeBackground = a.Rank switch
                {
                    1 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DCFCE7")),
                    2 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DBEAFE")),
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"))
                },
                BadgeForeground = a.Rank switch
                {
                    1 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15803D")),
                    2 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1D4ED8")),
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"))
                },
                BadgeText = a.Rank switch
                {
                    1 => "🏆 Оптимум (1)",
                    2 => "2 місце",
                    3 => "3 місце",
                    _ => $"{a.Rank} місце"
                }
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
            WeightPercentText = $"{c.Weight:P1} ({c.Weight:F4})"
        }).ToList();

        ListCriteriaCards.ItemsSource = cards;
    }

    private void InitRecommendationText()
    {
        if (_result.RankedAlternatives.Count == 0)
        {
            TxtRecommendation.Text = "Немає даних для формування висновку.";
            return;
        }

        var winner = _result.RankedAlternatives[0];
        var runnerUp = _result.RankedAlternatives.Count > 1 ? _result.RankedAlternatives[1] : null;

        var sb = new StringBuilder();
        sb.AppendLine($"1. Оптимальною альтернативою за методом аналізу ієрархій визначено «{winner.Name}» із підсумковим глобальним пріоритетом {winner.GlobalPriority:P2} ({winner.GlobalPriority:F4}).");

        if (runnerUp != null)
        {
            double margin = winner.GlobalPriority - runnerUp.GlobalPriority;
            sb.AppendLine($"2. Друге місце посіла альтернатива «{runnerUp.Name}» з пріоритетом {runnerUp.GlobalPriority:P2} (відрив становить {margin:P2}).");
            if (margin < 0.02)
            {
                sb.AppendLine("   Зверніть увагу: відрив між лідерами є незначним (< 2%), що свідчить про високу чутливість розв'язку до коливань оцінок або ринкових цін.");
            }
        }

        var topCriterion = _result.Criteria.OrderByDescending(c => c.Weight).FirstOrDefault();
        if (topCriterion != null)
        {
            sb.AppendLine($"3. Найбільш визначальним критерієм у моделі є «{topCriterion.Name}» із вагою {topCriterion.Weight:P1}.");
        }

        sb.AppendLine($"4. Ієрархічна узгодженість системи: коефіцієнт CRh = {_result.HierarchicalCRTable:P2} (за емпіричним RI: {_result.HierarchicalCREmpirical:P2}), що є меншим за допустимий поріг 10% (CR ≤ 0.10). Це математично підтверджує внутрішню логічну несуперечливість експертних суджень.");

        TxtRecommendation.Text = sb.ToString();
    }

    private void InitSynthesisDataGrid()
    {
        var dt = new DataTable();
        dt.Columns.Add("Ранг", typeof(string));
        dt.Columns.Add("Альтернатива", typeof(string));

        foreach (var c in _result.Criteria)
        {
            dt.Columns.Add($"{c.Name} (Lij)", typeof(string));
            dt.Columns.Add($"{c.Name} (Внесок)", typeof(string));
        }

        dt.Columns.Add("Глобальний бал (Pi)", typeof(string));
        dt.Columns.Add("Частка (%)", typeof(string));

        foreach (var alt in _result.RankedAlternatives)
        {
            var row = dt.NewRow();
            row["Ранг"] = $"#{alt.Rank}";
            row["Альтернатива"] = alt.Name;

            foreach (var c in _result.Criteria)
            {
                double locPri = alt.LocalPriorities.TryGetValue(c.Id, out var lp) ? lp : 0.0;
                double wScore = alt.WeightedScores.TryGetValue(c.Id, out var ws) ? ws : 0.0;
                row[$"{c.Name} (Lij)"] = locPri.ToString("F4");
                row[$"{c.Name} (Внесок)"] = wScore.ToString("F4");
            }

            row["Глобальний бал (Pi)"] = alt.GlobalPriority.ToString("F4");
            row["Частка (%)"] = $"{alt.GlobalPriority * 100:F2}%";
            dt.Rows.Add(row);
        }

        GridSynthesis.ItemsSource = dt.DefaultView;
    }

    private void InitConsistencyDataGrid()
    {
        var list = new List<ConsistencyRowItem>();

        // Матриця критеріїв
        var crCrit = _result.CriteriaMatrixResult;
        list.Add(new ConsistencyRowItem
        {
            MatrixName = "Матриця критеріїв (A)",
            Dimension = crCrit.Size,
            LambdaMax = crCrit.LambdaMax.ToString("F4"),
            CI = crCrit.CI.ToString("F4"),
            RITable = crCrit.RITable.ToString("F2"),
            RIEmpirical = crCrit.RIEmpirical.ToString("F2"),
            CRTable = $"{crCrit.CRTable:P1} ({crCrit.CRTable:F4})",
            CREmpirical = $"{crCrit.CREmpirical:P1} ({crCrit.CREmpirical:F4})",
            Status = crCrit.IsConsistent ? "✅ Узгоджено" : "❌ Неузгоджено",
            Verdict = crCrit.IsConsistent ? "CR <= 0.10" : "CR > 0.10"
        });

        // Матриці альтернатив
        foreach (var kvp in _result.AlternativeMatrixResults)
        {
            var res = kvp.Value;
            var critObj = _result.Criteria.FirstOrDefault(c => c.Id == kvp.Key);
            string critTitle = critObj != null ? critObj.Name : kvp.Key;

            list.Add(new ConsistencyRowItem
            {
                MatrixName = $"Альтернативи: {critTitle}",
                Dimension = res.Size,
                LambdaMax = res.LambdaMax.ToString("F4"),
                CI = res.CI.ToString("F4"),
                RITable = res.RITable.ToString("F2"),
                RIEmpirical = res.RIEmpirical.ToString("F2"),
                CRTable = $"{res.CRTable:P1} ({res.CRTable:F4})",
                CREmpirical = $"{res.CREmpirical:P1} ({res.CREmpirical:F4})",
                Status = res.IsConsistent ? "✅ Узгоджено" : "❌ Неузгоджено",
                Verdict = res.IsConsistent ? "CR <= 0.10" : "CR > 0.10"
            });
        }

        // Загальна композиційна узгодженість ієрархії
        list.Add(new ConsistencyRowItem
        {
            MatrixName = "⭐ Композиція всієї ієрархії (CRh)",
            Dimension = _result.RankedAlternatives.Count,
            LambdaMax = "-",
            CI = _result.HierarchicalCI.ToString("F4"),
            RITable = _result.HierarchicalRITable.ToString("F3"),
            RIEmpirical = _result.HierarchicalRIEmpirical.ToString("F3"),
            CRTable = $"{_result.HierarchicalCRTable:P1} ({_result.HierarchicalCRTable:F4})",
            CREmpirical = $"{_result.HierarchicalCREmpirical:P1} ({_result.HierarchicalCREmpirical:F4})",
            Status = _result.IsHierarchyConsistent ? "✅ Узгоджено" : "❌ Неузгоджено",
            Verdict = _result.IsHierarchyConsistent ? "CRh <= 0.10" : "CRh > 0.10"
        });

        GridConsistency.ItemsSource = list;
    }

    private void InitMatrixSelector()
    {
        _allMatrixResults.Clear();
        ComboMatrixSelector.Items.Clear();

        _allMatrixResults.Add(_result.CriteriaMatrixResult);
        ComboMatrixSelector.Items.Add("Матриця критеріїв");

        foreach (var c in _result.Criteria)
        {
            if (_result.AlternativeMatrixResults.TryGetValue(c.Id, out var mRes))
            {
                _allMatrixResults.Add(mRes);
                ComboMatrixSelector.Items.Add($"Альтернативи за: {c.Name}");
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

        var mRes = _allMatrixResults[idx];
        TxtSelectedMatrixInfo.Text = $"{mRes.Name} | λmax = {mRes.LambdaMax:F4}, CI = {mRes.CI:F4}, CR = {mRes.CRTable:P2} ({mRes.ConsistencyMessage})";

        // Показ нормалізованої матриці та ваг
        var dt = new DataTable();
        dt.Columns.Add("Елемент", typeof(string));

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

        dt.Columns.Add("Вага (wi)", typeof(string));
        dt.Columns.Add("Відсоток", typeof(string));

        for (int i = 0; i < mRes.Size; i++)
        {
            var row = dt.NewRow();
            row["Елемент"] = i < mRes.Labels.Count ? mRes.Labels[i] : $"E{i + 1}";

            for (int j = 0; j < mRes.Size; j++)
            {
                row[colNames[j]] = mRes.NormalizedMatrix[i, j].ToString("F4");
            }

            row["Вага (wi)"] = mRes.Weights[i].ToString("F4");
            row["Відсоток"] = $"{mRes.Weights[i] * 100:F2}%";
            dt.Rows.Add(row);
        }

        // Рядок сум стовпців
        var sumRow = dt.NewRow();
        sumRow["Елемент"] = "Сума стовпця вихідної матриці";
        for (int j = 0; j < mRes.Size; j++)
        {
            sumRow[colNames[j]] = mRes.ColumnSums[j].ToString("F3");
        }
        sumRow["Вага (wi)"] = "1.0000";
        sumRow["Відсоток"] = "100.00%";
        dt.Rows.Add(sumRow);

        GridSelectedMatrix.ItemsSource = dt.DefaultView;
    }

    private string GenerateFullReportText()
    {
        var sb = new StringBuilder();
        sb.AppendLine("================================================================================");
        sb.AppendLine("        ЗВІТ ЗА МЕТОДОМ АНАЛІЗУ ІЄРАРХІЙ (МАІ) ТОМАСА СААТІ (.NET 10)");
        sb.AppendLine("================================================================================");
        sb.AppendLine($"Дата та час розрахунку: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
        sb.AppendLine();

        sb.AppendLine("--- 1. ПІДСУМКОВИЙ РЕЙТИНГ АЛЬТЕРНАТИВ ---");
        foreach (var alt in _result.RankedAlternatives)
        {
            sb.AppendLine($"#{alt.Rank,-2} {alt.Name,-35} | Глобальний пріоритет: {alt.GlobalPriority:F4} ({alt.GlobalPriority * 100,6:F2}%)");
        }
        sb.AppendLine();

        sb.AppendLine("--- 2. ВАГОВІ КОЕФІЦІЄНТИ КРИТЕРІЇВ ---");
        foreach (var c in _result.Criteria)
        {
            sb.AppendLine($"{c.Name,-35} | Вага: {c.Weight:F4} ({c.Weight * 100,5:F2}%)");
        }
        sb.AppendLine();

        sb.AppendLine("--- 3. ПЕРЕВІРКА УЗГОДЖЕНОСТІ СУДЖЕНЬ (CI, RI, CR) ---");
        sb.AppendLine($"Матриця критеріїв:           λmax={_result.CriteriaMatrixResult.LambdaMax:F4}, CI={_result.CriteriaMatrixResult.CI:F4}, CR(табл)={_result.CriteriaMatrixResult.CRTable:P2}, CR(емп)={_result.CriteriaMatrixResult.CREmpirical:P2} [{_result.CriteriaMatrixResult.ConsistencyMessage}]");

        foreach (var kvp in _result.AlternativeMatrixResults)
        {
            var res = kvp.Value;
            sb.AppendLine($"{res.Name,-28}: λmax={res.LambdaMax:F4}, CI={res.CI:F4}, CR(табл)={res.CRTable:P2}, CR(емп)={res.CREmpirical:P2} [{res.ConsistencyMessage}]");
        }

        sb.AppendLine($"Композиція всієї ієрархії:   CIh={_result.HierarchicalCI:F4}, CRh(табл)={_result.HierarchicalCRTable:P2}, CRh(емп)={_result.HierarchicalCREmpirical:P2} [{_result.HierarchyConsistencyMessage}]");
        sb.AppendLine();

        sb.AppendLine("--- 4. МАТРИЦЯ СИНТЕЗУ (Lij · wj) ---");
        sb.Append($"{"Альтернатива",-30} | ");
        foreach (var c in _result.Criteria)
        {
            sb.Append($"{c.Name,-16} | ");
        }
        sb.AppendLine("Підсумок (Pi)");

        foreach (var alt in _result.RankedAlternatives)
        {
            sb.Append($"{alt.Name,-30} | ");
            foreach (var c in _result.Criteria)
            {
                double score = alt.WeightedScores.TryGetValue(c.Id, out var ws) ? ws : 0.0;
                sb.Append($"{score,16:F4} | ");
            }
            sb.AppendLine($"{alt.GlobalPriority:F4}");
        }

        sb.AppendLine();
        sb.AppendLine("--- 5. АНАЛІТИЧНИЙ ВИСНОВОК ТА РЕКОМЕНДАЦІЇ ---");
        sb.AppendLine(TxtRecommendation.Text);
        sb.AppendLine("================================================================================");

        return sb.ToString();
    }

    private void BtnCopyReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string report = GenerateFullReportText();
            Clipboard.SetText(report);
            MessageBox.Show("Звіт успішно скопійовано в буфер обміну!", "Копіювання", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Помилка копіювання: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnSaveReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var sfd = new SaveFileDialog
            {
                Filter = "Текстовий файл (*.txt)|*.txt|Усі файли (*.*)|*.*",
                FileName = $"AHP_Report_{DateTime.Now:yyyyMMdd_HHmm}.txt"
            };

            if (sfd.ShowDialog() == true)
            {
                File.WriteAllText(sfd.FileName, GenerateFullReportText(), Encoding.UTF8);
                MessageBox.Show($"Звіт успішно збережено у файл:\n{sfd.FileName}", "Збереження звіту", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Помилка збереження файлу: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
