namespace TableDatabaseMVC.Models
{
    public class Table
    {
        public string Name { get; set; }
        public List<Column> Columns { get; set; } = new();
        public List<Row> Rows { get; set; } = new();
    }
}
