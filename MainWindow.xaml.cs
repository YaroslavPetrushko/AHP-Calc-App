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
    private RiCalculationMode _currentRiMode = RiCalculationMode.SaatyTable;

    // Cache textboxes for quick reciprocal access
    private TextBox[,] _critTextBoxes = new TextBox[0, 0];
    private TextBox[,] _altTextBoxes = new TextBox[0, 0];

    public MainWindow()
    {
        InitializeComponent();

        GridCriteria.ItemsSource = _criteria;
        GridAlternatives.ItemsSource = _alternatives;

        // Завантажуємо контрольний набір Лабораторних робіт №3–4 за замовчуванням
        LoadPreset(PresetService.GetLab34Preset());
    }

    private void LoadPreset(AhpProblemPreset preset)
    {
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
            tb.SetResourceReference(TextBox.BorderBrushProperty, "DangerRed");
        }
    }

    private void UpdateCriteriaDiagnostics()
    {
        var result = AhpCalculator.CalculateMatrix(_criteriaMatrix, "Матриця критеріїв", _currentRiMode);

        TxtCritWeightsPreview.Text = "w = [" + string.Join(", ", _criteria.Select((c, idx) =>
            $"{c.Id}: {result.Weights[idx]:F3} ({result.Weights[idx]:P1})")) + "]";

        TxtCritLambdaCI.Text = $"λmax = {result.LambdaMax:F4} | CI = {result.CI:F4}";
        string riLabel = _currentRiMode == RiCalculationMode.Empirical ? "RI(емп)" : "RI(табл)";
        TxtCritRIRatio.Text = $"{riLabel} = {result.ActiveRI:F3} | CR = {result.ActiveCR:P1} ({result.ActiveCR:F3})";

        if (result.IsConsistent)
        {
            PillCritStatus.SetResourceReference(Border.BackgroundProperty, "PillConsistentBg");
            PillCritStatus.SetResourceReference(Border.BorderBrushProperty, "PillConsistentBorder");
            TxtCritStatus.Text = $"Узгоджено (CR = {result.ActiveCR:P1} <= 10%)";
            TxtCritStatus.SetResourceReference(TextBlock.ForegroundProperty, "PillConsistentFg");
        }
        else
        {
            PillCritStatus.SetResourceReference(Border.BackgroundProperty, "PillInconsistentBg");
            PillCritStatus.SetResourceReference(Border.BorderBrushProperty, "PillInconsistentBorder");
            TxtCritStatus.Text = $"Неузгоджено (CR = {result.ActiveCR:P1} > 10%)";
            TxtCritStatus.SetResourceReference(TextBlock.ForegroundProperty, "PillInconsistentFg");
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
            tb.SetResourceReference(TextBox.BorderBrushProperty, "DangerRed");
        }
    }

    private void UpdateAlternativeDiagnostics()
    {
        if (string.IsNullOrEmpty(_selectedCriterionId) || !_alternativeMatrices.TryGetValue(_selectedCriterionId, out var altMatrix))
            return;

        var critObj = _criteria.FirstOrDefault(c => c.Id == _selectedCriterionId);
        string critTitle = critObj != null ? critObj.Name : _selectedCriterionId;

        TxtAltLocalPriTitle.Text = $"Локальні пріоритети за критерієм: «{critTitle}»";

        var result = AhpCalculator.CalculateMatrix(altMatrix, $"Альтернативи за {_selectedCriterionId}", _currentRiMode);

        TxtAltLocalPriPreview.Text = string.Join(", ", _alternatives.Select((a, idx) =>
            $"{a.Id}: {result.Weights[idx]:F3} ({result.Weights[idx]:P1})"));

        TxtAltLambdaCI.Text = $"λmax = {result.LambdaMax:F4} | CI = {result.CI:F4}";
        string riLabel = _currentRiMode == RiCalculationMode.Empirical ? "RI(емп)" : "RI(табл)";
        TxtAltRIRatio.Text = $"{riLabel} = {result.ActiveRI:F3} | CR = {result.ActiveCR:P1} ({result.ActiveCR:F3})";

        if (result.IsConsistent)
        {
            PillAltStatus.SetResourceReference(Border.BackgroundProperty, "PillConsistentBg");
            PillAltStatus.SetResourceReference(Border.BorderBrushProperty, "PillConsistentBorder");
            TxtAltStatus.Text = $"Узгоджено (CR = {result.ActiveCR:P1} <= 10%)";
            TxtAltStatus.SetResourceReference(TextBlock.ForegroundProperty, "PillConsistentFg");
        }
        else
        {
            PillAltStatus.SetResourceReference(Border.BackgroundProperty, "PillInconsistentBg");
            PillAltStatus.SetResourceReference(Border.BorderBrushProperty, "PillInconsistentBorder");
            TxtAltStatus.Text = $"Неузгоджено (CR = {result.ActiveCR:P1} > 10%)";
            TxtAltStatus.SetResourceReference(TextBlock.ForegroundProperty, "PillInconsistentFg");
        }
    }

    #endregion

    private void UpdateAllDiagnostics()
    {
        UpdateCriteriaDiagnostics();
        UpdateAlternativeDiagnostics();
    }

    #region Criteria & Alternatives Management

    private void BtnAddCriterion_Click(object sender, RoutedEventArgs e)
    {
        if (_criteria.Count >= 8)
        {
            MessageBox.Show("Рекомендована максимальна кількість критеріїв за методом Сааті — не більше 8 (для запобігання втрати узгодженості суджень).", "Обмеження розмірності", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        int nextNum = _criteria.Count + 1;
        var newCrit = new Criterion($"C{nextNum}", $"Критерій {nextNum}", "Новий критерій");
        _criteria.Add(newCrit);

        _criteriaMatrix.Resize(_criteria.Count, _criteria.Select(c => c.Name).ToList());
        _alternativeMatrices[newCrit.Id] = new ComparisonMatrix(_alternatives.Count, _alternatives.Select(a => a.Name).ToList());

        UpdateCountsUI();
        BuildCriteriaMatrixGrid();
        BuildAlternativesCriteriaSelector();
        UpdateAllDiagnostics();
    }

    private void BtnRemoveCriterion_Click(object sender, RoutedEventArgs e)
    {
        if (_criteria.Count <= 2)
        {
            MessageBox.Show("Для застосування методу багатокритеріального аналізу необхідно мінімум 2 критерії.", "Обмеження", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var selected = GridCriteria.SelectedItem as Criterion ?? _criteria.LastOrDefault();
        if (selected != null)
        {
            _criteria.Remove(selected);
            _alternativeMatrices.Remove(selected.Id);

            _criteriaMatrix.Resize(_criteria.Count, _criteria.Select(c => c.Name).ToList());

            if (_selectedCriterionId == selected.Id)
            {
                _selectedCriterionId = _criteria.FirstOrDefault()?.Id ?? string.Empty;
            }

            UpdateCountsUI();
            BuildCriteriaMatrixGrid();
            BuildAlternativesCriteriaSelector();
            BuildAlternativeMatrixGrid();
            UpdateAllDiagnostics();
        }
    }

    private void BtnAddAlternative_Click(object sender, RoutedEventArgs e)
    {
        if (_alternatives.Count >= 8)
        {
            MessageBox.Show("Рекомендована максимальна кількість альтернатив — до 8 варіантів.", "Обмеження розмірності", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        int nextNum = _alternatives.Count + 1;
        var newAlt = new Alternative($"A{nextNum}", $"Альтернатива {nextNum}", "Новий варіант");
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
    }

    private void BtnRemoveAlternative_Click(object sender, RoutedEventArgs e)
    {
        if (_alternatives.Count <= 2)
        {
            MessageBox.Show("Для порівняння необхідно мінімум 2 альтернативи.", "Обмеження", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var selected = GridAlternatives.SelectedItem as Alternative ?? _alternatives.LastOrDefault();
        if (selected != null)
        {
            _alternatives.Remove(selected);

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
        }
    }

    #endregion

    #region Preset Buttons

    private void BtnPresetLab34_Click(object sender, RoutedEventArgs e)
    {
        LoadPreset(PresetService.GetLab34Preset());
        MessageBox.Show("Завантажено контрольний тестовий набір даних Лабораторних робіт №3–4:\n• 4 критерії (Productivity, Functionality, Actuality, Cost)\n• 5 альтернатив (RTX 3050, 3070, 4060 Ti, 5060 Ti, RX 7700 XT)", "Пресет завантажено", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnPresetCoursework_Click(object sender, RoutedEventArgs e)
    {
        LoadPreset(PresetService.GetCourseworkPreset());
        MessageBox.Show("Завантажено тестовий набір даних Курсової роботи:\n• 4 критерії\n• 5 актуалізованих альтернатив (Blackwell / RDNA 4)", "Пресет завантажено", MessageBoxButton.OK, MessageBoxImage.Information);
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
            UpdateAllDiagnostics();
        }
    }

    private void BtnCalculate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var result = AhpCalculator.SolveHierarchy(
                _criteria.ToList(),
                _alternatives.ToList(),
                _criteriaMatrix,
                _alternativeMatrices,
                _currentRiMode);

            var resultsWin = new ResultsWindow(result)
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
        else if (e.Key == Key.F1)
        {
            MenuHelpAbout_Click(this, new RoutedEventArgs());
            e.Handled = true;
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

    private void MenuSettingsRiTable_Click(object sender, RoutedEventArgs e)
    {
        _currentRiMode = RiCalculationMode.SaatyTable;
        MenuSettingsRiTable.IsChecked = true;
        MenuSettingsRiEmpirical.IsChecked = false;
        UpdateAllDiagnostics();
    }

    private void MenuSettingsRiEmpirical_Click(object sender, RoutedEventArgs e)
    {
        _currentRiMode = RiCalculationMode.Empirical;
        MenuSettingsRiEmpirical.IsChecked = true;
        MenuSettingsRiTable.IsChecked = false;
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
        MessageBox.Show(
            "Метод аналізу ієрархій Томаса Сааті (AHP / МАІ)\n" +
            "Версія: 1.1 (.NET 10 WPF)\n\n" +
            "Системний аналіз та теорія прийняття рішень.\n" +
            "Підтримує попарні порівняння, перевірку узгодженості (CI, RI, CR), " +
            "ієрархічний синтез, збереження задач у JSON та експорт звітів.",
            "Про програму",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
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
                    Title = "Задача багатокритеріального вибору",
                    Description = $"Критеріїв: {_criteria.Count}, Альтернатив: {_alternatives.Count}",
                    RiMode = _currentRiMode,
                    Criteria = _criteria.ToList(),
                    Alternatives = _alternatives.ToList(),
                    CriteriaMatrix = _criteriaMatrix.ToJaggedArray(),
                    AlternativeMatrices = _alternativeMatrices.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.ToJaggedArray())
                };

                ProjectStorageService.SaveProject(sfd.FileName, data);
                TxtBottomStatus.Text = $"Задачу успішно збережено у файл: {Path.GetFileName(sfd.FileName)}";
                MessageBox.Show($"Задачу успішно збережено у файл:\n{sfd.FileName}", "Збереження виконано", MessageBoxButton.OK, MessageBoxImage.Information);
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

                _criteriaMatrix = new ComparisonMatrix(_criteria.Count, _criteria.Select(c => c.Name).ToList());
                if (data.CriteriaMatrix != null && data.CriteriaMatrix.Length > 0)
                {
                    _criteriaMatrix.LoadFromJaggedArray(data.CriteriaMatrix);
                }

                _alternativeMatrices.Clear();
                foreach (var c in _criteria)
                {
                    var altMat = new ComparisonMatrix(_alternatives.Count, _alternatives.Select(a => a.Name).ToList());
                    if (data.AlternativeMatrices != null && data.AlternativeMatrices.TryGetValue(c.Id, out var jaggedMat))
                    {
                        altMat.LoadFromJaggedArray(jaggedMat);
                    }
                    _alternativeMatrices[c.Id] = altMat;
                }

                _selectedCriterionId = _criteria.FirstOrDefault()?.Id ?? string.Empty;

                _currentRiMode = data.RiMode;
                if (_currentRiMode == RiCalculationMode.Empirical)
                {
                    MenuSettingsRiEmpirical.IsChecked = true;
                    MenuSettingsRiTable.IsChecked = false;
                }
                else
                {
                    MenuSettingsRiTable.IsChecked = true;
                    MenuSettingsRiEmpirical.IsChecked = false;
                }

                UpdateCountsUI();
                BuildCriteriaMatrixGrid();
                BuildAlternativesCriteriaSelector();
                BuildAlternativeMatrixGrid();
                UpdateAllDiagnostics();

                TxtBottomStatus.Text = $"Завантажено задачу: {Path.GetFileName(ofd.FileName)} ({_criteria.Count} критеріїв, {_alternatives.Count} альтернатив).";
                MessageBox.Show($"Задачу успішно завантажено з файлу:\n{ofd.FileName}\n\n• Критеріїв: {_criteria.Count}\n• Альтернатив: {_alternatives.Count}", "Завантаження успішне", MessageBoxButton.OK, MessageBoxImage.Information);
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
                _currentRiMode);

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
                MessageBox.Show($"Звіт успішно сформовано та експортовано:\n{sfd.FileName}", "Експорт звіту", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не вдалося сформувати звіт:\n{ex.Message}", "Помилка експорту", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion
}