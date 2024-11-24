using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TableDatabaseMVC.Models;
using System.Linq;

namespace TableDatabaseMVC.Services
{
    public class TableService
    {
        private readonly string FilePath;

        public TableService()
        {
            // Переконайтеся, що папка "Data" існує. Якщо ні, створіть її.
            var dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Data");
            if (!Directory.Exists(dataDirectory))
            {
                Directory.CreateDirectory(dataDirectory);
            }

            FilePath = Path.Combine(dataDirectory, "tables.json");
        }

        public List<Table> LoadTables()
        {
            if (!File.Exists(FilePath))
                return new List<Table>();

            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<List<Table>>(json) ?? new List<Table>();
        }

        public void SaveTables(List<Table> tables)
        {
            var json = JsonSerializer.Serialize(tables, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }

        public void AddTable(Table table)
        {
            var tables = LoadTables();
            tables.Add(table);
            SaveTables(tables);
        }

        public void DeleteTable(string tableName)
        {
            var tables = LoadTables();
            var table = tables.FirstOrDefault(t => t.Name.Equals(tableName, StringComparison.OrdinalIgnoreCase));
            if (table != null)
            {
                tables.Remove(table);
                SaveTables(tables);
            }
        }

        public void UpdateTable(string originalName, Table updatedTable)
        {
            var tables = LoadTables();
            var index = tables.FindIndex(t => t.Name.Equals(originalName, StringComparison.OrdinalIgnoreCase));
            if (index != -1)
            {
                tables[index] = updatedTable;
                SaveTables(tables);
            }
        }

        public void DeleteRow(string tableName, int rowIndex)
        {
            var tables = LoadTables();
            var table = tables.FirstOrDefault(t => t.Name.Equals(tableName, StringComparison.OrdinalIgnoreCase));
            if (table != null && rowIndex >= 0 && rowIndex < table.Rows.Count)
            {
                table.Rows.RemoveAt(rowIndex);
                SaveTables(tables);
            }
        }
    }
}
