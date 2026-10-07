using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MAI.Models;

namespace MAI.Services;

/// <summary>
/// Експортер табличних звітів у формати CSV, Markdown та HTML.
/// </summary>
public static class ReportExporter
{
    /// <summary>
    /// Рендеринг таблиць у формат CSV (роздільник ';', поля екрануються лапками, починається з BOM).
    /// </summary>
    public static string ToCsv(IEnumerable<ReportTable> tables)
    {
        var sb = new StringBuilder("\uFEFF");

        bool first = true;
        foreach (var t in tables)
        {
            if (!first)
            {
                sb.AppendLine();
            }
            first = false;

            // Заголовок таблиці в окремому рядку
            sb.AppendLine(EscapeCsvField(t.Title));

            // Заголовки стовпців
            sb.AppendLine(string.Join(";", t.Headers.Select(EscapeCsvField)));

            // Рядки даних
            foreach (var row in t.Rows)
            {
                sb.AppendLine(string.Join(";", row.Select(EscapeCsvField)));
            }

            // Примітка, якщо є
            if (!string.IsNullOrWhiteSpace(t.Note))
            {
                sb.AppendLine(EscapeCsvField($"Примітка: {t.Note}"));
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Запис CSV у файл із кодуванням UTF-8 з маркером послідовності байтів (BOM).
    /// </summary>
    public static void SaveCsv(string filePath, IEnumerable<ReportTable> tables)
    {
        string content = ToCsv(tables);
        File.WriteAllText(filePath, content, new UTF8Encoding(false));
    }

    /// <summary>
    /// Рендеринг таблиць у формат Markdown із pipe-таблицями (`|---|---|`) та екрануванням `|`.
    /// </summary>
    public static string ToMarkdown(IEnumerable<ReportTable> tables)
    {
        var sb = new StringBuilder();

        bool first = true;
        foreach (var t in tables)
        {
            if (!first)
            {
                sb.AppendLine();
            }
            first = false;

            sb.AppendLine($"### {t.Title}");
            sb.AppendLine();

            // Заголовки
            sb.AppendLine("| " + string.Join(" | ", t.Headers.Select(EscapeMarkdownCell)) + " |");

            // Роздільник pipe таблиці
            sb.AppendLine("|" + string.Join("|", t.Headers.Select(_ => "---")) + "|");

            // Рядки
            foreach (var row in t.Rows)
            {
                sb.AppendLine("| " + string.Join(" | ", row.Select(EscapeMarkdownCell)) + " |");
            }

            // Примітка
            if (!string.IsNullOrWhiteSpace(t.Note))
            {
                sb.AppendLine();
                sb.AppendLine($"*Примітка: {t.Note}*");
            }

            sb.AppendLine();
        }

        return sb.ToString().TrimEnd() + Environment.NewLine;
    }

    /// <summary>
    /// Запис Markdown у файл (UTF-8).
    /// </summary>
    public static void SaveMarkdown(string filePath, IEnumerable<ReportTable> tables)
    {
        string content = ToMarkdown(tables);
        File.WriteAllText(filePath, content, Encoding.UTF8);
    }

    /// <summary>
    /// Рендеринг повного HTML документа зі збереженням структури таблиць для Word (Times New Roman 12pt).
    /// </summary>
    public static string ToHtml(IEnumerable<ReportTable> tables)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"uk\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"utf-8\">");
        sb.AppendLine("  <title>Звіт МАІ (AHP)</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine("    body { font-family: 'Times New Roman', Times, serif; font-size: 12pt; line-height: 1.3; color: #000; margin: 20px; }");
        sb.AppendLine("    table { border-collapse: collapse; width: 100%; margin-bottom: 24px; }");
        sb.AppendLine("    th, td { border: 1px solid #000; padding: 4px 8px; text-align: left; vertical-align: top; }");
        sb.AppendLine("    th { background-color: #f2f2f2; font-weight: bold; }");
        sb.AppendLine("    p.table-title { font-weight: bold; font-size: 12pt; margin: 16px 0 6px 0; }");
        sb.AppendLine("    p.table-note { font-style: italic; font-size: 11pt; margin: 4px 0 16px 0; color: #333; }");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        foreach (var t in tables)
        {
            sb.AppendLine($"  <p class=\"table-title\"><b>{System.Net.WebUtility.HtmlEncode(t.Title)}</b></p>");
            sb.AppendLine("  <table border=\"1\" cellspacing=\"0\" cellpadding=\"4\">");

            // Headers
            sb.AppendLine("    <thead>");
            sb.AppendLine("      <tr>");
            foreach (var h in t.Headers)
            {
                sb.AppendLine($"        <th>{System.Net.WebUtility.HtmlEncode(h)}</th>");
            }
            sb.AppendLine("      </tr>");
            sb.AppendLine("    </thead>");

            // Rows
            sb.AppendLine("    <tbody>");
            foreach (var row in t.Rows)
            {
                sb.AppendLine("      <tr>");
                foreach (var cell in row)
                {
                    sb.AppendLine($"        <td>{System.Net.WebUtility.HtmlEncode(cell)}</td>");
                }
                sb.AppendLine("      </tr>");
            }
            sb.AppendLine("    </tbody>");
            sb.AppendLine("  </table>");

            if (!string.IsNullOrWhiteSpace(t.Note))
            {
                sb.AppendLine($"  <p class=\"table-note\"><i>Примітка: {System.Net.WebUtility.HtmlEncode(t.Note)}</i></p>");
            }
        }

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    /// <summary>
    /// Запис HTML у файл (UTF-8).
    /// </summary>
    public static void SaveHtml(string filePath, IEnumerable<ReportTable> tables)
    {
        string content = ToHtml(tables);
        File.WriteAllText(filePath, content, Encoding.UTF8);
    }

    private static string EscapeCsvField(string field)
    {
        if (field == null) return string.Empty;
        if (field.Contains(';') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }

    private static string EscapeMarkdownCell(string cell)
    {
        if (cell == null) return string.Empty;
        return cell.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
    }
}
