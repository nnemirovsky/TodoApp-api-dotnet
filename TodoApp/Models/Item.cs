using System.ComponentModel.DataAnnotations.Schema;

namespace TodoApp.Models;

public class Item
{
    public Item(string name, ItemList list)
    {
        Name = name;
        List = list;
    }

    public Item()
    { }

    public long Id { get; init; }
    public string Name { get; set; }

    public bool IsComplete { get; set; }

    [ForeignKey("ListId")] public ItemList List { get; init; }
}

public record ItemDto
{
    public string Name { get; init; }
}

public record ItemDetailDto
{
    public long Id { get; init; }
    public string Name { get; init; }
    public bool IsComplete { get; init; }
}

public record CreateItemDto
{
    public string Name { get; init; }
    public long ListId { get; init; }
}

public record UpdateItemDto
{
    public string? Name { get; init; }
    public bool IsComplete { get; init; }
}
