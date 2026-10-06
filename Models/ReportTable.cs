using System;
using System.Collections.Generic;

namespace MAI.Models;

/// <summary>
/// Уніфікована структура звітної таблиці для експорту в CSV, Markdown та HTML.
/// </summary>
public class ReportTable
{
    public string Title { get; set; } = string.Empty;
    public string[] Headers { get; set; } = Array.Empty<string>();
    public List<string[]> Rows { get; set; } = new();
    public string? Note { get; set; }

    public ReportTable() { }

    public ReportTable(string title, string[] headers, List<string[]> rows, string? note = null)
    {
        Title = title;
        Headers = headers;
        Rows = rows;
        Note = note;
    }
}
