namespace EdgeTech.API.Models;

public class ProductSpecification
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    public Product Product { get; set; } = null!;
}
