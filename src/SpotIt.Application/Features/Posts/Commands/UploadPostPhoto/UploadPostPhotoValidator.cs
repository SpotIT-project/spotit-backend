using FluentValidation;

namespace SpotIt.Application.Features.Posts.Commands.UploadPostPhoto;

public class UploadPostPhotoValidator : AbstractValidator<UploadPostPhotoCommand>
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    public UploadPostPhotoValidator()
    {
        RuleFor(x => x.Photo)
            .NotNull().WithMessage("Photo is required.");

        RuleFor(x => x.Photo.Length)
            .LessThanOrEqualTo(MaxFileSizeBytes)
            .WithMessage($"File size must not exceed {MaxFileSizeBytes / 1024 / 1024} MB.")
            .When(x => x.Photo != null);

        RuleFor(x => Path.GetExtension(x.Photo.FileName).ToLower())
            .Must(ext => AllowedExtensions.Contains(ext))
            .WithMessage($"Allowed extensions: {string.Join(", ", AllowedExtensions)}.")
            .When(x => x.Photo != null);
    }
}
