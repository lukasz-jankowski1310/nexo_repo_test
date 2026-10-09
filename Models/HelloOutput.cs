using System.ComponentModel.DataAnnotations;

namespace Template.Module.Models;

public sealed class HelloOutput
{
    [Required]
    public string Message { get; init; } = "";
}
