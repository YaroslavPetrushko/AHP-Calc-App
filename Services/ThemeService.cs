using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using MAI.Models;

namespace MAI.Services;

/// <summary>
/// Сервіс керування та динамічного перемикання кольорових тем (Світла / Темна).
/// Оновлює ресурси кольорів та пензлів (SolidColorBrush) у реальному часі на рівні Application.Current.Resources.
/// </summary>
public static class ThemeService
{
    public static AppTheme CurrentTheme { get; private set; } = AppTheme.Light;

    public static event Action<AppTheme>? ThemeChanged;

    public static void ApplyTheme(AppTheme theme)
    {
        CurrentTheme = theme;
        var res = Application.Current?.Resources;
        if (res == null) return;

        var palette = theme == AppTheme.Dark ? GetDarkPalette() : GetLightPalette();
        foreach (var (key, brush) in palette)
        {
            res[key] = brush;
        }

        // Apply WPF SystemColors overrides for native controls (Menus, Popups, Tooltips)
        ApplySystemColors(res, theme);

        ThemeChanged?.Invoke(theme);
    }

    private static Dictionary<string, Brush> GetLightPalette()
    {
        return new Dictionary<string, Brush>
        {
            ["AppBackground"] = CreateBrush("#F8FAFC"),
            ["CardBg"] = CreateBrush("#FFFFFF"),
            ["BorderColor"] = CreateBrush("#E2E8F0"),
            ["TextDark"] = CreateBrush("#0F172A"),
            ["TextMuted"] = CreateBrush("#64748B"),

            ["PrimaryBlue"] = CreateBrush("#2563EB"),
            ["PrimaryBlueHover"] = CreateBrush("#1D4ED8"),
            ["PrimaryBluePressed"] = CreateBrush("#1E40AF"),
            ["SuccessGreen"] = CreateBrush("#059669"),
            ["WarningOrange"] = CreateBrush("#D97706"),
            ["DangerRed"] = CreateBrush("#DC2626"),

            ["SecondaryBtnBg"] = CreateBrush("#F1F5F9"),
            ["SecondaryBtnFg"] = CreateBrush("#334155"),
            ["SecondaryBtnBorder"] = CreateBrush("#CBD5E1"),
            ["SecondaryBtnHoverBg"] = CreateBrush("#E2E8F0"),
            ["SecondaryBtnPressedBg"] = CreateBrush("#CBD5E1"),

            ["TabItemBg"] = CreateBrush("#E2E8F0"),
            ["TabItemSelectedBg"] = CreateBrush("#FFFFFF"),
            ["TabItemHoverBg"] = CreateBrush("#F1F5F9"),
            ["TabItemFg"] = CreateBrush("#64748B"),
            ["TabItemBorder"] = CreateBrush("#CBD5E1"),

            ["MatrixContainerBg"] = CreateBrush("#F8FAFC"),
            ["MatrixHeaderBg"] = CreateBrush("#F1F5F9"),
            ["MatrixHeaderFg"] = CreateBrush("#475569"),
            ["MatrixRowHeaderBg"] = CreateBrush("#F8FAFC"),
            ["MatrixInputBg"] = CreateBrush("#FFFFFF"),
            ["MatrixInputFocusedBg"] = CreateBrush("#EFF6FF"),
            ["MatrixInputReadOnlyBg"] = CreateBrush("#F8FAFC"),
            ["MatrixInputReadOnlyFg"] = CreateBrush("#64748B"),
            ["InputBorder"] = CreateBrush("#CBD5E1"),

            ["DataGridRowBg"] = CreateBrush("#FFFFFF"),
            ["DataGridAltRowBg"] = CreateBrush("#F8FAFC"),
            ["DataGridHeaderBg"] = CreateBrush("#F1F5F9"),
            ["DataGridHeaderFg"] = CreateBrush("#475569"),
            ["DataGridGridLine"] = CreateBrush("#F1F5F9"),
            ["DataGridSelectedBg"] = CreateBrush("#EFF6FF"),
            ["DataGridSelectedFg"] = CreateBrush("#1E40AF"),

            ["BannerBg"] = CreateBrush("#EFF6FF"),
            ["BannerBorder"] = CreateBrush("#BFDBFE"),
            ["BannerTitle"] = CreateBrush("#1E3A8A"),
            ["BannerSubtitle"] = CreateBrush("#475569"),

            ["BadgeBg"] = CreateBrush("#DBEAFE"),
            ["BadgeFg"] = CreateBrush("#1E40AF"),

            ["PillConsistentBg"] = CreateBrush("#DCFCE7"),
            ["PillConsistentBorder"] = CreateBrush("#86EFAC"),
            ["PillConsistentFg"] = CreateBrush("#15803D"),
            ["PillInconsistentBg"] = CreateBrush("#FEE2E2"),
            ["PillInconsistentBorder"] = CreateBrush("#FCA5A5"),
            ["PillInconsistentFg"] = CreateBrush("#B91C1C"),

            ["MenuBg"] = CreateBrush("#FFFFFF"),
            ["MenuFg"] = CreateBrush("#1E293B"),
            ["MenuHoverBg"] = CreateBrush("#F1F5F9"),

            ["ScrollBarTrackBg"] = CreateBrush("#F1F5F9"),
            ["ScrollBarThumbBg"] = CreateBrush("#CBD5E1"),
            ["ScrollBarThumbHover"] = CreateBrush("#94A3B8"),
            ["ScrollBarThumbPressed"] = CreateBrush("#64748B")
        };
    }

