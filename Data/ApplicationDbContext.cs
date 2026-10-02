using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PSAcademyBack.Entities;
using PSAcademyBack.Enums;

namespace PSAcademyBack.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Language> Languages => Set<Language>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<ExerciseTemplate> ExerciseTemplates => Set<ExerciseTemplate>();
    public DbSet<ExerciseInput> ExerciseInputs => Set<ExerciseInput>();
    public DbSet<UserProgress> UserProgress => Set<UserProgress>();
    public DbSet<UserCodeDraft> UserCodeDrafts => Set<UserCodeDraft>();
    public DbSet<TutorialStep> TutorialSteps => Set<TutorialStep>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(320).IsRequired();
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash").HasMaxLength(512).IsRequired();
            entity.Property(e => e.Role).HasColumnName("role").HasConversion(LowerCase<UserRole>()).HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");

            entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("ix_users_email");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(1000);
            entity.Property(e => e.OrderIndex).HasColumnName("order_index");

            entity.HasIndex(e => e.Name).IsUnique().HasDatabaseName("ix_categories_name");
            entity.HasIndex(e => e.OrderIndex).HasDatabaseName("ix_categories_order_index");
        });

        modelBuilder.Entity<Language>(entity =>
        {
            entity.ToTable("languages");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(80).IsRequired();
            entity.Property(e => e.Slug).HasColumnName("slug").HasMaxLength(40).IsRequired();
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);

            entity.HasIndex(e => e.Slug).IsUnique().HasDatabaseName("ix_languages_slug");
        });

        modelBuilder.Entity<Exercise>(entity =>
        {
            entity.ToTable("exercises");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").IsRequired();
            entity.Property(e => e.Difficulty).HasColumnName("difficulty").HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.ExpectedOutput).HasColumnName("expected_output").IsRequired();
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");

            entity.HasIndex(e => e.CategoryId).HasDatabaseName("ix_exercises_category_id");

            entity.HasOne(e => e.Category)
                  .WithMany(c => c.Exercises)
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ExerciseTemplate>(entity =>
        {
            entity.ToTable("exercise_templates");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ExerciseId).HasColumnName("exercise_id");
            entity.Property(e => e.LanguageId).HasColumnName("language_id");
            entity.Property(e => e.StarterCode).HasColumnName("starter_code").IsRequired();

            entity.HasIndex(e => new { e.ExerciseId, e.LanguageId })
                  .IsUnique()
                  .HasDatabaseName("ix_exercise_templates_exercise_id_language_id");

            entity.HasOne(e => e.Exercise)
                  .WithMany(x => x.Templates)
                  .HasForeignKey(e => e.ExerciseId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Language)
                  .WithMany(l => l.Templates)
                  .HasForeignKey(e => e.LanguageId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserProgress>(entity =>
        {
            entity.ToTable("user_progress");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ExerciseId).HasColumnName("exercise_id");
            entity.Property(e => e.Status).HasColumnName("status").HasConversion(LowerCase<ProgressStatus>()).HasMaxLength(20);
            entity.Property(e => e.LastSubmittedCode).HasColumnName("last_submitted_code");
            entity.Property(e => e.LastSubmittedLanguageId).HasColumnName("last_submitted_language_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone");

            entity.HasIndex(e => new { e.UserId, e.ExerciseId })
                  .IsUnique()
                  .HasDatabaseName("ix_user_progress_user_id_exercise_id");

            entity.HasOne(e => e.User)
                  .WithMany(u => u.Progress)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Exercise)
                  .WithMany(x => x.Progress)
                  .HasForeignKey(e => e.ExerciseId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Restricción: si el lenguaje se elimina, se conserva el progreso sin lenguaje.
            entity.HasOne(e => e.LastSubmittedLanguage)
                  .WithMany(l => l.Progress)
                  .HasForeignKey(e => e.LastSubmittedLanguageId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ExerciseInput>(entity =>
        {
            entity.ToTable("exercise_inputs");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ExerciseId).HasColumnName("exercise_id");
            entity.Property(e => e.OrderIndex).HasColumnName("order_index");
            entity.Property(e => e.Value).HasColumnName("value").IsRequired();
            entity.Property(e => e.ValueType).HasColumnName("value_type").HasConversion<string>().HasMaxLength(20);

            entity.HasIndex(e => new { e.ExerciseId, e.OrderIndex })
                  .HasDatabaseName("ix_exercise_inputs_exercise_id_order_index");

            entity.HasOne(e => e.Exercise)
                  .WithMany(x => x.Inputs)
                  .HasForeignKey(e => e.ExerciseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserCodeDraft>(entity =>
        {
            entity.ToTable("user_code_drafts");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ExerciseId).HasColumnName("exercise_id");
            entity.Property(e => e.LanguageId).HasColumnName("language_id");
            entity.Property(e => e.Code).HasColumnName("code").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone");

            // Un único borrador por alumno, ejercicio y lenguaje.
            entity.HasIndex(e => new { e.UserId, e.ExerciseId, e.LanguageId })
                  .IsUnique()
                  .HasDatabaseName("ix_user_code_drafts_user_id_exercise_id_language_id");

            entity.HasOne(e => e.User)
                  .WithMany(u => u.Drafts)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Exercise)
                  .WithMany(x => x.Drafts)
                  .HasForeignKey(e => e.ExerciseId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Language)
                  .WithMany(l => l.Drafts)
                  .HasForeignKey(e => e.LanguageId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    modelBuilder.Entity<TutorialStep>(entity =>
        {
            entity.ToTable("tutorial_steps");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ExerciseId).HasColumnName("exercise_id");
            entity.Property(e => e.OrderIndex).HasColumnName("order_index");
            entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
            entity.Property(e => e.Body).HasColumnName("body").IsRequired();
            entity.Property(e => e.Task).HasColumnName("task").HasMaxLength(1000);
            entity.Property(e => e.CodeSnippet).HasColumnName("code_snippet").IsRequired();
            entity.Property(e => e.ExpectedOutput).HasColumnName("expected_output").IsRequired();
            entity.Property(e => e.Tip).HasColumnName("tip").HasMaxLength(1000);

            entity.HasIndex(e => new { e.ExerciseId, e.OrderIndex })
                  .IsUnique()
                  .HasDatabaseName("ix_tutorial_steps_exercise_id_order_index");

            entity.HasOne(e => e.Exercise)
                  .WithMany(x => x.TutorialSteps)
                  .HasForeignKey(e => e.ExerciseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

    }

    /// <summary>
    /// Persiste el enum como texto en minúsculas ("admin", "completed") para que los
    /// valores en base de datos coincidan con el contrato descrito.
    /// </summary>
    private static ValueConverter<TEnum, string> LowerCase<TEnum>() where TEnum : struct, Enum
        => new(
            v => v.ToString().ToLowerInvariant(),
            v => Enum.Parse<TEnum>(v, true));
}
