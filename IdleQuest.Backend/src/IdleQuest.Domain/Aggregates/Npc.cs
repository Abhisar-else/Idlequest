namespace IdleQuest.Domain.Aggregates;

public class DialogueLine
{
    public string Text { get; set; } = "";
    public List<string> Responses { get; set; } = new();
}

public class ShopListing
{
    public Guid ItemId { get; set; }
    public int Price { get; set; }
    public int Stock { get; set; }
}

public class Npc
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public int ZoneId { get; set; }
    public bool IsMerchant { get; set; }
    public List<DialogueLine> Dialogue { get; set; } = new();
    public List<ShopListing> Shop { get; set; } = new();
}
