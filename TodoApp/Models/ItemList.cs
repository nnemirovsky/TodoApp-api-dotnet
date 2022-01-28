using System.ComponentModel.DataAnnotations.Schema;

namespace TodoApp.Models;

public class ItemList
{
    public long Id { get; init; }

    public string Name { get; init; } = default!;

    // public bool IsTotalComplete { get; init; }
    
    [ForeignKey("UserId")]
    public User Author { get; init; } = default!;
}

public record CreateListWithItemsDto
{
    public string Name { get; init; } = default!;
    public IEnumerable<ItemDto> Items { get; init; } = default!;
}

public record ItemListDetailDto
{
    public long Id { get; init; }

    public string Name { get; init; } = default!;
    
    public IEnumerable<ItemDto> Items { get; init; } = default!;
}