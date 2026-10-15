using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using MAI.Models;
using MAI.Services;

namespace MAI;

public partial class MainWindow : Window
{
    private ObservableCollection<Criterion> _criteria = new();
    private ObservableCollection<Alternative> _alternatives = new();

    private ComparisonMatrix _criteriaMatrix = new(4);
    private Dictionary<string, ComparisonMatrix> _alternativeMatrices = new();

    private string _selectedCriterionId = string.Empty;
    private bool _isUpdatingMatrixCell = false;
    private AhpCalculationOptions _calculationOptions = AhpCalculationOptions.Default.Clone();
    private string _projectTitle = "Задача багатокритеріального вибору";
    private string _projectDescription = string.Empty;

    // Cache textboxes for quick reciprocal access
    private TextBox[,] _critTextBoxes = new TextBox[0, 0];
    private TextBox[,] _altTextBoxes = new TextBox[0, 0];
    private static readonly System.Globalization.CultureInfo UkCulture = new("uk-UA");

    public MainWindow()
    {
        InitializeComponent();

        GridCriteria.ItemsSource = _criteria;
        GridAlternatives.ItemsSource = _alternatives;

        SyncCalculationOptionsUI();

        // Завантажуємо Набір №2 (відеокарти 2026) за замовчуванням
        LoadPreset(PresetService.GetGpu2026Preset());
    }

    private void LoadPreset(AhpProblemPreset preset)
    {
        _projectTitle = !string.IsNullOrWhiteSpace(preset.Name) ? preset.Name : "Задача багатокритеріального вибору";
        _projectDescription = preset.Description ?? string.Empty;

        _criteria.Clear();
        foreach (var c in preset.Criteria)
        {
            _criteria.Add(c);
        }

        _alternatives.Clear();
        foreach (var a in preset.Alternatives)
        {
            _alternatives.Add(a);
        }

        _criteriaMatrix = preset.CriteriaMatrix;
        _alternativeMatrices = preset.AlternativeMatrices;

        _selectedCriterionId = _criteria.FirstOrDefault()?.Id ?? string.Empty;

        UpdateCountsUI();
        BuildCriteriaMatrixGrid();
        BuildAlternativesCriteriaSelector();
        BuildAlternativeMatrixGrid();
        UpdateAllDiagnostics();
    }

    private void UpdateCountsUI()
    {
        TxtCritCount.Text = $"({_criteria.Count})";
        TxtAltCount.Text = $"({_alternatives.Count})";
        TxtBottomStatus.Text = $"Готово до розрахунку: {_criteria.Count} критерії, {_alternatives.Count} альтернатив.";
        TxtBottomStatus.Foreground = (Brush)FindResource("TextDark");
    }

    #region Criteria Matrix Grid

    private void BuildCriteriaMatrixGrid()
    {
        GridCritMatrixContainer.Children.Clear();
        GridCritMatrixContainer.RowDefinitions.Clear();
        GridCritMatrixContainer.ColumnDefinitions.Clear();

        int n = _criteria.Count;
        if (n < 1) return;

        _critTextBoxes = new TextBox[n, n];

        // Ensure matrix size matches
        if (_criteriaMatrix.Size != n)
        {
            _criteriaMatrix.Resize(n, _criteria.Select(c => c.Name).ToList());
        }

        // Header row + n data rows
        for (int i = 0; i <= n; i++)
        {
            GridCritMatrixContainer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        // Header col + n data cols
        GridCritMatrixContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
        for (int j = 0; j < n; j++)
        {
            GridCritMatrixContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(82) });
        }

        // Top-left cell
        var corner = new Border
        {
            BorderThickness = new Thickness(1),
            Padding = new Thickness(6, 4, 6, 4),
            Margin = new Thickness(1)
        };
        corner.SetResourceReference(Border.BackgroundProperty, "MatrixHeaderBg");
        corner.SetResourceReference(Border.BorderBrushProperty, "BorderColor");

