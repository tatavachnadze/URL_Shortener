using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using URLShortener.Application.Dtos;
using URLShortener.Domain.Models;

namespace URLShortener.Core.Validators;

// why? 
public class CreateUrlRequestValidator : AbstractValidator<CreateUrlRequest>
{
    public CreateUrlRequestValidator()
    {
        RuleFor(x => x.OriginalUrl)
            .NotEmpty().WithMessage("Original URL is required")
            .Must(BeValidUrl).WithMessage("Invalid URL format");

        RuleFor(x => x.ExpiresAt)
            .Must(BeInFuture).WithMessage("Expiration date must be in the future")
            .When(x => x.ExpiresAt.HasValue);

        RuleFor(x => x.CustomAlias)
            .Matches("^[a-zA-Z0-9_-]*$").WithMessage("Custom alias can only contain letters, numbers, hyphens, and underscores")
            .Length(3, 20).WithMessage("Custom alias must be between 3 and 20 characters")
            .When(x => !string.IsNullOrEmpty(x.CustomAlias));
    }

    private bool BeValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var result) &&
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }

    private bool BeInFuture(DateTime? date)
    {
        return !date.HasValue || date.Value > DateTime.UtcNow;
    }
}

public class UpdateUrlRequestValidator : AbstractValidator<UpdateUrlRequest>
{
    public UpdateUrlRequestValidator()
    {
        RuleFor(x => x.OriginalUrl)
            .Must(BeValidUrl).WithMessage("Invalid URL format")
            .When(x => !string.IsNullOrEmpty(x.OriginalUrl));

        RuleFor(x => x.ExpiresAt)
            .Must(BeInFuture).WithMessage("Expiration date must be in the future")
            .When(x => x.ExpiresAt.HasValue);
    }

    private bool BeValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var result) &&
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }

    private bool BeInFuture(DateTime? date)
    {
        return !date.HasValue || date.Value > DateTime.UtcNow;
    }
}
