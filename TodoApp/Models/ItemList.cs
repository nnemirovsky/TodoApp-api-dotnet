using System.ComponentModel.DataAnnotations.Schema;

namespace TodoApp.Models;

public class ItemList
{
    public ItemList(string name, User author)
    {
        Name = name;
        Author = author;
    }

    public ItemList()
    { }

    public long Id { get; init; }

    public string Name { get; init; }

    [ForeignKey("UserId")] public User Author { get; init; }
}

public record CreateListWithItemsDto
{
    public string Name { get; init; }
    public IEnumerable<ItemDto>? Items { get; init; }
}

public record ItemListDetailDto
{
    public long Id { get; init; }

    public string Name { get; init; }

    public IEnumerable<ItemDto>? Items { get; init; }
}
