using System.Collections.Concurrent;
using UserManagementAPI.Models;
using UserManagementAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSingleton<UserRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Users endpoints (CRUD)

app.MapGet("/users", (UserRepository repo) =>
    Results.Ok(repo.GetAll()));


app.MapGet("/users/{id:guid}", (Guid id, UserRepository repo) =>
{
    var user = repo.Get(id);
    return user is not null ? Results.Ok(user) : Results.NotFound();
});


app.MapPost("/users", (UserCreateDto dto, UserRepository repo) =>
{
    var user = new User
    {
        Id = Guid.NewGuid(),
        Name = dto.Name,
        Email = dto.Email,
        CreatedAt = DateTime.UtcNow
    };

    repo.Create(user);
    return Results.Created($"/users/{user.Id}", user);
});

// PUT: Actualizar un usuario existente
app.MapPut("/users/{id:guid}", (Guid id, UserUpdateDto dto, UserRepository repo) =>
{
    var existing = repo.Get(id);
    if (existing is null)
        return Results.NotFound();

    // Solo actualiza los campos que no son nulos
    if (!string.IsNullOrWhiteSpace(dto.Name))
        existing.Name = dto.Name;
    if (!string.IsNullOrWhiteSpace(dto.Email))
        existing.Email = dto.Email;

    repo.Update(id, existing);
    return Results.Ok(existing);
});

// DELETE: Eliminar un usuario por ID
app.MapDelete("/users/{id:guid}", (Guid id, UserRepository repo) =>
{
    var deleted = repo.Delete(id);
    return deleted ? Results.NoContent() : Results.NotFound();
});


app.Run();
