using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AurionCal.Api.Entities;

public class UserRefreshStatus
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>
    /// Nombre d'échecs consécutifs depuis le dernier succès.
    /// </summary>
    public int ConsecutiveFailureCount { get; set; }

    public DateTime? LastAttemptUtc { get; set; }
    public DateTime? LastSuccessUtc { get; set; }
    public DateTime? LastFailureUtc { get; set; }

    /// <summary>
    /// Petite description (tronquée) de la dernière erreur (HTTP, désérialisation, auth, etc.).
    /// </summary>
    public string? LastFailureReason { get; set; }

    /// <summary>
    /// Si défini, ne pas retenter avant cette date.
    /// </summary>
    public DateTime? NextAttemptUtc { get; set; }

    /// <summary>
    /// Date d'envoi du mail d'alerte. On ne renvoie plus jamais ensuite (pour l'instant).
    /// </summary>
    public DateTime? FailureEmailSentUtc { get; set; }

    public static void Configure(EntityTypeBuilder<UserRefreshStatus> builder)
    {
        builder.ToTable("UserRefreshStatuses");
        builder.HasKey(x => x.UserId);

        builder.Property(x => x.ConsecutiveFailureCount)
            .HasDefaultValue(0);

        builder.Property(x => x.LastFailureReason)
            .HasMaxLength(500);

        builder.HasOne(x => x.User)
            .WithOne(u => u.RefreshStatus)
            .HasForeignKey<UserRefreshStatus>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

