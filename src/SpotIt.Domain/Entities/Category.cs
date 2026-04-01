namespace SpotIt.Domain.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public string? AssignedEmployeeId { get; set; }

    // Navigation
    public ApplicationUser? AssignedEmployee { get; set; }
    public ICollection<Post> Posts { get; set; } = [];
}