        var cornerText = new TextBlock
        {
            Text = "Критерій",
            FontWeight = FontWeights.Bold,
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        cornerText.SetResourceReference(TextBlock.ForegroundProperty, "MatrixHeaderFg");
        corner.Child = cornerText;

        Grid.SetRow(corner, 0);
        Grid.SetColumn(corner, 0);
        GridCritMatrixContainer.Children.Add(corner);

        // Column headers
        for (int j = 0; j < n; j++)
        {
            var header = new Border
            {
                BorderThickness = new Thickness(1),
                Padding = new Thickness(4),
                Margin = new Thickness(1)
            };
            header.SetResourceReference(Border.BackgroundProperty, "MatrixHeaderBg");
            header.SetResourceReference(Border.BorderBrushProperty, "BorderColor");

            var headerText = new TextBlock
            {
                Text = _criteria[j].Id,
                ToolTip = _criteria[j].Name,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            headerText.SetResourceReference(TextBlock.ForegroundProperty, "TextDark");
            header.Child = headerText;

            Grid.SetRow(header, 0);
            Grid.SetColumn(header, j + 1);
            GridCritMatrixContainer.Children.Add(header);
        }

        // Row headers and matrix cells
        for (int i = 0; i < n; i++)
        {
            // Row header
            var rowHeader = new Border
            {
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(1)
            };
            rowHeader.SetResourceReference(Border.BackgroundProperty, "MatrixRowHeaderBg");
            rowHeader.SetResourceReference(Border.BorderBrushProperty, "BorderColor");

            var rowHeaderText = new TextBlock
            {
                Text = $"{_criteria[i].Id} - {_criteria[i].Name}",
                ToolTip = _criteria[i].Description,
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            rowHeaderText.SetResourceReference(TextBlock.ForegroundProperty, "TextDark");
            rowHeader.Child = rowHeaderText;

            Grid.SetRow(rowHeader, i + 1);
            Grid.SetColumn(rowHeader, 0);
            GridCritMatrixContainer.Children.Add(rowHeader);

            // Cells
            for (int j = 0; j < n; j++)
            {
                int r = i;
                int c = j;

                var tb = new TextBox
                {
                    Style = (Style)FindResource("MatrixInputStyle"),
                    Margin = new Thickness(1),
                    Text = ComparisonMatrix.FormatSaatyValue(_criteriaMatrix.Values[r, c])
                };

                if (r == c)
                {
                    tb.IsReadOnly = true;
                    tb.Text = "1";
                }
                else
                {
                    tb.LostFocus += (s, e) => HandleCriteriaCellEdit(r, c, tb);
                    tb.KeyDown += (s, e) =>
                    {
                        if (e.Key == System.Windows.Input.Key.Enter)
                        {
                            HandleCriteriaCellEdit(r, c, tb);
                        }
                    };
                }

                _critTextBoxes[r, c] = tb;
                Grid.SetRow(tb, r + 1);
                Grid.SetColumn(tb, c + 1);
                GridCritMatrixContainer.Children.Add(tb);
            }
        }
    }

    private void HandleCriteriaCellEdit(int row, int col, TextBox tb)
    {
        if (_isUpdatingMatrixCell) return;

        string text = tb.Text.Trim();
        double currentVal = _criteriaMatrix.Values[row, col];

        if (ComparisonMatrix.TryParseSaatyValue(text, out double val))
        {
            _isUpdatingMatrixCell = true;
            try
            {
                _criteriaMatrix.SetComparison(row, col, val);
                tb.Text = ComparisonMatrix.FormatSaatyValue(val);
                tb.SetResourceReference(TextBox.BorderBrushProperty, "InputBorder");

                // Sync reciprocal cell
                if (_critTextBoxes != null && row < _critTextBoxes.GetLength(0) && col < _critTextBoxes.GetLength(1))
                {
                    var recipTb = _critTextBoxes[col, row];
                    if (recipTb != null)
                    {
                        recipTb.Text = ComparisonMatrix.FormatSaatyValue(1.0 / val);
                        recipTb.SetResourceReference(TextBox.BorderBrushProperty, "InputBorder");
                    }
                }

                UpdateCriteriaDiagnostics();
            }
            finally
            {
                _isUpdatingMatrixCell = false;
            }
        }
        else
        {
            // Відновлюємо попереднє валідне значення для запобігання розсинхронізації UI та моделі даних
            tb.Text = ComparisonMatrix.FormatSaatyValue(currentVal);
            tb.SetResourceReference(TextBox.BorderBrushProperty, "InputBorder");
            TxtBottomStatus.Text = "Значення має бути в межах від 1/9 до 9";
        }
    }

    private void UpdateCriteriaDiagnostics()
    {
        var result = AhpCalculator.CalculateMatrix(_criteriaMatrix, "Матриця критеріїв", _calculationOptions);

        TxtCritWeightsPreview.Text = "w = [" + string.Join(", ", _criteria.Select((c, idx) =>
            $"{c.Id}: {result.Weights[idx]:F3} ({result.Weights[idx]:P1})")) + "]";

        TxtCritLambdaCI.Text = $"λmax = {result.LambdaMax:F4} | CI = {result.CI:F4}";
        string riLabel = _calculationOptions.RiMode == RiCalculationMode.Empirical ? "RI(емп)" : "RI(табл)";
        double threshold = result.Threshold;
        TxtCritRIRatio.Text = $"{riLabel} = {result.ActiveRI:F3} | CR = {result.ActiveCR:P1} ({result.ActiveCR:F3})";
        TxtCritJudgedProgress.Text = $"Заповнено {_criteriaMatrix.JudgedCount} з {_criteriaMatrix.TotalPairs} порівнянь";

        if (result.IsConsistent)
        {
            PillCritStatus.SetResourceReference(Border.BackgroundProperty, "PillConsistentBg");
            PillCritStatus.SetResourceReference(Border.BorderBrushProperty, "PillConsistentBorder");
            TxtCritStatus.Text = $"Узгоджено (CR = {result.ActiveCR:P1} <= {threshold:P0})";
            TxtCritStatus.SetResourceReference(TextBlock.ForegroundProperty, "PillConsistentFg");

            ClearCriteriaCellHighlights();
            BorderCritInconsistentHint.Visibility = Visibility.Collapsed;
        }
        else
        {
            PillCritStatus.SetResourceReference(Border.BackgroundProperty, "PillInconsistentBg");
            PillCritStatus.SetResourceReference(Border.BorderBrushProperty, "PillInconsistentBorder");
            TxtCritStatus.Text = $"Неузгоджено (CR = {result.ActiveCR:P1} > {threshold:P0})";
            TxtCritStatus.SetResourceReference(TextBlock.ForegroundProperty, "PillInconsistentFg");

            var hint = AhpCalculator.FindMostInconsistentCell(_criteriaMatrix, result.Weights);
            if (hint != null)
            {
                int r = hint.Value.Row;
                int c = hint.Value.Col;
                string nameA = r < _criteria.Count ? _criteria[r].Name : $"C{r + 1}";
                string nameB = c < _criteria.Count ? _criteria[c].Name : $"C{c + 1}";
                string x = ComparisonMatrix.FormatSaatyValue(hint.Value.Current);
                string y = hint.Value.Implied.ToString("0.00", UkCulture);
                string z = ComparisonMatrix.FormatSaatyValue(hint.Value.Recommended);

                TxtCritInconsistentHint.Text = $"Найбільше відхилення: {nameA} vs {nameB}: задано {x}, узгоджене значення ≈ {y} (рекомендація: {z})";
                BorderCritInconsistentHint.Visibility = Visibility.Visible;
                HighlightCriteriaCells(r, c);
            }
            else
            {
                ClearCriteriaCellHighlights();
                BorderCritInconsistentHint.Visibility = Visibility.Collapsed;
            }
        }
    }

    #endregion

    #region Alternatives Matrix Grid

    private void BuildAlternativesCriteriaSelector()
    {
        PanelCritSelectors.Children.Clear();

        foreach (var c in _criteria)
        {
            var btn = new Button
            {
                Content = $"{c.Id}: {c.Name}",
                Tag = c.Id,
                Margin = new Thickness(0, 0, 8, 4),
                Style = (Style)FindResource("SecondaryBtnStyle"),
                Height = 32
            };

            if (c.Id == _selectedCriterionId)
            {
                btn.SetResourceReference(Button.BackgroundProperty, "PrimaryBlue");
                btn.Foreground = Brushes.White;
                btn.BorderThickness = new Thickness(0);
            }

            btn.Click += (s, e) =>
            {
                _selectedCriterionId = c.Id;
                BuildAlternativesCriteriaSelector();
                BuildAlternativeMatrixGrid();
                UpdateAlternativeDiagnostics();
            };

            PanelCritSelectors.Children.Add(btn);
        }
    }

    private void BuildAlternativeMatrixGrid()
    {
        GridAltMatrixContainer.Children.Clear();
        GridAltMatrixContainer.RowDefinitions.Clear();
        GridAltMatrixContainer.ColumnDefinitions.Clear();

        if (string.IsNullOrEmpty(_selectedCriterionId))
        {
            _selectedCriterionId = _criteria.FirstOrDefault()?.Id ?? string.Empty;
        }

        if (string.IsNullOrEmpty(_selectedCriterionId)) return;

        int m = _alternatives.Count;
        if (m < 1) return;

        _altTextBoxes = new TextBox[m, m];

        if (!_alternativeMatrices.TryGetValue(_selectedCriterionId, out var altMatrix) || altMatrix.Size != m)
        {
            altMatrix = new ComparisonMatrix(m, _alternatives.Select(a => a.Name).ToList());
            _alternativeMatrices[_selectedCriterionId] = altMatrix;
        }

        // Header row + m data rows
        for (int i = 0; i <= m; i++)
        {
            GridAltMatrixContainer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        // Header col + m data cols
        GridAltMatrixContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(170) });
        for (int j = 0; j < m; j++)
        {
            GridAltMatrixContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(82) });
        }

