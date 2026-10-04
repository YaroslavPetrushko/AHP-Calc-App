using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9")),
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1")),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(6, 4, 6, 4),
            Margin = new Thickness(1)
        };
        corner.Child = new TextBlock
        {
            Text = "Критерій",
            FontWeight = FontWeights.Bold,
            FontSize = 11,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569")),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        Grid.SetRow(corner, 0);
        Grid.SetColumn(corner, 0);
        GridCritMatrixContainer.Children.Add(corner);

        // Column headers
        for (int j = 0; j < n; j++)
        {
            var header = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1")),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(4),
                Margin = new Thickness(1)
            };
            header.Child = new TextBlock
            {
                Text = _criteria[j].Id,
                ToolTip = _criteria[j].Name,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
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
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1")),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(1)
            };
            rowHeader.Child = new TextBlock
            {
                Text = $"{_criteria[i].Id} - {_criteria[i].Name}",
                ToolTip = _criteria[i].Description,
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
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
                    tb.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"));
                    tb.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
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
                tb.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));

                // Sync reciprocal cell
                if (_critTextBoxes != null && row < _critTextBoxes.GetLength(0) && col < _critTextBoxes.GetLength(1))
                {
                    var recipTb = _critTextBoxes[col, row];
                    if (recipTb != null)
                    {
                        recipTb.Text = ComparisonMatrix.FormatSaatyValue(1.0 / val);
                        recipTb.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));
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
            tb.BorderBrush = (SolidColorBrush)FindResource("DangerRed");
        }
    }

    private void UpdateCriteriaDiagnostics()
    {
        var result = AhpCalculator.CalculateMatrix(_criteriaMatrix, "Матриця критеріїв");

        TxtCritWeightsPreview.Text = "w = [" + string.Join(", ", _criteria.Select((c, idx) =>
            $"{c.Id}: {result.Weights[idx]:F3} ({result.Weights[idx]:P1})")) + "]";

        TxtCritLambdaCI.Text = $"λmax = {result.LambdaMax:F4} | CI = {result.CI:F4}";
        TxtCritRIRatio.Text = $"RI(табл) = {result.RITable:F2} | CR = {result.CRTable:P1} ({result.CRTable:F3})";

        if (result.IsConsistent)
        {
            PillCritStatus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DCFCE7"));
            PillCritStatus.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#86EFAC"));
            TxtCritStatus.Text = $"Узгоджено (CR = {result.CRTable:P1} <= 10%)";
            TxtCritStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15803D"));
        }
        else
        {
            PillCritStatus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEE2E2"));
            PillCritStatus.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCA5A5"));
            TxtCritStatus.Text = $"Неузгоджено (CR = {result.CRTable:P1} > 10%)";
            TxtCritStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C"));
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
                btn.Background = (SolidColorBrush)FindResource("PrimaryBlue");
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
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9")),
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1")),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(6, 4, 6, 4),
            Margin = new Thickness(1)
        };
        corner.Child = new TextBlock
        {
            Text = "Альтернатива",
            FontWeight = FontWeights.Bold,
            FontSize = 11,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569")),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        Grid.SetRow(corner, 0);
        Grid.SetColumn(corner, 0);
        GridAltMatrixContainer.Children.Add(corner);

        // Column headers
        for (int j = 0; j < m; j++)
        {
            var header = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1")),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(4),
                Margin = new Thickness(1)
            };
            header.Child = new TextBlock
            {
                Text = _alternatives[j].Id,
                ToolTip = _alternatives[j].Name,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
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
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1")),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(1)
            };
            rowHeader.Child = new TextBlock
            {
                Text = $"{_alternatives[i].Id} - {_alternatives[i].Name}",
                ToolTip = _alternatives[i].Description,
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
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
                    tb.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"));
                    tb.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
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
                tb.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));

                // Sync reciprocal cell
                if (_altTextBoxes != null && row < _altTextBoxes.GetLength(0) && col < _altTextBoxes.GetLength(1))
                {
                    var recipTb = _altTextBoxes[col, row];
                    if (recipTb != null)
                    {
                        recipTb.Text = ComparisonMatrix.FormatSaatyValue(1.0 / val);
                        recipTb.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));
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
            tb.BorderBrush = (SolidColorBrush)FindResource("DangerRed");
        }
    }

    private void UpdateAlternativeDiagnostics()
    {
        if (string.IsNullOrEmpty(_selectedCriterionId) || !_alternativeMatrices.TryGetValue(_selectedCriterionId, out var altMatrix))
            return;

        var critObj = _criteria.FirstOrDefault(c => c.Id == _selectedCriterionId);
        string critTitle = critObj != null ? critObj.Name : _selectedCriterionId;

        TxtAltLocalPriTitle.Text = $"Локальні пріоритети за критерієм: «{critTitle}»";

        var result = AhpCalculator.CalculateMatrix(altMatrix, $"Альтернативи за {_selectedCriterionId}");

        TxtAltLocalPriPreview.Text = string.Join(", ", _alternatives.Select((a, idx) =>
            $"{a.Id}: {result.Weights[idx]:F3} ({result.Weights[idx]:P1})"));

        TxtAltLambdaCI.Text = $"λmax = {result.LambdaMax:F4} | CI = {result.CI:F4}";
        TxtAltRIRatio.Text = $"RI(табл) = {result.RITable:F2} | CR = {result.CRTable:P1} ({result.CRTable:F3})";

        if (result.IsConsistent)
        {
            PillAltStatus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DCFCE7"));
            PillAltStatus.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#86EFAC"));
            TxtAltStatus.Text = $"Узгоджено (CR = {result.CRTable:P1} <= 10%)";
            TxtAltStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15803D"));
        }
        else
        {
            PillAltStatus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEE2E2"));
            PillAltStatus.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCA5A5"));
            TxtAltStatus.Text = $"Неузгоджено (CR = {result.CRTable:P1} > 10%)";
            TxtAltStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C"));
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
                _alternativeMatrices);

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
}