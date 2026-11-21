using System.Text.Encodings.Web;
using System.Text.Unicode;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All));

var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING")
    ?? "Host=localhost;Port=5432;Database=app;Username=postgres;Password=1234";
builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));

var app = builder.Build();

var dataSource = app.Services.GetRequiredService<NpgsqlDataSource>();
await using (var create = dataSource.CreateCommand(
    "CREATE TABLE IF NOT EXISTS tasks (id SERIAL PRIMARY KEY, title TEXT NOT NULL, done BOOLEAN NOT NULL DEFAULT FALSE)"))
{
    await create.ExecuteNonQueryAsync();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/tasks", async (NpgsqlDataSource db) =>
{
    var list = new List<TaskItem>();
    await using var cmd = db.CreateCommand("SELECT id, title, done FROM tasks ORDER BY id");
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        list.Add(new TaskItem(reader.GetInt32(0), reader.GetString(1), reader.GetBoolean(2)));
    }
    return Results.Ok(list);
});

app.MapGet("/tasks/{id:int}", async (int id, NpgsqlDataSource db) =>
{
    await using var cmd = db.CreateCommand("SELECT id, title, done FROM tasks WHERE id = $1");
    cmd.Parameters.AddWithValue(id);
    await using var reader = await cmd.ExecuteReaderAsync();
    if (!await reader.ReadAsync())
    {
        return Results.NotFound();
    }
    return Results.Ok(new TaskItem(reader.GetInt32(0), reader.GetString(1), reader.GetBoolean(2)));
});

app.MapPost("/tasks", async (TaskInput input, NpgsqlDataSource db) =>
{
    await using var cmd = db.CreateCommand(
        "INSERT INTO tasks (title, done) VALUES ($1, $2) RETURNING id");
    cmd.Parameters.AddWithValue(input.Title);
    cmd.Parameters.AddWithValue(input.Done);
    var id = (int)(await cmd.ExecuteScalarAsync())!;
    return Results.Created($"/tasks/{id}", new TaskItem(id, input.Title, input.Done));
});

app.MapPut("/tasks/{id:int}", async (int id, TaskInput input, NpgsqlDataSource db) =>
{
    await using var cmd = db.CreateCommand(
        "UPDATE tasks SET title = $1, done = $2 WHERE id = $3");
    cmd.Parameters.AddWithValue(input.Title);
    cmd.Parameters.AddWithValue(input.Done);
    cmd.Parameters.AddWithValue(id);
    var rows = await cmd.ExecuteNonQueryAsync();
    return rows == 0 ? Results.NotFound() : Results.Ok(new TaskItem(id, input.Title, input.Done));
});

app.MapDelete("/tasks/{id:int}", async (int id, NpgsqlDataSource db) =>
{
    await using var cmd = db.CreateCommand("DELETE FROM tasks WHERE id = $1");
    cmd.Parameters.AddWithValue(id);
    var rows = await cmd.ExecuteNonQueryAsync();
    return rows == 0 ? Results.NotFound() : Results.NoContent();
});

app.Run();

record TaskItem(int Id, string Title, bool Done);
record TaskInput(string Title, bool Done);
