using System.ComponentModel.DataAnnotations;

namespace Template.Module.Models;

public sealed class HelloInput
{
    [Required]
    public string Name { get; init; } = "";
}
