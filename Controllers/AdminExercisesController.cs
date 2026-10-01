using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PSAcademyBack.Data;
using PSAcademyBack.Dtos.Admin;
using PSAcademyBack.Dtos.Exercises;
using PSAcademyBack.Entities;
using PSAcademyBack.Enums;

namespace PSAcademyBack.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class AdminExercisesController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<AdminExercisesController> _logger;

    public AdminExercisesController(ApplicationDbContext dbContext, ILogger<AdminExercisesController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    // ------------------------------------------------------------- Categorías

    [HttpGet("categories")]
    [ProducesResponseType(typeof(IEnumerable<Entities.Category>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Entities.Category>>> GetCategories(CancellationToken cancellationToken)
    {
        var categories = await _dbContext.Categories
            .AsNoTracking()
            .OrderBy(c => c.OrderIndex)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return Ok(categories);
    }

    [HttpPost("categories")]
    [ProducesResponseType(typeof(Entities.Category), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Entities.Category>> CreateCategory(
        [FromBody] UpsertCategoryDto request,
        CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();

        if (await _dbContext.Categories.AnyAsync(c => c.Name.ToLower() == name.ToLower(), cancellationToken))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Categoría duplicada",
                Detail = $"Ya existe una categoría llamada '{name}'.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var category = new Entities.Category
        {
            Name = name,
            Description = request.Description?.Trim(),
            OrderIndex = request.OrderIndex
        };

        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Categoría {CategoryId} creada", category.Id);

        return Created($"/api/admin/categories/{category.Id}", category);
    }

    [HttpPut("categories/{id:int}")]
    [ProducesResponseType(typeof(Entities.Category), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Entities.Category>> UpdateCategory(
        int id,
        [FromBody] UpsertCategoryDto request,
        CancellationToken cancellationToken)
    {
        var category = await _dbContext.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (category is null)
        {
            return NotFound(NotFoundDetails("Categoría", id));
        }

        var name = request.Name.Trim();

        if (await _dbContext.Categories
                .AnyAsync(c => c.Id != id && c.Name.ToLower() == name.ToLower(), cancellationToken))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Categoría duplicada",
                Detail = $"Ya existe otra categoría llamada '{name}'.",
                Status = StatusCodes.Status409Conflict
            });
        }

        category.Name = name;
        category.Description = request.Description?.Trim();
        category.OrderIndex = request.OrderIndex;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(category);
    }

    [HttpDelete("categories/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(int id, CancellationToken cancellationToken)
    {
        var category = await _dbContext.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (category is null)
        {
            return NotFound(NotFoundDetails("Categoría", id));
        }

        // La FK está en Restrict: borrarla con ejercicios colgaría deja la API en error 500.
        if (await _dbContext.Exercises.AnyAsync(e => e.CategoryId == id, cancellationToken))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Categoría en uso",
                Detail = "No se puede eliminar: la categoría tiene ejercicios asociados. Desactívelos o reasígnalos primero.",
                Status = StatusCodes.Status409Conflict
            });
        }

        _dbContext.Categories.Remove(category);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Categoría {CategoryId} eliminada", id);

        return NoContent();
    }

    // -------------------------------------------------------------- Ejercicios

    [HttpGet("exercises")]
    [ProducesResponseType(typeof(IEnumerable<AdminExerciseResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AdminExerciseResponse>>> GetExercises(
        [FromQuery] int? categoryId,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Exercises
            .AsNoTracking()
            .Include(e => e.Templates).ThenInclude(t => t.Language)
            .AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(e => e.CategoryId == categoryId.Value);
        }

        var exercises = await query
            .OrderBy(e => e.Id)
            .Select(e => new AdminExerciseResponse
            {
                Id = e.Id,
                CategoryId = e.CategoryId,
                CategoryName = e.Category.Name,
                Title = e.Title,
                Description = e.Description,
                Difficulty = e.Difficulty,
                ExpectedOutput = e.ExpectedOutput,
                IsActive = e.IsActive,
                CreatedAt = e.CreatedAt,
                Templates = e.Templates
                    .OrderBy(t => t.Language.Name)
                    .Select(t => new AdminTemplateResponse
                    {
                        Id = t.Id,
                        LanguageId = t.LanguageId,
                        LanguageName = t.Language.Name,
                        LanguageSlug = t.Language.Slug,
                        StarterCode = t.StarterCode
                    })
                    .ToList(),
                Inputs = e.Inputs
                    .OrderBy(i => i.OrderIndex)
                    .Select(i => new ExerciseInputResponse
                    {
                        OrderIndex = i.OrderIndex,
                        Value = i.Value,
                        ValueType = i.ValueType
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return Ok(exercises);
    }

    [HttpGet("exercises/{id:int}")]
    [ProducesResponseType(typeof(AdminExerciseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminExerciseResponse>> GetExercise(int id, CancellationToken cancellationToken)
    {
        var exercise = await ProjectToAdminResponse()
            .SingleOrDefaultAsync(e => e.Id == id, cancellationToken);

        return exercise is null ? NotFound(NotFoundDetails("Ejercicio", id)) : Ok(exercise);
    }

    [HttpPost("exercises")]
    [ProducesResponseType(typeof(AdminExerciseResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminExerciseResponse>> CreateExercise(
        [FromBody] UpsertExerciseDto request,
        CancellationToken cancellationToken)
    {
        if (!await _dbContext.Categories.AnyAsync(c => c.Id == request.CategoryId, cancellationToken))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Categoría inválida",
                Detail = $"No existe una categoría con id {request.CategoryId}.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var invalidLanguageIds = await FindInvalidLanguageIdsAsync(request.Templates, cancellationToken);
        if (invalidLanguageIds.Count > 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Lenguaje inválido",
                Detail = $"Id de lenguaje inexistente: {string.Join(", ", invalidLanguageIds)}.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var duplicateTemplate = request.Templates
            .GroupBy(t => t.LanguageId)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicateTemplate is not null)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Plantilla duplicada",
                Detail = $"El lenguaje {duplicateTemplate.Key} aparece más de una vez.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var inputError = ValidateInputs(request.Inputs);
        if (inputError is not null)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Valor de entrada inválido",
                Detail = inputError,
                Status = StatusCodes.Status400BadRequest
            });
        }

        var exercise = new Exercise
        {
            CategoryId = request.CategoryId,
            Title = request.Title.Trim(),
            Description = request.Description,
            Difficulty = request.Difficulty,
            ExpectedOutput = request.ExpectedOutput,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        AddTemplates(exercise, request.Templates);
        AddInputs(exercise, request.Inputs);

        _dbContext.Exercises.Add(exercise);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Ejercicio {ExerciseId} creado", exercise.Id);

        var created = await ProjectToAdminResponse()
            .SingleAsync(e => e.Id == exercise.Id, cancellationToken);

        return Created($"/api/admin/exercises/{exercise.Id}", created);
    }

    [HttpPut("exercises/{id:int}")]
    [ProducesResponseType(typeof(AdminExerciseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminExerciseResponse>> UpdateExercise(
        int id,
        [FromBody] UpsertExerciseDto request,
        CancellationToken cancellationToken)
    {
        var exercise = await _dbContext.Exercises
            .Include(e => e.Templates)
            .Include(e => e.Inputs)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (exercise is null)
        {
            return NotFound(NotFoundDetails("Ejercicio", id));
        }

        if (!await _dbContext.Categories.AnyAsync(c => c.Id == request.CategoryId, cancellationToken))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Categoría inválida",
                Detail = $"No existe una categoría con id {request.CategoryId}.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var invalidLanguageIds = await FindInvalidLanguageIdsAsync(request.Templates, cancellationToken);
        if (invalidLanguageIds.Count > 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Lenguaje inválido",
                Detail = $"Id de lenguaje inexistente: {string.Join(", ", invalidLanguageIds)}.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var duplicateTemplate = request.Templates
            .GroupBy(t => t.LanguageId)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicateTemplate is not null)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Plantilla duplicada",
                Detail = $"El lenguaje {duplicateTemplate.Key} aparece más de una vez.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var inputError = ValidateInputs(request.Inputs);
        if (inputError is not null)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Valor de entrada inválido",
                Detail = inputError,
                Status = StatusCodes.Status400BadRequest
            });
        }

        exercise.CategoryId = request.CategoryId;
        exercise.Title = request.Title.Trim();
        exercise.Description = request.Description;
        exercise.Difficulty = request.Difficulty;
        exercise.ExpectedOutput = request.ExpectedOutput;
        exercise.IsActive = request.IsActive;

        // Reemplazo completo: los templates omitidos se eliminan.
        _dbContext.ExerciseTemplates.RemoveRange(exercise.Templates);
        AddTemplates(exercise, request.Templates);

        // Igual para las entradas del "Leer".
        _dbContext.ExerciseInputs.RemoveRange(exercise.Inputs);
        AddInputs(exercise, request.Inputs);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var updated = await ProjectToAdminResponse()
            .SingleAsync(e => e.Id == exercise.Id, cancellationToken);

        return Ok(updated);
    }

    /// <summary>
    /// Baja lógica en lugar de borrado físico: UserProgress tiene FK con Cascade,
    /// y un DELETE real destruiría el historial de los alumnos.
    /// </summary>
    [HttpDelete("exercises/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteExercise(int id, CancellationToken cancellationToken)
    {
        var exercise = await _dbContext.Exercises.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (exercise is null)
        {
            return NotFound(NotFoundDetails("Ejercicio", id));
        }

        exercise.IsActive = false;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Ejercicio {ExerciseId} desactivado", id);

        return NoContent();
    }

    private IQueryable<AdminExerciseResponse> ProjectToAdminResponse()
        => _dbContext.Exercises
            .AsNoTracking()
            .Select(e => new AdminExerciseResponse
            {
                Id = e.Id,
                CategoryId = e.CategoryId,
                CategoryName = e.Category.Name,
                Title = e.Title,
                Description = e.Description,
                Difficulty = e.Difficulty,
                ExpectedOutput = e.ExpectedOutput,
                IsActive = e.IsActive,
                CreatedAt = e.CreatedAt,
                Templates = e.Templates
                    .OrderBy(t => t.Language.Name)
                    .Select(t => new AdminTemplateResponse
                    {
                        Id = t.Id,
                        LanguageId = t.LanguageId,
                        LanguageName = t.Language.Name,
                        LanguageSlug = t.Language.Slug,
                        StarterCode = t.StarterCode
                    })
                    .ToList(),
                Inputs = e.Inputs
                    .OrderBy(i => i.OrderIndex)
                    .Select(i => new ExerciseInputResponse
                    {
                        OrderIndex = i.OrderIndex,
                        Value = i.Value,
                        ValueType = i.ValueType
                    })
                    .ToList()
            });

    private static void AddTemplates(Exercise exercise, List<UpsertTemplateDto> templates)
    {
        foreach (var template in templates)
        {
            exercise.Templates.Add(new ExerciseTemplate
            {
                LanguageId = template.LanguageId,
                StarterCode = template.StarterCode
            });
        }
    }

    private static void AddInputs(Exercise exercise, List<UpsertInputDto> inputs)
    {
        for (var i = 0; i < inputs.Count; i++)
        {
            exercise.Inputs.Add(new ExerciseInput
            {
                OrderIndex = i,
                Value = inputs[i].Value,
                ValueType = inputs[i].ValueType
            });
        }
    }

    private static string? ValidateInputs(List<UpsertInputDto> inputs)
    {
        for (var i = 0; i < inputs.Count; i++)
        {
            var input = inputs[i];

            if (input.ValueType == InputValueType.Number &&
                !double.TryParse(input.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            {
                return $"El valor #{i + 1} está marcado como número, pero '{input.Value}' no es un número válido.";
            }
        }

        return null;
    }

    private async Task<List<int>> FindInvalidLanguageIdsAsync(
        List<UpsertTemplateDto> templates,
        CancellationToken cancellationToken)
    {
        var ids = templates.Select(t => t.LanguageId).Distinct().ToList();

        if (ids.Count == 0)
        {
            return [];
        }

        var existingIds = await _dbContext.Languages
            .Where(l => ids.Contains(l.Id))
            .Select(l => l.Id)
            .ToListAsync(cancellationToken);

        return ids.Except(existingIds).ToList();
    }

    private static ProblemDetails NotFoundDetails(string resource, int id) => new()
    {
        Title = $"{resource} no encontrado",
        Detail = $"No existe {resource.ToLowerInvariant()} con id {id}.",
        Status = StatusCodes.Status404NotFound
    };
}
