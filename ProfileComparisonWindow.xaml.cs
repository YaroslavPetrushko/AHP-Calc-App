using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using MAI.Models;
using MAI.Services;

namespace MAI;

/// <summary>
/// Логіка взаємодії для ProfileComparisonWindow.xaml.
/// Відображає зіставлення розрахункових профілів у 3 таблицях з експортом у CSV, Markdown та HTML.
/// </summary>
public partial class ProfileComparisonWindow : Window
{
    private readonly ProfileComparisonResult _comparisonResult;

    public ProfileComparisonWindow(ProfileComparisonResult result)
    {
        InitializeComponent();
        _comparisonResult = result ?? throw new ArgumentNullException(nameof(result));
        LoadData();
    }

    public ProfileComparisonWindow(AhpProjectData projectData)
        : this(ProfileComparisonService.Compare(projectData))
    {
    }

    public ProfileComparisonWindow(
        List<Criterion> criteria,
        List<Alternative> alternatives,
        ComparisonMatrix criteriaMatrix,
        Dictionary<string, ComparisonMatrix> altMatrices,
        AhpCalculationOptions? activeOptions = null)
        : this(ProfileComparisonService.Compare(criteria, alternatives, criteriaMatrix, altMatrices, null, activeOptions))
    {
    }

    private void LoadData()
    {
        PopulateCriteriaGrid();
        PopulateAlternativesGrid();
        PopulateConsistencyGrid();
        TxtSummary.Text = _comparisonResult.SummaryText;
    }