        // Top-left corner
        var corner = new Border
        {
            BorderThickness = new Thickness(1),
            Padding = new Thickness(6, 4, 6, 4),
            Margin = new Thickness(1)
        };
        corner.SetResourceReference(Border.BackgroundProperty, "MatrixHeaderBg");
        corner.SetResourceReference(Border.BorderBrushProperty, "BorderColor");

        var cornerText = new TextBlock
        {
            Text = "Альтернатива",
            FontWeight = FontWeights.Bold,
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        cornerText.SetResourceReference(TextBlock.ForegroundProperty, "MatrixHeaderFg");
        corner.Child = cornerText;

        Grid.SetRow(corner, 0);
        Grid.SetColumn(corner, 0);
        GridAltMatrixContainer.Children.Add(corner);

        // Column headers
        for (int j = 0; j < m; j++)
        {
            var header = new Border
            {
                BorderThickness = new Thickness(1),
                Padding = new Thickness(4),
                Margin = new Thickness(1)
            };
            header.SetResourceReference(Border.BackgroundProperty, "MatrixHeaderBg");
            header.SetResourceReference(Border.BorderBrushProperty, "BorderColor");

            var headerText = new TextBlock
            {
                Text = _alternatives[j].Id,
                ToolTip = _alternatives[j].Name,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            headerText.SetResourceReference(TextBlock.ForegroundProperty, "TextDark");
            header.Child = headerText;

            Grid.SetRow(header, 0);
            Grid.SetColumn(header, j + 1);
            GridAltMatrixContainer.Children.Add(header);
        }

        // Rows and cells
        for (int i = 0; i < m; i++)
        {
            // Row header
            var rowHeader = new Border
            {
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(1)
            };
            rowHeader.SetResourceReference(Border.BackgroundProperty, "MatrixRowHeaderBg");
            rowHeader.SetResourceReference(Border.BorderBrushProperty, "BorderColor");

            var rowHeaderText = new TextBlock
            {
                Text = $"{_alternatives[i].Id} - {_alternatives[i].Name}",
                ToolTip = _alternatives[i].Description,
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            rowHeaderText.SetResourceReference(TextBlock.ForegroundProperty, "TextDark");
            rowHeader.Child = rowHeaderText;

            Grid.SetRow(rowHeader, i + 1);
            Grid.SetColumn(rowHeader, 0);
            GridAltMatrixContainer.Children.Add(rowHeader);

            // Cells
            for (int j = 0; j < m; j++)
            {
                int r = i;
                int c = j;

                var tb = new TextBox
                {
                    Style = (Style)FindResource("MatrixInputStyle"),
                    Margin = new Thickness(1),
                    Text = ComparisonMatrix.FormatSaatyValue(altMatrix.Values[r, c])
                };

                if (r == c)
                {
                    tb.IsReadOnly = true;
                    tb.Text = "1";
                }
                else
                {
                    tb.LostFocus += (s, e) => HandleAlternativeCellEdit(r, c, tb, altMatrix);
                    tb.KeyDown += (s, e) =>
                    {
                        if (e.Key == System.Windows.Input.Key.Enter)
                        {
                            HandleAlternativeCellEdit(r, c, tb, altMatrix);
                        }
                    };
                }

                _altTextBoxes[r, c] = tb;
                Grid.SetRow(tb, r + 1);
                Grid.SetColumn(tb, c + 1);
                GridAltMatrixContainer.Children.Add(tb);
            }
        }

        UpdateAlternativeDiagnostics();
    }

    private void HandleAlternativeCellEdit(int row, int col, TextBox tb, ComparisonMatrix altMatrix)
    {
        if (_isUpdatingMatrixCell) return;

        string text = tb.Text.Trim();
        double currentVal = altMatrix.Values[row, col];

        if (ComparisonMatrix.TryParseSaatyValue(text, out double val))
        {
            _isUpdatingMatrixCell = true;
            try
            {
                altMatrix.SetComparison(row, col, val);
                tb.Text = ComparisonMatrix.FormatSaatyValue(val);
                tb.SetResourceReference(TextBox.BorderBrushProperty, "InputBorder");

                // Sync reciprocal cell
                if (_altTextBoxes != null && row < _altTextBoxes.GetLength(0) && col < _altTextBoxes.GetLength(1))
                {
                    var recipTb = _altTextBoxes[col, row];
                    if (recipTb != null)
                    {
                        recipTb.Text = ComparisonMatrix.FormatSaatyValue(1.0 / val);
                        recipTb.SetResourceReference(TextBox.BorderBrushProperty, "InputBorder");
                    }
                }

                UpdateAlternativeDiagnostics();
            }
            finally
            {
                _isUpdatingMatrixCell = false;
            }
        }
        else
        {
            // Відновлюємо попереднє валідне значення для запобігання розсинхронізації UI та моделі даних
            tb.Text = ComparisonMatrix.FormatSaatyValue(currentVal);
            tb.SetResourceReference(TextBox.BorderBrushProperty, "InputBorder");
            TxtBottomStatus.Text = "Значення має бути в межах від 1/9 до 9";
        }
    }

    private void UpdateAlternativeDiagnostics()
    {
        if (string.IsNullOrEmpty(_selectedCriterionId) || !_alternativeMatrices.TryGetValue(_selectedCriterionId, out var altMatrix))
            return;

        var critObj = _criteria.FirstOrDefault(c => c.Id == _selectedCriterionId);
        string critTitle = critObj != null ? critObj.Name : _selectedCriterionId;

        TxtAltLocalPriTitle.Text = $"Локальні пріоритети за критерієм: «{critTitle}»";

        var result = AhpCalculator.CalculateMatrix(altMatrix, $"Альтернативи за {_selectedCriterionId}", _calculationOptions);

        TxtAltLocalPriPreview.Text = string.Join(", ", _alternatives.Select((a, idx) =>
            $"{a.Id}: {result.Weights[idx]:F3} ({result.Weights[idx]:P1})"));

        TxtAltLambdaCI.Text = $"λmax = {result.LambdaMax:F4} | CI = {result.CI:F4}";
        string riLabel = _calculationOptions.RiMode == RiCalculationMode.Empirical ? "RI(емп)" : "RI(табл)";
        double threshold = result.Threshold;
        TxtAltRIRatio.Text = $"{riLabel} = {result.ActiveRI:F3} | CR = {result.ActiveCR:P1} ({result.ActiveCR:F3})";
        TxtAltJudgedProgress.Text = $"Заповнено {altMatrix.JudgedCount} з {altMatrix.TotalPairs} порівнянь";

        if (result.IsConsistent)
        {
            PillAltStatus.SetResourceReference(Border.BackgroundProperty, "PillConsistentBg");
            PillAltStatus.SetResourceReference(Border.BorderBrushProperty, "PillConsistentBorder");
            TxtAltStatus.Text = $"Узгоджено (CR = {result.ActiveCR:P1} <= {threshold:P0})";
            TxtAltStatus.SetResourceReference(TextBlock.ForegroundProperty, "PillConsistentFg");

            ClearAlternativeCellHighlights();
            BorderAltInconsistentHint.Visibility = Visibility.Collapsed;
        }
        else
        {
            PillAltStatus.SetResourceReference(Border.BackgroundProperty, "PillInconsistentBg");
            PillAltStatus.SetResourceReference(Border.BorderBrushProperty, "PillInconsistentBorder");
            TxtAltStatus.Text = $"Неузгоджено (CR = {result.ActiveCR:P1} > {threshold:P0})";
            TxtAltStatus.SetResourceReference(TextBlock.ForegroundProperty, "PillInconsistentFg");

            var hint = AhpCalculator.FindMostInconsistentCell(altMatrix, result.Weights);
            if (hint != null)
            {
                int r = hint.Value.Row;
                int c = hint.Value.Col;
                string nameA = r < _alternatives.Count ? _alternatives[r].Name : $"A{r + 1}";
                string nameB = c < _alternatives.Count ? _alternatives[c].Name : $"A{c + 1}";
                string x = ComparisonMatrix.FormatSaatyValue(hint.Value.Current);
                string y = hint.Value.Implied.ToString("0.00", UkCulture);
                string z = ComparisonMatrix.FormatSaatyValue(hint.Value.Recommended);

                TxtAltInconsistentHint.Text = $"Найбільше відхилення: {nameA} vs {nameB}: задано {x}, узгоджене значення ≈ {y} (рекомендація: {z})";
                BorderAltInconsistentHint.Visibility = Visibility.Visible;
                HighlightAlternativeCells(r, c);
            }
            else
            {
                ClearAlternativeCellHighlights();
                BorderAltInconsistentHint.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void ClearCriteriaCellHighlights()
    {
        if (_critTextBoxes == null) return;
        int rows = _critTextBoxes.GetLength(0);
        int cols = _critTextBoxes.GetLength(1);
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                var tb = _critTextBoxes[i, j];
                if (tb != null)
                {
                    tb.SetResourceReference(TextBox.BorderBrushProperty, "InputBorder");
                    tb.BorderThickness = new Thickness(1);
                }
            }
        }
    }

    private void HighlightCriteriaCells(int r, int c)
    {
        ClearCriteriaCellHighlights();
        var orange = (Brush)FindResource("WarningOrange");
        var thick = new Thickness(2);

        if (_critTextBoxes != null && r < _critTextBoxes.GetLength(0) && c < _critTextBoxes.GetLength(1))
        {
            var tb1 = _critTextBoxes[r, c];
            if (tb1 != null)
            {
                tb1.BorderBrush = orange;
                tb1.BorderThickness = thick;
            }
            var tb2 = _critTextBoxes[c, r];
            if (tb2 != null)
            {
                tb2.BorderBrush = orange;
                tb2.BorderThickness = thick;
            }
        }
    }

    private void ClearAlternativeCellHighlights()
    {
        if (_altTextBoxes == null) return;
        int rows = _altTextBoxes.GetLength(0);
        int cols = _altTextBoxes.GetLength(1);
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                var tb = _altTextBoxes[i, j];
                if (tb != null)
                {
                    tb.SetResourceReference(TextBox.BorderBrushProperty, "InputBorder");
                    tb.BorderThickness = new Thickness(1);
                }
            }
        }
    }

    private void HighlightAlternativeCells(int r, int c)
    {
        ClearAlternativeCellHighlights();
        var orange = (Brush)FindResource("WarningOrange");
        var thick = new Thickness(2);

        if (_altTextBoxes != null && r < _altTextBoxes.GetLength(0) && c < _altTextBoxes.GetLength(1))
        {
            var tb1 = _altTextBoxes[r, c];
            if (tb1 != null)
            {
                tb1.BorderBrush = orange;
                tb1.BorderThickness = thick;
            }
            var tb2 = _altTextBoxes[c, r];
            if (tb2 != null)
            {
                tb2.BorderBrush = orange;
                tb2.BorderThickness = thick;
            }
        }
    }

    #endregion

    private void UpdateAllDiagnostics()
    {
        UpdateCriteriaDiagnostics();
        UpdateAlternativeDiagnostics();
    }

    #region Criteria & Alternatives Management

    private string _prevCellCritId = string.Empty;
    private string _prevCellCritName = string.Empty;
    private string _prevCellAltId = string.Empty;
    private string _prevCellAltName = string.Empty;

    private void GridCriteria_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
    {
        if (e.Row.Item is Criterion crit)
        {
            _prevCellCritId = crit.Id;
            _prevCellCritName = crit.Name;
        }
    }

    private void GridCriteria_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction != DataGridEditAction.Commit) return;
        if (e.Row.Item is not Criterion crit) return;
        if (e.EditingElement is not TextBox tb) return;

