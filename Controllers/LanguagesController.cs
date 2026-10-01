using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PSAcademyBack.Data;
using PSAcademyBack.Dtos.Languages;

namespace PSAcademyBack.Controllers;

[ApiController]
[Route("api/languages")]
public class LanguagesController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public LanguagesController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Catálogo de lenguajes disponibles. El cliente lo necesita para ofrecer un
    /// selector de lenguaje: sin este endpoint solo se podrían conocer los
    /// lenguajes que ya tienen alguna plantilla creada.
    /// </summary>
    /// <param name="includeInactive">
    /// Reservado a laadministration. Por defecto se omiten los inactivos para que el
    /// alumno no pueda elegir un lenguaje que el ejecutor no habilitará.
    /// </param>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<LanguageResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<LanguageResponse>>> GetLanguages(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Languages.AsNoTracking().AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(l => l.IsActive);
        }

        var languages = await query
            .OrderBy(l => l.Name)
            .Select(l => new LanguageResponse
            {
                Id = l.Id,
                Name = l.Name,
                Slug = l.Slug,
                IsActive = l.IsActive
            })
            .ToListAsync(cancellationToken);

        return Ok(languages);
    }
}
