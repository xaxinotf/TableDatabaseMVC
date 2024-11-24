using Microsoft.AspNetCore.Mvc;
using TableDatabaseMVC.Models;
using TableDatabaseMVC.Services;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace TableDatabaseMVC.Controllers
{
    public class TablesController : Controller
    {
        private readonly TableService _tableService;
        private readonly ILogger<TablesController> _logger;
        private const string FilePath = "Data/database.json";

        public TablesController(TableService tableService, ILogger<TablesController> logger)
        {
            _tableService = tableService;
            _logger = logger;
        }

        // GET: Tables
        public IActionResult Index()
        {
            var tables = _tableService.LoadTables();
            return View(tables);
        }

        // GET: Tables/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Tables/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Table table)
        {
            if (ModelState.IsValid)
            {
                _tableService.AddTable(table);
                LogAndSaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(table);
        }

        // GET: Tables/Edit?name=TableName
        public IActionResult Edit(string name)
        {
            var table = _tableService.LoadTables().FirstOrDefault(t => t.Name == name);
            if (table == null)
            {
                return NotFound();
            }
            return View(table);
        }

        // POST: Tables/Edit?originalName=OldName
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(string originalName, Table updatedTable)
        {
            if (ModelState.IsValid)
            {
                var tables = _tableService.LoadTables();
                var table = tables.FirstOrDefault(t => t.Name == originalName);
                if (table == null)
                {
                    return NotFound();
                }

                // Якщо ім'я таблиці змінилося, перевірте унікальність
                if (!originalName.Equals(updatedTable.Name, StringComparison.OrdinalIgnoreCase) &&
                    tables.Any(t => t.Name.Equals(updatedTable.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    ModelState.AddModelError("Name", "A table with this name already exists.");
                    return View(updatedTable);
                }

                _tableService.UpdateTable(originalName, updatedTable);
                LogAndSaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(updatedTable);
        }

        // POST: Tables/RenameColumn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RenameColumn(string tableName, int columnIndex, string newName)
        {
            var table = _tableService.LoadTables().FirstOrDefault(t => t.Name == tableName);
            if (table == null || columnIndex < 0 || columnIndex >= table.Columns.Count)
            {
                return NotFound();
            }

            if (table.Columns.Any(c => c.Name == newName))
            {
                ModelState.AddModelError(string.Empty, "A column with this name already exists.");
                return View("Edit", table);
            }

            table.Columns[columnIndex].Name = newName;
            _tableService.UpdateTable(originalName: tableName, updatedTable: table);
            LogAndSaveChanges();

            return RedirectToAction("Edit", new { name = tableName });
        }

        // POST: Tables/MoveColumn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MoveColumn(string tableName, int columnIndex, int newIndex)
        {
            var table = _tableService.LoadTables().FirstOrDefault(t => t.Name == tableName);
            if (table == null || columnIndex < 0 || columnIndex >= table.Columns.Count || newIndex < 0 || newIndex > table.Columns.Count)
            {
                return NotFound();
            }

            var column = table.Columns[columnIndex];
            table.Columns.RemoveAt(columnIndex);
            table.Columns.Insert(newIndex, column);

            // Update rows to match new column order
            foreach (var row in table.Rows)
            {
                var value = row.Values[columnIndex];
                row.Values.RemoveAt(columnIndex);
                row.Values.Insert(newIndex, value);
            }

            _tableService.UpdateTable(originalName: tableName, updatedTable: table);
            LogAndSaveChanges();

            return RedirectToAction("Edit", new { name = tableName });
        }

        // GET: Tables/Delete?name=TableName
        public IActionResult Delete(string name)
        {
            var table = _tableService.LoadTables().FirstOrDefault(t => t.Name == name);
            if (table == null)
            {
                return NotFound();
            }
            return View(table);
        }

        // POST: Tables/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(string name)
        {
            var tables = _tableService.LoadTables();
            var table = tables.FirstOrDefault(t => t.Name == name);
            if (table == null)
            {
                return NotFound();
            }

            tables.Remove(table);
            _tableService.SaveTables(tables);
            LogAndSaveChanges();

            return RedirectToAction(nameof(Index));
        }


        // GET: Tables/Details?name=TableName
        public IActionResult Details(string name)
        {
            var table = _tableService.LoadTables().FirstOrDefault(t => t.Name == name);
            if (table == null)
            {
                return NotFound();
            }
            return View(table);
        }

        // GET: Tables/AddRow?tableName=TableName
        public IActionResult AddRow(string tableName)
        {
            var table = _tableService.LoadTables().FirstOrDefault(t => t.Name == tableName);
            if (table == null)
            {
                return NotFound();
            }
            return View(table);
        }

        // POST: Tables/AddRow
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddRow(string tableName, List<string> values)
        {
            var table = _tableService.LoadTables().FirstOrDefault(t => t.Name == tableName);
            if (table == null)
            {
                return NotFound();
            }

            if (values.Count != table.Columns.Count)
            {
                ModelState.AddModelError(string.Empty, "Number of values does not match number of columns.");
                return View(table);
            }

            var row = new Row();
            for (int i = 0; i < values.Count; i++)
            {
                var columnType = table.Columns[i].Type;
                object value = null;
                try
                {
                    switch (columnType)
                    {
                        case "integer":
                            value = int.Parse(values[i]);
                            break;
                        case "real":
                            value = double.Parse(values[i]);
                            break;
                        case "char":
                            value = char.Parse(values[i]);
                            break;
                        case "string":
                            value = values[i];
                            break;
                        case "$":
                            value = decimal.Parse(values[i]);
                            if ((decimal)value > 10000000000000.00m)
                            {
                                throw new FormatException("Value exceeds the maximum allowed amount.");
                            }
                            break;
                        case "$Invl":
                            var parts = values[i].Split('-');
                            if (parts.Length != 2 ||
                                !double.TryParse(parts[0], out double start) ||
                                !double.TryParse(parts[1], out double end))
                            {
                                throw new FormatException("Invalid interval format.");
                            }
                            value = new { Start = start, End = end };
                            break;
                        default:
                            value = values[i];
                            break;
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"Error parsing column '{table.Columns[i].Name}': {ex.Message}");
                    return View(table);
                }
                row.Values.Add(value);
            }

            table.Rows.Add(row);
            _tableService.UpdateTable(originalName: tableName, updatedTable: table);
            LogAndSaveChanges();

            return RedirectToAction("Details", new { name = tableName });
        }

        // GET: Tables/EditRow?tableName=TableName&rowIndex=0
        public IActionResult EditRow(string tableName, int rowIndex)
        {
            var table = _tableService.LoadTables().FirstOrDefault(t => t.Name == tableName);
            if (table == null || rowIndex < 0 || rowIndex >= table.Rows.Count)
            {
                return NotFound();
            }

            var row = table.Rows[rowIndex];
            ViewBag.RowIndex = rowIndex;
            return View(table);
        }

        // POST: Tables/EditRow
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditRow(string tableName, int rowIndex, List<string> values)
        {
            var table = _tableService.LoadTables().FirstOrDefault(t => t.Name == tableName);
            if (table == null || rowIndex < 0 || rowIndex >= table.Rows.Count)
            {
                return NotFound();
            }

            if (values.Count != table.Columns.Count)
            {
                ModelState.AddModelError(string.Empty, "Number of values does not match number of columns.");
                ViewBag.RowIndex = rowIndex;
                return View(table);
            }

            var row = new Row();
            for (int i = 0; i < values.Count; i++)
            {
                var columnType = table.Columns[i].Type;
                object value = null;
                try
                {
                    switch (columnType)
                    {
                        case "integer":
                            value = int.Parse(values[i]);
                            break;
                        case "real":
                            value = double.Parse(values[i]);
                            break;
                        case "char":
                            value = char.Parse(values[i]);
                            break;
                        case "string":
                            value = values[i];
                            break;
                        case "$":
                            value = decimal.Parse(values[i]);
                            if ((decimal)value > 10000000000000.00m)
                            {
                                throw new FormatException("Value exceeds the maximum allowed amount.");
                            }
                            break;
                        case "$Invl":
                            var parts = values[i].Split('-');
                            if (parts.Length != 2 ||
                                !double.TryParse(parts[0], out double start) ||
                                !double.TryParse(parts[1], out double end))
                            {
                                throw new FormatException("Invalid interval format.");
                            }
                            value = new { Start = start, End = end };
                            break;
                        default:
                            value = values[i];
                            break;
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"Error parsing column '{table.Columns[i].Name}': {ex.Message}");
                    ViewBag.RowIndex = rowIndex;
                    return View(table);
                }
                row.Values.Add(value);
            }

            table.Rows[rowIndex] = row;
            _tableService.UpdateTable(originalName: tableName, updatedTable: table);
            LogAndSaveChanges();

            return RedirectToAction("Details", new { name = tableName });
        }

        // GET: Tables/DeleteRow?tableName=TableName&rowIndex=0
        public IActionResult DeleteRow(string tableName, int rowIndex)
        {
            var table = _tableService.LoadTables().FirstOrDefault(t => t.Name == tableName);
            if (table == null || rowIndex < 0 || rowIndex >= table.Rows.Count)
            {
                return NotFound();
            }

            var row = table.Rows[rowIndex];
            ViewBag.RowIndex = rowIndex;
            return View(table);
        }

        // POST: Tables/DeleteRowConfirmed
        [HttpPost, ActionName("DeleteRow")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteRowConfirmed(string tableName, int rowIndex)
        {
            var table = _tableService.LoadTables().FirstOrDefault(t => t.Name == tableName);
            if (table == null || rowIndex < 0 || rowIndex >= table.Rows.Count)
            {
                return NotFound();
            }

            table.Rows.RemoveAt(rowIndex);
            _tableService.UpdateTable(originalName: tableName, updatedTable: table);
            LogAndSaveChanges();

            return RedirectToAction("Details", new { name = tableName });
        }

        private void LogAndSaveChanges()
        {
            var tables = _tableService.LoadTables();
            var json = JsonSerializer.Serialize(tables, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(FilePath, json);
        }
    }
}
