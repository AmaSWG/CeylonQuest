using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ProviderCatalogService.DTOs;

public class CreateProviderApplicationRequest
{
    [Required]
    public string BusinessName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    public string ServiceType { get; set; } = string.Empty;

    [Required]
    public string Location { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public List<IFormFile> LegalDocuments { get; set; } = new();

    public IFormFile? LegalDocument { get; set; }
}