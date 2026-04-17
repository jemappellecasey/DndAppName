namespace DndApp.Api.Data;

public sealed class DatabaseOptions
{
    public string Provider { get; set; } = "sqlite";
    public DatabaseConnectionStrings ConnectionStrings { get; set; } = new();
}

public sealed class DatabaseConnectionStrings
{
    public string Sqlite { get; set; } = "Data Source=dndapp.sqlite";
    public string Postgres { get; set; } = string.Empty;
}
