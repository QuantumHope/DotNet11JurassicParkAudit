using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using ParkOps.Data;

public sealed class PaddockOpenAttribute : AsyncValidationAttribute
{
    protected override async Task<ValidationResult?> IsValidAsync(
        object? value, ValidationContext context, CancellationToken cancellationToken)
    {
        var db = context.GetRequiredService<ParkDb>();
        var code = value as string;
        return await db.Paddocks.AnyAsync(p => p.Code == code && p.IsOpen, cancellationToken)
            ? ValidationResult.Success
            : new ValidationResult($"Paddock '{code}' is closed. Tour denied.");
    }

    // RC2 also requires the synchronous override; this check only makes sense asynchronously.
    protected override ValidationResult? IsValid(object? value, ValidationContext context) =>
        throw new NotSupportedException("PaddockOpen validates asynchronously.");
}
