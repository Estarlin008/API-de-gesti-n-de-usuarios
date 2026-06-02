using System.ComponentModel.DataAnnotations;
using UserManagementAPI.Models;
using UserManagementAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSingleton<UserRepository>();

var key = Encoding.ASCII.GetBytes("ClaveSuperSecreta123!"); // 🔑 Usa una clave segura en producción
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

var app = builder.Build();

//Middleware de gestión de errores global
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync($"{{\"error\":\"Internal server error: {ex.Message}\"}}");
    }
});

//Middleware de autenticación personalizado
app.Use(async (context, next) =>
{
    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
    if (authHeader is null || !authHeader.StartsWith("Bearer "))
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("{\"error\":\"Unauthorized - Token missing.\"}");
        return;
    }

    var token = authHeader.Substring("Bearer ".Length).Trim();
    var tokenHandler = new JwtSecurityTokenHandler();

    try
    {
        tokenHandler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        }, out _);

        await next(); // Token válido → continuar
    }
    catch
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("{\"error\":\"Unauthorized - Invalid token.\"}");
    }
});

//Middleware de registro
app.Use(async (context, next) =>
{
    await next();

    var method = context.Request.Method;
    var path = context.Request.Path;
    var statusCode = context.Response.StatusCode;

    Console.WriteLine($"[LOG] {method} {path} => {statusCode}");
});


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
