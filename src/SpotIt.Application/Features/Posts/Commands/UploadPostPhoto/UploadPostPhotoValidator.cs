using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace SpotIt.Application.Features.Posts.Commands.UploadPostPhoto;

public class UploadPostPhotoValidator : AbstractValidator<UploadPostPhotoCommand>
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly byte[] JpegMagic = [0xFF, 0xD8];
    private static readonly byte[] PngMagic  = [0x89, 0x50, 0x4E, 0x47];
    private static readonly byte[] RiffMagic = [0x52, 0x49, 0x46, 0x46]; // WebP: RIFF....WEBP
    private static readonly byte[] WebpMagic = [0x57, 0x45, 0x42, 0x50]; // bytes 8-11

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

        RuleFor(x => x.Photo)
            .Must(IsValidImageSignature)
            .WithMessage("File content does not match a valid image format.")
            .When(x => x.Photo != null);
    }

    private static bool IsValidImageSignature(IFormFile file)
    {
        if (file.Length < 12) return false;

        var buffer = new byte[12];
        using var stream = file.OpenReadStream();
        stream.ReadExactly(buffer, 0, 12);

        if (buffer.Take(2).SequenceEqual(JpegMagic)) return true;
        if (buffer.Take(4).SequenceEqual(PngMagic)) return true;
        if (buffer.Take(4).SequenceEqual(RiffMagic) && buffer.Skip(8).Take(4).SequenceEqual(WebpMagic)) return true;

        return false;
    }
}