        string colHeader = e.Column.Header?.ToString() ?? string.Empty;
        string newVal = tb.Text.Trim();

        if (colHeader.Contains("ID") || colHeader.Contains("Код") || e.Column.DisplayIndex == 0)
        {
            if (string.IsNullOrWhiteSpace(newVal))
            {
                tb.Text = _prevCellCritId;
                crit.Id = _prevCellCritId;
                TxtBottomStatus.Text = "Помилка валідації: Код критерію не може бути порожнім.";
                return;
            }

            if (!string.Equals(newVal, _prevCellCritId, StringComparison.OrdinalIgnoreCase) &&
                _criteria.Any(c => c != crit && string.Equals(c.Id, newVal, StringComparison.OrdinalIgnoreCase)))
            {
                tb.Text = _prevCellCritId;
                crit.Id = _prevCellCritId;
                MessageBox.Show($"Критерій з кодом «{newVal}» вже існує. Коди мають бути унікальними.", "Помилка валідації", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string oldId = _prevCellCritId;
            crit.Id = newVal;

            if (!string.Equals(oldId, newVal, StringComparison.OrdinalIgnoreCase))
            {
                if (_alternativeMatrices.TryGetValue(oldId, out var mat))
                {
                    _alternativeMatrices.Remove(oldId);
                    _alternativeMatrices[newVal] = mat;
                }

                if (_selectedCriterionId == oldId)
                {
                    _selectedCriterionId = newVal;
                }

                Dispatcher.BeginInvoke(() =>
                {
                    BuildAlternativesCriteriaSelector();
                    UpdateCountsUI();
                });
            }
        }
        else if (colHeader.Contains("Назва") || e.Column.DisplayIndex == 1)
        {
            if (string.IsNullOrWhiteSpace(newVal))
            {
                tb.Text = _prevCellCritName;
                crit.Name = _prevCellCritName;
                TxtBottomStatus.Text = "Помилка валідації: Назва критерію не може бути порожньою.";
                return;
            }

            if (!string.Equals(newVal, _prevCellCritName, StringComparison.OrdinalIgnoreCase) &&
                _criteria.Any(c => c != crit && string.Equals(c.Name, newVal, StringComparison.OrdinalIgnoreCase)))
            {
                tb.Text = _prevCellCritName;
                crit.Name = _prevCellCritName;
                MessageBox.Show($"Критерій з назвою «{newVal}» вже існує. Назви критеріїв мають бути унікальними.", "Помилка валідації", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            crit.Name = newVal;
            Dispatcher.BeginInvoke(() =>
            {
                _criteriaMatrix.Labels = _criteria.Select(c => c.Name).ToList();
                BuildCriteriaMatrixGrid();
                BuildAlternativesCriteriaSelector();
                UpdateAllDiagnostics();
            });
        }
    }

    private void GridAlternatives_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
    {
        if (e.Row.Item is Alternative alt)
        {
            _prevCellAltId = alt.Id;
            _prevCellAltName = alt.Name;
        }
    }

    private void GridAlternatives_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction != DataGridEditAction.Commit) return;
        if (e.Row.Item is not Alternative alt) return;
        if (e.EditingElement is not TextBox tb) return;

        string colHeader = e.Column.Header?.ToString() ?? string.Empty;
        string newVal = tb.Text.Trim();

        if (colHeader.Contains("ID") || colHeader.Contains("Код") || e.Column.DisplayIndex == 0)
        {
            if (string.IsNullOrWhiteSpace(newVal))
            {
                tb.Text = _prevCellAltId;
                alt.Id = _prevCellAltId;
                TxtBottomStatus.Text = "Помилка валідації: Код альтернативи не може бути порожнім.";
                return;
            }

            if (!string.Equals(newVal, _prevCellAltId, StringComparison.OrdinalIgnoreCase) &&
                _alternatives.Any(a => a != alt && string.Equals(a.Id, newVal, StringComparison.OrdinalIgnoreCase)))
            {
                tb.Text = _prevCellAltId;
                alt.Id = _prevCellAltId;
                MessageBox.Show($"Альтернатива з кодом «{newVal}» вже існує. Коди мають бути унікальними.", "Помилка валідації", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            alt.Id = newVal;
            Dispatcher.BeginInvoke(() =>
            {
                UpdateAllDiagnostics();
            });
        }
        else if (colHeader.Contains("Назва") || e.Column.DisplayIndex == 1)
        {
            if (string.IsNullOrWhiteSpace(newVal))
            {
                tb.Text = _prevCellAltName;
                alt.Name = _prevCellAltName;
                TxtBottomStatus.Text = "Помилка валідації: Назва альтернативи не може бути порожньою.";
                return;
            }

            if (!string.Equals(newVal, _prevCellAltName, StringComparison.OrdinalIgnoreCase) &&
                _alternatives.Any(a => a != alt && string.Equals(a.Name, newVal, StringComparison.OrdinalIgnoreCase)))
            {
                tb.Text = _prevCellAltName;
                alt.Name = _prevCellAltName;
                MessageBox.Show($"Альтернатива з назвою «{newVal}» вже існує. Назви мають бути унікальними.", "Помилка валідації", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            alt.Name = newVal;
            Dispatcher.BeginInvoke(() =>
            {
                foreach (var mat in _alternativeMatrices.Values)
                {
                    mat.Labels = _alternatives.Select(a => a.Name).ToList();
                }
                BuildAlternativeMatrixGrid();
                UpdateAllDiagnostics();
            });
        }
    }

    private string GenerateNextCriterionId()
    {
        int num = 1;
        while (_criteria.Any(c => c.Id == $"C{num}") || _alternativeMatrices.ContainsKey($"C{num}"))
        {
            num++;
        }
        return $"C{num}";
    }

    private string GenerateNextAlternativeId()
    {
        int num = 1;
        while (_alternatives.Any(a => a.Id == $"A{num}"))
        {
            num++;
        }
        return $"A{num}";
    }

    private void BtnAddCriterion_Click(object sender, RoutedEventArgs e)
    {
        if (_criteria.Count >= 8)
        {
            MessageBox.Show("Рекомендована максимальна кількість критеріїв за методом Сааті — не більше 8 (для запобігання втрати узгодженості суджень).", "Обмеження розмірності", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string nextId = GenerateNextCriterionId();
        int displayNum = _criteria.Count + 1;
        string defaultName = $"Критерій {displayNum}";
        int nameCounter = 2;
        while (_criteria.Any(c => c.Name.Equals(defaultName, StringComparison.OrdinalIgnoreCase)))
        {
            defaultName = $"Критерій {displayNum}_{nameCounter++}";
        }

        var newCrit = new Criterion(nextId, defaultName, "Новий критерій оцінювання");
        _criteria.Add(newCrit);

        _criteriaMatrix.Resize(_criteria.Count, _criteria.Select(c => c.Name).ToList());
        _alternativeMatrices[newCrit.Id] = new ComparisonMatrix(_alternatives.Count, _alternatives.Select(a => a.Name).ToList());

        UpdateCountsUI();
        BuildCriteriaMatrixGrid();
        BuildAlternativesCriteriaSelector();
        UpdateAllDiagnostics();
        TxtBottomStatus.Text = $"Додано новий критерій «{newCrit.Name}» ({newCrit.Id}).";
    }

    private void BtnRemoveCriterion_Click(object sender, RoutedEventArgs e)
    {
        if (_criteria.Count <= 2)
        {
            MessageBox.Show("Для застосування методу багатокритеріального аналізу необхідно мінімум 2 критерії.", "Обмеження", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var selected = GridCriteria.SelectedItem as Criterion;
        if (selected == null)
        {
            MessageBox.Show("Будь ласка, оберіть критерій для видалення зі списку в таблиці.", "Вибір елемента", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show($"Ви дійсно бажаєте видалити критерій «{selected.Name}» ({selected.Id})?\nУсі його попарні порівняння та матрицю альтернатив буде видалено.",
            "Підтвердження видалення", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        int idx = _criteria.IndexOf(selected);
        if (idx >= 0)
        {
            _criteria.RemoveAt(idx);
            _alternativeMatrices.Remove(selected.Id);
            _criteriaMatrix.RemoveAt(idx);

            if (_selectedCriterionId == selected.Id)
            {
                _selectedCriterionId = _criteria.FirstOrDefault()?.Id ?? string.Empty;
            }

            UpdateCountsUI();
            BuildCriteriaMatrixGrid();
            BuildAlternativesCriteriaSelector();
            BuildAlternativeMatrixGrid();
            UpdateAllDiagnostics();
            TxtBottomStatus.Text = $"Критерій «{selected.Name}» успішно видалено.";
        }
    }

    private void BtnAddAlternative_Click(object sender, RoutedEventArgs e)
    {
        if (_alternatives.Count >= 8)
        {
            MessageBox.Show("Рекомендована максимальна кількість альтернатив — до 8 варіантів.", "Обмеження розмірності", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string nextId = GenerateNextAlternativeId();
        int displayNum = _alternatives.Count + 1;
        string defaultName = $"Альтернатива {displayNum}";
        int nameCounter = 2;
        while (_alternatives.Any(a => a.Name.Equals(defaultName, StringComparison.OrdinalIgnoreCase)))
        {
            defaultName = $"Альтернатива {displayNum}_{nameCounter++}";
        }

        var newAlt = new Alternative(nextId, defaultName, "Новий варіант вибору");
        _alternatives.Add(newAlt);

        foreach (var c in _criteria)
        {
            if (_alternativeMatrices.TryGetValue(c.Id, out var mat))
            {
                mat.Resize(_alternatives.Count, _alternatives.Select(a => a.Name).ToList());
            }
        }

        UpdateCountsUI();
        BuildAlternativeMatrixGrid();
        UpdateAllDiagnostics();
        TxtBottomStatus.Text = $"Додано нову альтернативу «{newAlt.Name}» ({newAlt.Id}).";
    }

    private void BtnRemoveAlternative_Click(object sender, RoutedEventArgs e)
    {
        if (_alternatives.Count <= 2)
        {
            MessageBox.Show("Для порівняння необхідно мінімум 2 альтернативи.", "Обмеження", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var selected = GridAlternatives.SelectedItem as Alternative;
        if (selected == null)
        {
            MessageBox.Show("Будь ласка, оберіть альтернативу для видалення зі списку в таблиці.", "Вибір елемента", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show($"Ви дійсно бажаєте видалити альтернативу «{selected.Name}» ({selected.Id})?\nЇї оцінки за всіма критеріями буде видалено.",
            "Підтвердження видалення", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        int idx = _alternatives.IndexOf(selected);
        if (idx >= 0)
        {
            _alternatives.RemoveAt(idx);

            foreach (var c in _criteria)
            {
                if (_alternativeMatrices.TryGetValue(c.Id, out var mat))
                {
                    mat.RemoveAt(idx);
                }
            }

            UpdateCountsUI();
            BuildAlternativeMatrixGrid();
            UpdateAllDiagnostics();
            TxtBottomStatus.Text = $"Альтернативу «{selected.Name}» успішно видалено.";
        }
    }

    #endregion

    #region Preset Buttons

    private void BtnPresetReference_Click(object sender, RoutedEventArgs e)
    {
        LoadPreset(PresetService.GetReferencePreset());
        TxtBottomStatus.Text = "Завантажено: Контрольний набір №1: відеокарти (попереднє покоління).";
    }

    private void BtnPresetGpu2026_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var preset = PresetService.GetGpu2026Preset();
            LoadPreset(preset);
            TxtBottomStatus.Text = "Завантажено: Набір №2: відеокарти 2026 (RTX 50 / RX 9000).";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Помилка завантаження набору №2:\n{ex.Message}", "Помилка пресету", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnResetEmpty_Click(object sender, RoutedEventArgs e)
    {
        var res = MessageBox.Show("Створити новий чистий шаблон задачі? Усі поточні матриці будуть скинуті до одиниць.", "Скидання", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (res == MessageBoxResult.Yes)
        {
            LoadPreset(PresetService.CreateEmptyPreset(3, 3));
        }
    }

    #endregion

    private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source is TabControl)
        {
            // Оновлюємо актуальні мітки для матриць динамічно
            _criteriaMatrix.Labels = _criteria.Select(c => c.Name).ToList();
            foreach (var c in _criteria)
            {
                if (_alternativeMatrices.TryGetValue(c.Id, out var mat))
                {
                    mat.Labels = _alternatives.Select(a => a.Name).ToList();
                }
            }

            BuildCriteriaMatrixGrid();
            BuildAlternativesCriteriaSelector();
            BuildAlternativeMatrixGrid();
            UpdateAllDiagnostics();
        }
    }

    private void BtnCalculate_Click(object sender, RoutedEventArgs e)
    {
        // 1. Валідація наявності назв
        if (_criteria.Any(c => string.IsNullOrWhiteSpace(c.Name)) || _alternatives.Any(a => string.IsNullOrWhiteSpace(a.Name)))
        {
            MessageBox.Show("Назви критеріїв та альтернатив не можуть бути порожніми.", "Помилка валідації", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 2. Валідація унікальності назв критеріїв
        var critDuplicate = _criteria
            .GroupBy(c => c.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);

        if (critDuplicate != null)
        {
            MessageBox.Show($"Виявлено дублювання назви критерію: «{critDuplicate.Key}».\nУсі критерії повинні мати унікальні назви для коректного формування матриці синтезу.",
                "Помилка валідації", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 3. Валідація унікальності назв альтернатив
        var altDuplicate = _alternatives
            .GroupBy(a => a.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);

        if (altDuplicate != null)
        {
            MessageBox.Show($"Виявлено дублювання назви альтернативи: «{altDuplicate.Key}».\nУсі альтернативи повинні мати унікальні назви для однозначного ранжування.",
                "Помилка валідації", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 4. Перевірка заповненості матриць парних порівнянь
        var incompleteMatrices = new List<string>();
        if (!_criteriaMatrix.IsFullyJudged)
        {
            incompleteMatrices.Add($"Матриця критеріїв (заповнено {_criteriaMatrix.JudgedCount} з {_criteriaMatrix.TotalPairs})");
        }
        foreach (var c in _criteria)
        {
            if (_alternativeMatrices.TryGetValue(c.Id, out var mat) && !mat.IsFullyJudged)
            {
                incompleteMatrices.Add($"Матриця альтернатив за «{c.Name}» (заповнено {mat.JudgedCount} з {mat.TotalPairs})");
            }
        }

        if (incompleteMatrices.Count > 0)
        {
            string listText = string.Join("\n• ", incompleteMatrices);
            string confirmMsg = $"Виявлено не повністю заповнені матриці порівнянь:\n• {listText}\n\nНе оцінені пари вважаються рівними (1). Продовжити?";
            var dr = MessageBox.Show(confirmMsg, "Неповні оцінки матриць", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (dr != MessageBoxResult.Yes)
            {
                return;
            }
        }

        try
        {
            // Оновлюємо актуальні мітки перед ієрархічним синтезом
            _criteriaMatrix.Labels = _criteria.Select(c => c.Name).ToList();
            foreach (var c in _criteria)
            {
                if (_alternativeMatrices.TryGetValue(c.Id, out var mat))
                {
                    mat.Labels = _alternatives.Select(a => a.Name).ToList();
                }
            }

            var result = AhpCalculator.SolveHierarchy(
                _criteria.ToList(),
                _alternatives.ToList(),
                _criteriaMatrix,
                _alternativeMatrices,
                _calculationOptions);

            var resultsWin = new ResultsWindow(
                result,
                _criteria.ToList(),
                _alternatives.ToList(),
                _criteriaMatrix,
                _alternativeMatrices)
            {
                Owner = this
            };

            resultsWin.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Помилка під час ієрархічного синтезу:\n{ex.Message}", "Помилка обчислень", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S)
        {
            SaveProjectToFile();
            e.Handled = true;
        }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.O)
        {
            OpenProjectFromFile();
            e.Handled = true;
        }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.E)
        {
            ExportReportToFile();
            e.Handled = true;
        }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.P)
        {
            MenuCompareProfiles_Click(this, new RoutedEventArgs());
            e.Handled = true;
        }
        else if (e.Key == Key.F1)
        {
            MenuHelpAbout_Click(this, new RoutedEventArgs());
            e.Handled = true;
        }
    }

    private void MenuCompareProfiles_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var win = new ProfileComparisonWindow(
                _criteria.ToList(),
                _alternatives.ToList(),
                _criteriaMatrix,
                _alternativeMatrices,
                _calculationOptions)
            {
                Owner = this
            };
            win.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Помилка відкриття порівняння профілів:\n{ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #region Menu Event Handlers & Project Storage

    private void MenuFileSaveAs_Click(object sender, RoutedEventArgs e)
    {
        SaveProjectToFile();
    }

    private void MenuFileOpen_Click(object sender, RoutedEventArgs e)
    {
        OpenProjectFromFile();
    }

    private void MenuFileExportReport_Click(object sender, RoutedEventArgs e)
    {
        ExportReportToFile();
    }

    private void MenuFileExit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void SyncCalculationOptionsUI()
    {
        bool isMethodical = _calculationOptions.IsMethodicalProfile;
        bool isClassic = _calculationOptions.IsClassicSaatyProfile;

        MenuProfileMethodical.IsChecked = isMethodical;
        MenuProfileClassic.IsChecked = isClassic;
        MenuProfileCustom.IsChecked = !isMethodical && !isClassic;

        MenuWeightColumnNorm.IsChecked = _calculationOptions.WeightMethod == WeightCalculationMethod.ColumnNormalization;
        MenuWeightGeometric.IsChecked = _calculationOptions.WeightMethod == WeightCalculationMethod.GeometricMean;
        MenuWeightPowerIteration.IsChecked = _calculationOptions.WeightMethod == WeightCalculationMethod.PrincipalEigenvector;

        MenuLambdaColSum.IsChecked = _calculationOptions.LambdaMethod == LambdaCalculationMethod.ColumnSumProduct;
        MenuLambdaRowRatio.IsChecked = _calculationOptions.LambdaMethod == LambdaCalculationMethod.RowRatioMean;
        MenuLambdaExact.IsChecked = _calculationOptions.LambdaMethod == LambdaCalculationMethod.ExactEigenvalue;

        MenuRiEmpirical.IsChecked = _calculationOptions.RiMode == RiCalculationMode.Empirical;
        MenuRiTable.IsChecked = _calculationOptions.RiMode == RiCalculationMode.SaatyTable;

        MenuThresholdUniform.IsChecked = _calculationOptions.ThresholdMode == ConsistencyThresholdMode.Uniform10Percent;
        MenuThresholdAdaptive.IsChecked = _calculationOptions.ThresholdMode == ConsistencyThresholdMode.SaatyAdaptive;

        TxtActiveProfile.Text = $"Профіль: {_calculationOptions.ProfileName}";

        string weightDesc = _calculationOptions.WeightMethod switch
        {
            WeightCalculationMethod.ColumnNormalization => "Нормалізація стовпців",
            WeightCalculationMethod.GeometricMean => "Геометричне середнє",
            WeightCalculationMethod.PrincipalEigenvector => "Степеневий метод",
            _ => _calculationOptions.WeightMethod.ToString()
        };

        string lambdaDesc = AhpCalculationOptions.GetLambdaMethodDescription(_calculationOptions.LambdaMethod);

        string riDesc = _calculationOptions.RiMode == RiCalculationMode.Empirical ? "Емпіричний RI" : "Табличний RI";
        string threshDesc = _calculationOptions.ThresholdMode == ConsistencyThresholdMode.Uniform10Percent ? "CR ≤ 10%" : "Адаптивний CR";

        TxtActiveProfileDesc.Text = $"Профіль: {_calculationOptions.ProfileName} • {weightDesc} • {lambdaDesc} • {riDesc} • {threshDesc}";
    }

    private void MenuProfileMethodical_Click(object sender, RoutedEventArgs e)
    {
        _calculationOptions = AhpCalculationOptions.MethodicalProfile.Clone();
        SyncCalculationOptionsUI();
        UpdateAllDiagnostics();
    }

    private void MenuProfileClassic_Click(object sender, RoutedEventArgs e)
    {
        _calculationOptions = AhpCalculationOptions.ClassicSaatyProfile.Clone();
        SyncCalculationOptionsUI();
        UpdateAllDiagnostics();
    }

    private void MenuWeightColumnNorm_Click(object sender, RoutedEventArgs e)
    {
        _calculationOptions.WeightMethod = WeightCalculationMethod.ColumnNormalization;
        SyncCalculationOptionsUI();
        UpdateAllDiagnostics();
    }

    private void MenuWeightGeometric_Click(object sender, RoutedEventArgs e)
    {
        _calculationOptions.WeightMethod = WeightCalculationMethod.GeometricMean;
        SyncCalculationOptionsUI();
        UpdateAllDiagnostics();
    }

    private void MenuWeightPowerIteration_Click(object sender, RoutedEventArgs e)
    {
        _calculationOptions.WeightMethod = WeightCalculationMethod.PrincipalEigenvector;
        SyncCalculationOptionsUI();
        UpdateAllDiagnostics();
    }

    private void MenuLambdaColSum_Click(object sender, RoutedEventArgs e)
    {
        _calculationOptions.LambdaMethod = LambdaCalculationMethod.ColumnSumProduct;
        SyncCalculationOptionsUI();
        UpdateAllDiagnostics();
    }

    private void MenuLambdaRowRatio_Click(object sender, RoutedEventArgs e)
    {
        _calculationOptions.LambdaMethod = LambdaCalculationMethod.RowRatioMean;
        SyncCalculationOptionsUI();
        UpdateAllDiagnostics();
    }

    private void MenuLambdaExact_Click(object sender, RoutedEventArgs e)
    {
        _calculationOptions.LambdaMethod = LambdaCalculationMethod.ExactEigenvalue;
        SyncCalculationOptionsUI();
        UpdateAllDiagnostics();
    }

    private void MenuRiEmpirical_Click(object sender, RoutedEventArgs e)
    {
        _calculationOptions.RiMode = RiCalculationMode.Empirical;
        SyncCalculationOptionsUI();
        UpdateAllDiagnostics();
    }

    private void MenuRiTable_Click(object sender, RoutedEventArgs e)
    {
        _calculationOptions.RiMode = RiCalculationMode.SaatyTable;
        SyncCalculationOptionsUI();
        UpdateAllDiagnostics();
    }

    private void MenuThresholdUniform_Click(object sender, RoutedEventArgs e)
    {
        _calculationOptions.ThresholdMode = ConsistencyThresholdMode.Uniform10Percent;
        SyncCalculationOptionsUI();
        UpdateAllDiagnostics();
    }

    private void MenuThresholdAdaptive_Click(object sender, RoutedEventArgs e)
    {
        _calculationOptions.ThresholdMode = ConsistencyThresholdMode.SaatyAdaptive;
        SyncCalculationOptionsUI();
        UpdateAllDiagnostics();
    }

    private void MenuThemeLight_Click(object sender, RoutedEventArgs e)
    {
        MenuThemeLight.IsChecked = true;
        MenuThemeDark.IsChecked = false;
        ThemeService.ApplyTheme(AppTheme.Light);
        BuildAlternativesCriteriaSelector();
        UpdateAllDiagnostics();
    }

    private void MenuThemeDark_Click(object sender, RoutedEventArgs e)
    {
        MenuThemeDark.IsChecked = true;
        MenuThemeLight.IsChecked = false;
        ThemeService.ApplyTheme(AppTheme.Dark);
        BuildAlternativesCriteriaSelector();
        UpdateAllDiagnostics();
    }

    private void MenuHelpAbout_Click(object sender, RoutedEventArgs e)
    {
        var aboutWindow = new AboutWindow
        {
            Owner = this
        };
        aboutWindow.ShowDialog();
    }

    private void SaveProjectToFile()
    {
        try
        {
            var sfd = new SaveFileDialog
            {
                Title = "Зберегти задачу МАІ як...",
                Filter = "Файли задач AHP (*.ahp.json;*.json)|*.ahp.json;*.json|Усі файли (*.*)|*.*",
                DefaultExt = ".ahp.json",
                FileName = $"AHP_Task_{DateTime.Now:yyyyMMdd_HHmm}.ahp.json"
            };

            if (sfd.ShowDialog(this) == true)
            {
                var data = new AhpProjectData
                {
                    Title = !string.IsNullOrWhiteSpace(_projectTitle) ? _projectTitle : "Задача багатокритеріального вибору",
                    Description = !string.IsNullOrWhiteSpace(_projectDescription)
                        ? _projectDescription
                        : $"Критеріїв: {_criteria.Count}, Альтернатив: {_alternatives.Count}",
                    Options = _calculationOptions.Clone(),
                    RiMode = _calculationOptions.RiMode,
                    Criteria = _criteria.ToList(),
                    Alternatives = _alternatives.ToList(),
                    CriteriaMatrix = _criteriaMatrix.ToJaggedArray(),
                    AlternativeMatrices = _alternativeMatrices.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.ToJaggedArray())
                };

                ProjectStorageService.SaveProject(sfd.FileName, data);
                TxtBottomStatus.Text = $"Задачу успішно збережено у файл: {Path.GetFileName(sfd.FileName)}";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Помилка збереження задачі:\n{ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenProjectFromFile()
    {
        try
        {
            var ofd = new OpenFileDialog
            {
                Title = "Завантажити задачу МАІ...",
                Filter = "Файли задач AHP (*.ahp.json;*.json)|*.ahp.json;*.json|Усі файли (*.*)|*.*"
            };

            if (ofd.ShowDialog(this) == true)
            {
                var data = ProjectStorageService.LoadProject(ofd.FileName);
                _projectTitle = !string.IsNullOrWhiteSpace(data.Title) ? data.Title : "Задача багатокритеріального вибору";
                _projectDescription = data.Description ?? string.Empty;

                // Транзакційне застосування: спочатку готуємо всі матриці локально
                var newCriteriaMatrix = new ComparisonMatrix(data.Criteria.Count, data.Criteria.Select(c => c.Name).ToList());
                if (data.CriteriaMatrix != null && data.CriteriaMatrix.Length > 0)
                {
                    newCriteriaMatrix.LoadFromJaggedArray(data.CriteriaMatrix);
                }

                var newAlternativeMatrices = new Dictionary<string, ComparisonMatrix>();
                foreach (var c in data.Criteria)
                {
                    var altMat = new ComparisonMatrix(data.Alternatives.Count, data.Alternatives.Select(a => a.Name).ToList());
                    if (data.AlternativeMatrices != null && data.AlternativeMatrices.TryGetValue(c.Id, out var jaggedMat))
                    {
                        altMat.LoadFromJaggedArray(jaggedMat);
                    }
                    newAlternativeMatrices[c.Id] = altMat;
                }

                // Усі структури успішно підготовлено без винятків — фіксуємо стан застосунку
                _criteria.Clear();
                foreach (var c in data.Criteria)
                {
                    _criteria.Add(c);
                }

                _alternatives.Clear();
                foreach (var a in data.Alternatives)
                {
                    _alternatives.Add(a);
                }

                _criteriaMatrix = newCriteriaMatrix;
                _alternativeMatrices = newAlternativeMatrices;
                _selectedCriterionId = _criteria.FirstOrDefault()?.Id ?? string.Empty;

                _calculationOptions = data.Options.Clone();
                SyncCalculationOptionsUI();

                UpdateCountsUI();
                BuildCriteriaMatrixGrid();
                BuildAlternativesCriteriaSelector();
                BuildAlternativeMatrixGrid();
                UpdateAllDiagnostics();

                if (data.Title.Contains("ЧЕРНЕТКА", StringComparison.OrdinalIgnoreCase))
                {
                    TxtBottomStatus.Text = $"⚠️ УВАГА: Завантажено «{data.Title}». Судження є попередніми та не обґрунтованими!";
                    TxtBottomStatus.Foreground = (Brush)FindResource("WarningOrange");
                }
                else
                {
                    TxtBottomStatus.Text = $"Завантажено задачу: {Path.GetFileName(ofd.FileName)} ({_criteria.Count} критеріїв, {_alternatives.Count} альтернатив).";
                    TxtBottomStatus.Foreground = (Brush)FindResource("TextDark");
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Помилка завантаження задачі:\n{ex.Message}", "Помилка завантаження", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportReportToFile()
    {
        try
        {
            var result = AhpCalculator.SolveHierarchy(
                _criteria.ToList(),
                _alternatives.ToList(),
                _criteriaMatrix,
                _alternativeMatrices,
                _calculationOptions);

            var sfd = new SaveFileDialog
            {
                Title = "Експорт повного звіту результатів МАІ",
                Filter = "Текстовий файл (*.txt)|*.txt|Усі файли (*.*)|*.*",
                DefaultExt = ".txt",
                FileName = $"AHP_Report_{DateTime.Now:yyyyMMdd_HHmm}.txt"
            };

            if (sfd.ShowDialog(this) == true)
            {
                string report = ProjectStorageService.GenerateFullReportText(result);
                File.WriteAllText(sfd.FileName, report, Encoding.UTF8);
                TxtBottomStatus.Text = $"Звіт успішно експортовано у файл: {Path.GetFileName(sfd.FileName)}";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не вдалося сформувати звіт:\n{ex.Message}", "Помилка експорту", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion
}