    private static Dictionary<string, Brush> GetDarkPalette()
    {
        return new Dictionary<string, Brush>
        {
            ["AppBackground"] = CreateBrush("#0F172A"),
            ["CardBg"] = CreateBrush("#1E293B"),
            ["BorderColor"] = CreateBrush("#334155"),
            ["TextDark"] = CreateBrush("#F8FAFC"),
            ["TextMuted"] = CreateBrush("#94A3B8"),

            ["PrimaryBlue"] = CreateBrush("#3B82F6"),
            ["PrimaryBlueHover"] = CreateBrush("#60A5FA"),
            ["PrimaryBluePressed"] = CreateBrush("#2563EB"),
            ["SuccessGreen"] = CreateBrush("#10B981"),
            ["WarningOrange"] = CreateBrush("#F59E0B"),
            ["DangerRed"] = CreateBrush("#EF4444"),

            ["SecondaryBtnBg"] = CreateBrush("#334155"),
            ["SecondaryBtnFg"] = CreateBrush("#F1F5F9"),
            ["SecondaryBtnBorder"] = CreateBrush("#475569"),
            ["SecondaryBtnHoverBg"] = CreateBrush("#475569"),
            ["SecondaryBtnPressedBg"] = CreateBrush("#1E293B"),

            ["TabItemBg"] = CreateBrush("#1E293B"),
            ["TabItemSelectedBg"] = CreateBrush("#0F172A"),
            ["TabItemHoverBg"] = CreateBrush("#334155"),
            ["TabItemFg"] = CreateBrush("#94A3B8"),
            ["TabItemBorder"] = CreateBrush("#334155"),

            ["MatrixContainerBg"] = CreateBrush("#0B132B"),
            ["MatrixHeaderBg"] = CreateBrush("#1E293B"),
            ["MatrixHeaderFg"] = CreateBrush("#94A3B8"),
            ["MatrixRowHeaderBg"] = CreateBrush("#182234"),
            ["MatrixInputBg"] = CreateBrush("#0F172A"),
            ["MatrixInputFocusedBg"] = CreateBrush("#1E3A8A"),
            ["MatrixInputReadOnlyBg"] = CreateBrush("#1E293B"),
            ["MatrixInputReadOnlyFg"] = CreateBrush("#64748B"),
            ["InputBorder"] = CreateBrush("#475569"),

            ["DataGridRowBg"] = CreateBrush("#1E293B"),
            ["DataGridAltRowBg"] = CreateBrush("#182234"),
            ["DataGridHeaderBg"] = CreateBrush("#0F172A"),
            ["DataGridHeaderFg"] = CreateBrush("#CBD5E1"),
            ["DataGridGridLine"] = CreateBrush("#334155"),
            ["DataGridSelectedBg"] = CreateBrush("#1E3A8A"),
            ["DataGridSelectedFg"] = CreateBrush("#93C5FD"),

            ["BannerBg"] = CreateBrush("#1E293B"),
            ["BannerBorder"] = CreateBrush("#2563EB"),
            ["BannerTitle"] = CreateBrush("#93C5FD"),
            ["BannerSubtitle"] = CreateBrush("#94A3B8"),

            ["BadgeBg"] = CreateBrush("#1E3A8A"),
            ["BadgeFg"] = CreateBrush("#93C5FD"),

            ["PillConsistentBg"] = CreateBrush("#064E3B"),
            ["PillConsistentBorder"] = CreateBrush("#059669"),
            ["PillConsistentFg"] = CreateBrush("#6EE7B7"),
            ["PillInconsistentBg"] = CreateBrush("#7F1D1D"),
            ["PillInconsistentBorder"] = CreateBrush("#DC2626"),
            ["PillInconsistentFg"] = CreateBrush("#FCA5A5"),

            ["MenuBg"] = CreateBrush("#1E293B"),
            ["MenuFg"] = CreateBrush("#F8FAFC"),
            ["MenuHoverBg"] = CreateBrush("#334155"),

            ["ScrollBarTrackBg"] = CreateBrush("#182234"),
            ["ScrollBarThumbBg"] = CreateBrush("#475569"),
            ["ScrollBarThumbHover"] = CreateBrush("#64748B"),
            ["ScrollBarThumbPressed"] = CreateBrush("#94A3B8")
        };
    }

