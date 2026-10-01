using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PSAcademyBack.Data;
using PSAcademyBack.Dtos.Categories;
using PSAcademyBack.Extensions;

namespace PSAcademyBack.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public CategoriesController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<CategoryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CategoryResponse>>> GetCategories(CancellationToken cancellationToken)
    {
        var categories = await _dbContext.Categories
            .AsNoTracking()
            .OrderBy(c => c.OrderIndex)
            .ThenBy(c => c.Name)
            .Select(c => new CategoryResponse
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                OrderIndex = c.OrderIndex,
                // Cuenta solo ejercicios activos: los inactivos no deben aparecer en el catálogo.
                ExerciseCount = c.Exercises.Count(e => e.IsActive)
            })
            .ToListAsync(cancellationToken);

        return Ok(categories);
    }

    [HttpGet("{id:int}/exercises")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<ExerciseSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ExerciseSummaryResponse>>> GetExercisesByCategory(
        int id,
        CancellationToken cancellationToken)
    {
        var categoryExists = await _dbContext.Categories
            .AsNoTracking()
            .AnyAsync(c => c.Id == id, cancellationToken);

        if (!categoryExists)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Categoría no encontrada",
                Detail = $"No existe una categoría con id {id}.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var exercises = await _dbContext.Exercises
            .AsNoTracking()
            .Where(e => e.CategoryId == id && e.IsActive)
            .OrderBy(e => e.Id)
            .Select(e => new ExerciseSummaryResponse
            {
                Id = e.Id,
                Title = e.Title,
                Difficulty = e.Difficulty
            })
            .ToListAsync(cancellationToken);

        // El endpoint es anónimo, pero si llega un JWT válido se marca el progreso.
        var userId = User.GetUserId();
        if (userId.HasValue)
        {
            var progress = await _dbContext.UserProgress
                .AsNoTracking()
                .Where(p => p.UserId == userId.Value && p.Exercise.CategoryId == id && p.Exercise.IsActive)
                .Select(p => new
                {
                    p.ExerciseId,
                    p.Status,
                    CompletedLanguageSlug = p.LastSubmittedLanguage != null
                        ? p.LastSubmittedLanguage.Slug
                        : null
                })
                .ToListAsync(cancellationToken);

            foreach (var entry in progress)
            {
                var status = entry.Status.ToString().ToLowerInvariant();
                var exercise = exercises.FirstOrDefault(e => e.Id == entry.ExerciseId);
                if (exercise is null) continue;

                exercise.UserStatus = status;

                if (status == "completed" && entry.CompletedLanguageSlug is not null)
                {
                    exercise.CompletedInLanguageSlug = entry.CompletedLanguageSlug;
                }
            }
        }

        return Ok(exercises);
    }
}
