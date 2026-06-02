using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
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
{
    try
    {
        // Optimización: devolver lista materializada para evitar múltiples enumeraciones
        var users = repo.GetAll().ToList();
        return Results.Ok(users);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error al recuperar usuarios: {ex.Message}");
    }
});


app.MapGet("/users/{id:guid}", (Guid id, UserRepository repo) =>
{
    try
    {
        var user = repo.Get(id);
        return user is not null ? Results.Ok(user) : Results.NotFound($"Usuario con ID {id} no encontrado.");
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error al recuperar usuario: {ex.Message}");
    }
});


app.MapPost("/users", (UserCreateDto dto, UserRepository repo) =>
{
   try
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Results.BadRequest("El nombre no puede estar vacío.");

        if (string.IsNullOrWhiteSpace(dto.Email) || !new EmailAddressAttribute().IsValid(dto.Email))
            return Results.BadRequest("El correo electrónico no es válido.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Email = dto.Email.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        repo.Create(user);
        return Results.Created($"/users/{user.Id}", user);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error al crear usuario: {ex.Message}");
    }
});

// PUT: Actualizar un usuario existente
app.MapPut("/users/{id:guid}", (Guid id, UserUpdateDto dto, UserRepository repo) =>
{
   try
    {
        var existing = repo.Get(id);
        if (existing is null)
            return Results.NotFound($"Usuario con ID {id} no encontrado.");

        if (!string.IsNullOrWhiteSpace(dto.Name))
            existing.Name = dto.Name.Trim();

        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            if (!new EmailAddressAttribute().IsValid(dto.Email))
                return Results.BadRequest("El correo electrónico no es válido.");
            existing.Email = dto.Email.Trim();
        }

        repo.Update(id, existing);
        return Results.Ok(existing);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error al actualizar usuario: {ex.Message}");
    }
});

// DELETE: Eliminar un usuario por ID
app.MapDelete("/users/{id:guid}", (Guid id, UserRepository repo) =>
{
    try
    {
        var deleted = repo.Delete(id);
        return deleted ? Results.NoContent() : Results.NotFound($"Usuario con ID {id} no encontrado.");
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error al eliminar usuario: {ex.Message}");
    }
});


app.Run();
