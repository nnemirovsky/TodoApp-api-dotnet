using System.ComponentModel.DataAnnotations.Schema;

namespace TodoApp.Models;

public class Item
{
    public long Id { get; init; }
    public string Name { get; set; } = default!;
    public bool IsComplete { get; set; }
    public long ListId { get; init; }

    [ForeignKey("ListId")] 
    public ItemList List { get; init; } = default!;
}

public record ItemDto
{
    public string Name { get; init; } = default!;
}

public record ItemDetailDto
{
    public long Id { get; init; }
    public string Name { get; init; } = default!;
    public bool IsComplete { get; init; }
}

public record CreateItemDto
{
    public string Name { get; init; } = default!;
    public long ListId { get; init; }
}

public record UpdateItemDto
{
    public string? Name { get; init; }
    public bool IsComplete { get; init; }
}