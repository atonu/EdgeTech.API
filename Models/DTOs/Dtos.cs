namespace EdgeTech.API.Models.DTOs;

// Auth DTOs
public record RegisterRequest(string Email, string Password, string FirstName, string LastName);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, string RefreshToken, DateTime Expires, UserDto User);
public record UserDto(string Id, string Email, string FirstName, string LastName, string Role);
public record CreateUserRequest(string Email, string Password, string FirstName, string LastName, string Role);
public record ChangeRoleRequest(string Role);

// Product DTOs
public record ProductDto(
    int Id, string Name, string Slug, string? Description, string? ShortDescription,
    decimal Price, decimal? DiscountPrice, string? SKU, int Stock,
    bool IsFeatured, bool IsActive,
    int CategoryId, string CategoryName, string CategorySlug,
    int BrandId, string BrandName, string BrandSlug,
    List<ProductImageDto> Images,
    List<ProductSpecDto> Specifications,
    double AverageRating, int ReviewCount,
    DateTime CreatedAt
);
public record ProductImageDto(int Id, string ImageUrl, bool IsPrimary, int DisplayOrder);
public record ProductSpecDto(int Id, string Key, string Value, int DisplayOrder);
public record ProductListDto(
    int Id, string Name, string Slug, decimal Price, decimal? DiscountPrice,
    string? PrimaryImageUrl, int Stock, bool IsFeatured,
    string CategoryName, string BrandName
);
public record CreateProductRequest(
    string Name, string? Description, string? ShortDescription,
    decimal Price, decimal? DiscountPrice, string? SKU, int Stock,
    int CategoryId, int BrandId, bool IsFeatured,
    List<CreateSpecRequest>? Specifications
);
public record UpdateProductRequest(
    string Name, string? Description, string? ShortDescription,
    decimal Price, decimal? DiscountPrice, string? SKU, int Stock,
    int CategoryId, int BrandId, bool IsFeatured, bool IsActive,
    List<CreateSpecRequest>? Specifications
);
public record CreateSpecRequest(string Key, string Value, int DisplayOrder);
public record ToggleFeaturedRequest(bool IsFeatured);

// Category DTOs
public record CategoryDto(int Id, string Name, string Slug, string? Description, string? ImageUrl, int DisplayOrder, bool IsActive, int? ParentCategoryId, List<CategoryDto>? SubCategories);
public record CreateCategoryRequest(string Name, string? Description, string? ImageUrl, int DisplayOrder, int? ParentCategoryId);
public record UpdateCategoryRequest(string Name, string? Description, string? ImageUrl, int DisplayOrder, bool IsActive, int? ParentCategoryId);

// Brand DTOs
public record BrandDto(int Id, string Name, string Slug, string? LogoUrl, string? Description, bool IsActive);
public record CreateBrandRequest(string Name, string? Description, string? LogoUrl);
public record UpdateBrandRequest(string Name, string? Description, string? LogoUrl, bool IsActive);

// Cart DTOs
public record CartItemDto(int Id, int ProductId, string ProductName, string? ImageUrl, decimal Price, decimal? DiscountPrice, int Quantity, int Stock);
public record CartDto(List<CartItemDto> Items, decimal Total);
public record AddToCartRequest(int ProductId, int Quantity);
public record UpdateCartItemRequest(int Quantity);

// Order DTOs
public record ShippingAddressDto(string FullName, string Phone, string Address, string City, string State, string PostalCode, string Country);
public record CustomerInfoDto(string FullName, string Email, string Phone);
public record PlaceOrderItemRequest(int ProductId, int Quantity);
public record PlaceOrderRequest(ShippingAddressDto ShippingAddress, string? Notes, string PaymentMethod, CustomerInfoDto Customer, List<PlaceOrderItemRequest> Items);
public record OrderDto(int Id, OrderStatus Status, decimal TotalAmount, CustomerInfoDto Customer, ShippingAddressDto ShippingAddress, string? Notes, string? PaymentMethod, DateTime CreatedAt, List<OrderItemDto> Items);
public record OrderItemDto(int Id, int ProductId, string ProductName, string? ImageUrl, decimal UnitPrice, int Quantity);
public record UpdateOrderStatusRequest(OrderStatus Status);
public record UpdateOrderAdminRequest(OrderStatus Status, string? Notes);

// Review DTOs
public record CreateReviewRequest(int Rating, string? Comment);
public record ReviewDto(int Id, string UserId, string UserName, int Rating, string? Comment, DateTime CreatedAt);

// Package Builder DTOs
public record PackageSlotDefinition(string SlotKey, string Label, string Description, string CategorySlug, string Icon);
public record PackageBuildDto(int Id, string Name, decimal TotalPrice, DateTime CreatedAt, List<PackageComponentDto> Components);
public record PackageComponentDto(string SlotKey, int ProductId, string ProductName, string? ImageUrl, decimal Price, int Quantity);
public record SavePackageRequest(string Name, List<SaveComponentRequest> Components);
public record SaveComponentRequest(string SlotKey, int ProductId, int Quantity);

// Pagination
public record PagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize, int TotalPages);

// Services
public record ServiceItemDto(int Id, string Name, string? Description, bool IsActive);
public record CreateServiceItemRequest(string Name, string? Description);
public record UpdateServiceItemRequest(string Name, string? Description, bool IsActive);

// Product groups
public record ProductGroupDto(int Id, string Key, string Name, bool IsActive, List<int> ProductIds, DateTime UpdatedAt);
public record ProductGroupWithProductsDto(int Id, string Key, string Name, bool IsActive, DateTime UpdatedAt, List<ProductListDto> Products);
public record CreateProductGroupRequest(string Key, string Name, bool IsActive, List<int> ProductIds);
public record UpdateProductGroupRequest(string Name, bool IsActive, List<int> ProductIds);

// Upload
public record UploadResponse(string Url);