    private static void ApplySystemColors(ResourceDictionary res, AppTheme theme)
    {
        if (theme == AppTheme.Dark)
        {
            res[SystemColors.MenuBrushKey] = CreateBrush("#1E293B");
            res[SystemColors.MenuTextBrushKey] = CreateBrush("#F8FAFC");
            res[SystemColors.MenuBarBrushKey] = CreateBrush("#1E293B");
            res[SystemColors.HighlightBrushKey] = CreateBrush("#334155");
            res[SystemColors.HighlightTextBrushKey] = CreateBrush("#F8FAFC");
            res[SystemColors.ControlBrushKey] = CreateBrush("#1E293B");
            res[SystemColors.ControlTextBrushKey] = CreateBrush("#F8FAFC");
            res[SystemColors.WindowBrushKey] = CreateBrush("#0F172A");
            res[SystemColors.WindowTextBrushKey] = CreateBrush("#F8FAFC");
        }
        else
        {
            res[SystemColors.MenuBrushKey] = CreateBrush("#FFFFFF");
            res[SystemColors.MenuTextBrushKey] = CreateBrush("#1E293B");
            res[SystemColors.MenuBarBrushKey] = CreateBrush("#FFFFFF");
            res[SystemColors.HighlightBrushKey] = CreateBrush("#EFF6FF");
            res[SystemColors.HighlightTextBrushKey] = CreateBrush("#1E40AF");
            res[SystemColors.ControlBrushKey] = CreateBrush("#FFFFFF");
            res[SystemColors.ControlTextBrushKey] = CreateBrush("#0F172A");
            res[SystemColors.WindowBrushKey] = CreateBrush("#FFFFFF");
            res[SystemColors.WindowTextBrushKey] = CreateBrush("#0F172A");
        }
    }

    private static SolidColorBrush CreateBrush(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}