    private void PopulateCriteriaGrid()
    {
        var dt = new DataTable();
        dt.Columns.Add("col_name", typeof(string));
        for (int i = 0; i < _comparisonResult.Profiles.Count; i++)
        {
            dt.Columns.Add($"col_prof_{i}", typeof(string));
        }

        GridCriteriaWeights.Columns.Clear();
        GridCriteriaWeights.Columns.Add(new DataGridTextColumn
        {
            Header = LocalizationService.Get("Str_Comp_ColCriterion"),
            Binding = new Binding("[col_name]"),
            FontWeight = FontWeights.SemiBold,
            Width = new DataGridLength(180)
        });

        for (int i = 0; i < _comparisonResult.Profiles.Count; i++)
        {
            var prof = _comparisonResult.Profiles[i];
            GridCriteriaWeights.Columns.Add(new DataGridTextColumn
            {
                Header = prof.ProfileName,
                Binding = new Binding($"[col_prof_{i}]"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });
        }

        for (int cIdx = 0; cIdx < _comparisonResult.Criteria.Count; cIdx++)
        {
            var crit = _comparisonResult.Criteria[cIdx];
            var row = dt.NewRow();
            row["col_name"] = crit.Name;
            for (int pIdx = 0; pIdx < _comparisonResult.Profiles.Count; pIdx++)
            {
                var prof = _comparisonResult.Profiles[pIdx];
                double w = (prof.Result.CriteriaMatrixResult?.Weights != null && prof.Result.CriteriaMatrixResult.Weights.Length > cIdx)
                    ? prof.Result.CriteriaMatrixResult.Weights[cIdx]
                    : 0.0;
                row[$"col_prof_{pIdx}"] = w.ToString("0.0000", LocalizationService.ActiveCulture);
            }
            dt.Rows.Add(row);
        }

        GridCriteriaWeights.ItemsSource = dt.DefaultView;
    }

    private void PopulateAlternativesGrid()
    {
        var dt = new DataTable();
        dt.Columns.Add("col_alt_name", typeof(string));
        for (int i = 0; i < _comparisonResult.Profiles.Count; i++)
        {
            dt.Columns.Add($"col_p_{i}", typeof(string));
            dt.Columns.Add($"col_r_{i}", typeof(string));
        }

        GridAlternativePriorities.Columns.Clear();
        GridAlternativePriorities.Columns.Add(new DataGridTextColumn
        {
            Header = LocalizationService.Get("Str_Comp_ColAlternative"),
            Binding = new Binding("[col_alt_name]"),
            FontWeight = FontWeights.SemiBold,
            Width = new DataGridLength(200)
        });

        string prioritySuffix = LocalizationService.CurrentLanguage == AppLanguage.En ? "Priority" : "Пріоритет";
        string rankSuffix = LocalizationService.CurrentLanguage == AppLanguage.En ? "Rank" : "Ранг";

        for (int i = 0; i < _comparisonResult.Profiles.Count; i++)
        {
            var prof = _comparisonResult.Profiles[i];
            GridAlternativePriorities.Columns.Add(new DataGridTextColumn
            {
                Header = $"{prof.ProfileName} ({prioritySuffix})",
                Binding = new Binding($"[col_p_{i}]"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });
            GridAlternativePriorities.Columns.Add(new DataGridTextColumn
            {
                Header = $"{prof.ProfileName} ({rankSuffix})",
                Binding = new Binding($"[col_r_{i}]"),
                Width = new DataGridLength(80)
            });
        }

        for (int aIdx = 0; aIdx < _comparisonResult.Alternatives.Count; aIdx++)
        {
            var alt = _comparisonResult.Alternatives[aIdx];
            var row = dt.NewRow();
            row["col_alt_name"] = alt.Name;
            for (int pIdx = 0; pIdx < _comparisonResult.Profiles.Count; pIdx++)
            {
                var prof = _comparisonResult.Profiles[pIdx];
                var matched = prof.Result.RankedAlternatives.FirstOrDefault(a => a.Id == alt.Id);
                double priority = matched?.GlobalPriority ?? 0.0;
                string rank = matched?.RankText ?? "-";
                row[$"col_p_{pIdx}"] = priority.ToString("0.0000", LocalizationService.ActiveCulture);
                row[$"col_r_{pIdx}"] = rank;
            }
            dt.Rows.Add(row);
        }

        GridAlternativePriorities.ItemsSource = dt.DefaultView;
    }

    private void PopulateConsistencyGrid()
    {
        var dt = new DataTable();
        dt.Columns.Add("col_metric_name", typeof(string));
        for (int i = 0; i < _comparisonResult.Profiles.Count; i++)
        {
            dt.Columns.Add($"col_metric_{i}", typeof(string));
        }

        bool isEn = LocalizationService.CurrentLanguage == AppLanguage.En;

        GridConsistencyIndices.Columns.Clear();
        GridConsistencyIndices.Columns.Add(new DataGridTextColumn
        {
            Header = isEn ? "Consistency Metric" : "Показник узгодженості",
            Binding = new Binding("[col_metric_name]"),
            FontWeight = FontWeights.SemiBold,
            Width = new DataGridLength(240)
        });

        for (int i = 0; i < _comparisonResult.Profiles.Count; i++)
        {
            var prof = _comparisonResult.Profiles[i];
            GridConsistencyIndices.Columns.Add(new DataGridTextColumn
            {
                Header = prof.ProfileName,
                Binding = new Binding($"[col_metric_{i}]"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });
        }

        // 1. λmax критеріїв
        var rowLam = dt.NewRow();
        rowLam["col_metric_name"] = isEn ? "λmax (criteria matrix)" : "λmax (матриця критеріїв)";
        for (int pIdx = 0; pIdx < _comparisonResult.Profiles.Count; pIdx++)
        {
            rowLam[$"col_metric_{pIdx}"] = (_comparisonResult.Profiles[pIdx].Result.CriteriaMatrixResult?.LambdaMax ?? 0.0).ToString("0.0000", LocalizationService.ActiveCulture);
        }
        dt.Rows.Add(rowLam);

        // 2. CR критеріїв
        var rowCrCrit = dt.NewRow();
        rowCrCrit["col_metric_name"] = isEn ? "CR (criteria matrix)" : "CR (матриця критеріїв)";
        for (int pIdx = 0; pIdx < _comparisonResult.Profiles.Count; pIdx++)
        {
            double cr = _comparisonResult.Profiles[pIdx].Result.CriteriaMatrixResult?.ActiveCR ?? 0.0;
            rowCrCrit[$"col_metric_{pIdx}"] = (cr * 100.0).ToString("0.00", LocalizationService.ActiveCulture) + " %";
        }
        dt.Rows.Add(rowCrCrit);

        // 3. CR альтернатив за кожним критерієм
        foreach (var c in _comparisonResult.Criteria)
        {
            var rowCrAlt = dt.NewRow();
            rowCrAlt["col_metric_name"] = isEn ? $"CR (alternatives for \"{c.Name}\")" : $"CR (альтернативи за «{c.Name}»)";
            for (int pIdx = 0; pIdx < _comparisonResult.Profiles.Count; pIdx++)
            {
                var prof = _comparisonResult.Profiles[pIdx];
                double cr = (prof.Result.AlternativeMatrixResults != null && prof.Result.AlternativeMatrixResults.TryGetValue(c.Id, out var mr))
                    ? mr.ActiveCR
                    : 0.0;
                rowCrAlt[$"col_metric_{pIdx}"] = (cr * 100.0).ToString("0.00", LocalizationService.ActiveCulture) + " %";
            }
            dt.Rows.Add(rowCrAlt);
        }

        // 4. CRh
        var rowCrh = dt.NewRow();
        rowCrh["col_metric_name"] = isEn ? "CRh (hierarchy consistency)" : "CRh (узгодженість ієрархії)";
        for (int pIdx = 0; pIdx < _comparisonResult.Profiles.Count; pIdx++)
        {
            double crh = _comparisonResult.Profiles[pIdx].Result.ActiveHierarchicalCR;
            rowCrh[$"col_metric_{pIdx}"] = (crh * 100.0).ToString("0.00", LocalizationService.ActiveCulture) + " %";
        }
        dt.Rows.Add(rowCrh);

        GridConsistencyIndices.ItemsSource = dt.DefaultView;
    }

    private void BtnExport_Click(object sender, RoutedEventArgs e)
    {
        if (BtnExport.ContextMenu != null)
        {
            BtnExport.ContextMenu.PlacementTarget = BtnExport;
            BtnExport.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            BtnExport.ContextMenu.IsOpen = true;
        }
    }

    private void MenuExportCsv_Click(object sender, RoutedEventArgs e)
    {
        bool isEn = LocalizationService.CurrentLanguage == AppLanguage.En;
        var sfd = new SaveFileDialog
        {
            Title = isEn ? "Save profile comparison to CSV..." : "Зберегти порівняння профілів у CSV...",
            Filter = isEn ? "CSV Files (*.csv)|*.csv" : "Файли CSV (*.csv)|*.csv",
            FileName = "profile_comparison.csv"
        };
        if (sfd.ShowDialog(this) == true)
        {
            try
            {
                ReportExporter.SaveCsv(sfd.FileName, _comparisonResult.ToReportTables());
                TxtStatus.Text = (isEn ? "Saved: " : "Збережено: ") + Path.GetFileName(sfd.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show((isEn ? "Error exporting to CSV: " : "Помилка експорту в CSV: ") + ex.Message,
                    isEn ? "Error" : "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void MenuExportMarkdown_Click(object sender, RoutedEventArgs e)
    {
        bool isEn = LocalizationService.CurrentLanguage == AppLanguage.En;
        var sfd = new SaveFileDialog
        {
            Title = isEn ? "Save profile comparison to Markdown..." : "Зберегти порівняння профілів у Markdown...",
            Filter = isEn ? "Markdown Files (*.md)|*.md" : "Файли Markdown (*.md)|*.md",
            FileName = "profile_comparison.md"
        };
        if (sfd.ShowDialog(this) == true)
        {
            try
            {
                ReportExporter.SaveMarkdown(sfd.FileName, _comparisonResult.ToReportTables());
                TxtStatus.Text = (isEn ? "Saved: " : "Збережено: ") + Path.GetFileName(sfd.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show((isEn ? "Error exporting to Markdown: " : "Помилка експорту в Markdown: ") + ex.Message,
                    isEn ? "Error" : "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void MenuExportHtml_Click(object sender, RoutedEventArgs e)
    {
        bool isEn = LocalizationService.CurrentLanguage == AppLanguage.En;
        var sfd = new SaveFileDialog
        {
            Title = isEn ? "Save profile comparison to HTML..." : "Зберегти порівняння профілів у HTML...",
            Filter = isEn ? "HTML Webpage (*.html)|*.html" : "Веб-сторінка HTML (*.html)|*.html",
            FileName = "profile_comparison.html"
        };
        if (sfd.ShowDialog(this) == true)
        {
            try
            {
                ReportExporter.SaveHtml(sfd.FileName, _comparisonResult.ToReportTables());
                TxtStatus.Text = (isEn ? "Saved: " : "Збережено: ") + Path.GetFileName(sfd.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show((isEn ? "Error exporting to HTML: " : "Помилка експорту в HTML: ") + ex.Message,
                    isEn ? "Error" : "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
