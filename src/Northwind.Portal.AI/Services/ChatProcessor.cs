using Microsoft.Extensions.Logging;
using Bipins.AI.LLM;
using Northwind.Portal.Domain.Services;
using Northwind.Portal.Domain.DTOs;
using Microsoft.EntityFrameworkCore;
using Northwind.Portal.Data.Contexts;

namespace Northwind.Portal.AI.Services;

public class ChatProcessor : IChatProcessor
{
    private readonly ILogger<ChatProcessor> _logger;
    private readonly IChatService? _chatService;
    private readonly ICatalogService? _catalogService;
    private readonly ICartService? _cartService;
    private readonly IOrderService? _orderService;
    private readonly ITenantContext? _tenantContext;
    private readonly NorthwindDbContext? _northwindContext;
    private readonly IProductEmbeddingService? _embeddingService;
    private readonly string? _userId;
    private object? _lastAddedProduct;

    public ChatProcessor(
        ILogger<ChatProcessor> logger, 
        IChatService? chatService = null,
        ICatalogService? catalogService = null,
        ICartService? cartService = null,
        IOrderService? orderService = null,
        ITenantContext? tenantContext = null,
        NorthwindDbContext? northwindContext = null,
        IProductEmbeddingService? embeddingService = null,
        string? userId = null)
    {
        _logger = logger;
        _chatService = chatService;
        _catalogService = catalogService;
        _cartService = cartService;
        _orderService = orderService;
        _tenantContext = tenantContext;
        _northwindContext = northwindContext;
        _embeddingService = embeddingService;
        _userId = userId;
    }
    
    private async Task<string?> GetCustomerIdFromUserIdAsync(string userId)
    {
        // Try TenantContext first (if HttpContext is available)
        if (_tenantContext != null)
        {
            try
            {
                var customerId = await _tenantContext.GetCurrentCustomerIdAsync();
                if (!string.IsNullOrEmpty(customerId))
                    return customerId;
            }
            catch
            {
                // Fall through to direct DB query
            }
        }
        
        // Fallback: Query database directly
        if (_northwindContext != null)
        {
            try
            {
                var map = await _northwindContext.PortalUserCustomerMaps
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.UserId == userId);
                return map?.CustomerId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer ID from user ID");
            }
        }
        
        return null;
    }

    public async Task<ChatResponse> ProcessMessageAsync(string message, string? userId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return new ChatResponse
                {
                    Message = "Please provide a message.",
                    IsSuccess = false,
                    ErrorMessage = "Message cannot be empty"
                };
            }

            _logger.LogInformation("Processing chat message from user {UserId}: {Message}", userId, message);
            
            // Use the userId parameter for this request (override instance-level userId if provided)
            var currentUserId = userId ?? _userId;

            // Use AI service if available (Bipins.AI integration)
            if (_chatService != null)
            {
                try
                {
                    // System prompt with function definitions
                    var systemPrompt = GetSystemPrompt();
                    
                    // Use the ChatAsync method from IChatService
                    var aiResponse = await _chatService.ChatAsync(message, systemPrompt);
                    
                    // Check if the response contains function calls and process them
                    var processedResponse = await ProcessFunctionCallsAsync(aiResponse ?? string.Empty, message, currentUserId);
                    
                    return new ChatResponse
                    {
                        Message = processedResponse,
                        IsSuccess = true,
                        Actions = GetActionsFromResponse(processedResponse)
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message with AI service");
                    // Fall through to default response
                }
            }
            
            // If no AI service or processing failed, try to process function calls anyway
            var fallbackResponse = await ProcessFunctionCallsAsync(string.Empty, message, currentUserId);
            if (!string.IsNullOrEmpty(fallbackResponse))
            {
                return new ChatResponse
                {
                    Message = fallbackResponse,
                    IsSuccess = true,
                    Actions = GetActionsFromResponse(fallbackResponse)
                };
            }

            // Default response if AI service is not available or fails
            return new ChatResponse
            {
                Message = $"Thank you for your message: \"{message}\". I'm here to help with your Northwind Portal questions. How can I assist you today?",
                IsSuccess = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return new ChatResponse
            {
                Message = "I'm sorry, I encountered an error processing your message. Please try again.",
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private string GetSystemPrompt()
    {
        return @"You are a helpful AI assistant for the Northwind Portal. You can help customers with:

1. **Product Discovery**:
   - Search for products by name
   - Find similar products
   - Get product details

2. **Shopping Cart Management**:
   - Add products to cart
   - Update quantities in cart
   - Remove items from cart
   - View cart contents
   - Get smart suggestions for complementary products

3. **Order Management**:
   - View order history (e.g., ""show my last 3 orders"")
   - Reorder previous orders (e.g., ""reorder what I bought last month"")
   - Check order status and shipping estimates
   - Get order details

4. **Smart Features**:
   - Suggest complementary products (""you might also need Y with X"")
   - Reorder entire previous orders
   - Bundle recommendations

Available functions:
- search_products(searchTerm): Search for products by name
- add_to_cart(productId, quantity): Add a product to cart
- update_cart_item(productId, quantity): Update quantity of item in cart
- get_order_history(limit): Get customer's recent orders
- reorder_order(orderId): Reorder all items from a previous order
- get_order_status(orderId): Get status and shipping info for an order
- get_cart_contents(): Get current cart items
- suggest_complements(productId): Get product suggestions

Always be helpful, friendly, and provide clear confirmations when actions are completed.";
    }

    private async Task<string> ProcessFunctionCallsAsync(string aiResponse, string userMessage, string? userId = null)
    {
        // Check if the AI response indicates it wants to call a function
        // This is a simplified implementation - Bipins.AI 1.0.4 may have built-in function calling
        
        // Pattern matching for function calls in the response
        // Example: "I'll search for 'chai' products" -> call search_products
        // Example: "I'll add product 1 to your cart" -> call add_to_cart
        
        var lowerResponse = aiResponse.ToLowerInvariant();
        var lowerMessage = userMessage.ToLowerInvariant();
        
        _logger.LogDebug("Processing function calls. Message: {Message}, UserId: {UserId}", userMessage, userId);
        
        // Check if user wants to add something to cart - more flexible matching
        // Match patterns like: "add X", "add X to cart", "add X to shopping cart", "put X in cart", "I want X", "get me X"
        var addToCartPattern = (lowerMessage.Contains("add") && 
            (lowerMessage.Contains("cart") || lowerMessage.Contains("shopping") || 
             lowerMessage.Contains("put") || lowerMessage.Contains("place"))) ||
            (lowerMessage.Contains("want") && lowerMessage.Contains("cart")) ||
            (lowerMessage.Contains("get") && lowerMessage.Contains("cart"));
        
        // Also check if message starts with "add" followed by a product name (even without "cart")
        var startsWithAdd = lowerMessage.TrimStart().StartsWith("add ");
        
        // Check for "I want X" or "get me X" patterns (but not if it's a search)
        var wantOrGetPattern = (lowerMessage.Contains("i want") || lowerMessage.Contains("get me") || 
                               lowerMessage.Contains("i need") || lowerMessage.Contains("give me")) &&
                               !lowerMessage.Contains("search") && !lowerMessage.Contains("find") &&
                               !lowerMessage.Contains("list") && !lowerMessage.Contains("show");
        
        if (addToCartPattern || (startsWithAdd && !lowerMessage.Contains("search") && !lowerMessage.Contains("find")) || wantOrGetPattern)
        {
            _logger.LogInformation("Detected add to cart intent. Extracting product info...");
            
            // Extract product name or ID from the message
            var productInfo = ExtractProductInfo(userMessage);
            _logger.LogDebug("Extracted product info - ProductId: {ProductId}, ProductName: {ProductName}", 
                productInfo.ProductId, productInfo.ProductName);
            
            if (productInfo.ProductId.HasValue)
            {
                // Direct product ID provided
                var quantity = ExtractQuantity(userMessage) ?? 1;
                var result = await AddToCartAsync(productInfo.ProductId.Value, quantity, userId);
                if (result.Success)
                {
                    // Store product info in actions for toast notification
                    _lastAddedProduct = new { productName = result.ProductName, description = result.Description };
                    return $"{aiResponse}\n\n✅ Successfully added {quantity} x {result.ProductName} to your cart!";
                }
                else
                {
                    return $"{aiResponse}\n\n❌ Could not add product to cart: {result.ErrorMessage}";
                }
            }
            else if (!string.IsNullOrEmpty(productInfo.ProductName))
            {
                // Search for product first
                var searchResults = await SearchProductsAsync(productInfo.ProductName);
                if (searchResults.Any())
                {
                    // Use the first matching product
                    var product = searchResults.First();
                    var quantity = ExtractQuantity(userMessage) ?? 1;
                    var result = await AddToCartAsync(product.ProductId, quantity, userId);
                    if (result.Success)
                    {
                        // Store product info in actions for toast notification
                        _lastAddedProduct = new { productName = result.ProductName, description = result.Description };
                        return $"{aiResponse}\n\n✅ Successfully added {quantity} x {result.ProductName} to your cart!";
                    }
                    else
                    {
                        return $"{aiResponse}\n\n❌ Could not add product to cart: {result.ErrorMessage}";
                    }
                }
                else
                {
                    return $"{aiResponse}\n\n❌ Product '{productInfo.ProductName}' not found. Please check the product name and try again.";
                }
            }
            else
            {
                _logger.LogWarning("Could not extract product info from message: {Message}", userMessage);
            }
        }
        
        // Check if user wants to search for products
        if (lowerMessage.Contains("search") || lowerMessage.Contains("find") || lowerMessage.Contains("look for"))
        {
            var searchTerm = ExtractSearchTerm(userMessage);
            if (!string.IsNullOrEmpty(searchTerm))
            {
                var products = await SearchProductsAsync(searchTerm);
                if (products.Any())
                {
                    var productList = string.Join("\n", products.Take(5).Select(p => 
                        $"• {p.ProductName} (ID: {p.ProductId}) - ${p.UnitPrice:F2} - {p.UnitsInStock} in stock"));
                    
                    return $"{aiResponse}\n\n📦 Found {products.Count} product(s):\n{productList}\n\nYou can ask me to add any of these to your cart!";
                }
                else
                {
                    return $"{aiResponse}\n\n❌ No products found matching '{searchTerm}'. Please try a different search term.";
                }
            }
        }
        
        // Check for order history queries
        if (lowerMessage.Contains("order") && (lowerMessage.Contains("history") || lowerMessage.Contains("show") || 
            lowerMessage.Contains("list") || lowerMessage.Contains("my orders") || lowerMessage.Contains("last")))
        {
            var orderHistoryResponse = await HandleOrderHistoryQueryAsync(userMessage, userId);
            if (!string.IsNullOrEmpty(orderHistoryResponse))
            {
                return $"{aiResponse}\n\n{orderHistoryResponse}";
            }
        }
        
        // Check for reorder requests
        if (lowerMessage.Contains("reorder") || (lowerMessage.Contains("order again") && lowerMessage.Contains("last")))
        {
            var reorderResponse = await HandleReorderRequestAsync(userMessage, userId);
            if (!string.IsNullOrEmpty(reorderResponse))
            {
                return $"{aiResponse}\n\n{reorderResponse}";
            }
        }
        
        // Check for cart modifications (change quantity, update)
        if ((lowerMessage.Contains("change") || lowerMessage.Contains("update") || lowerMessage.Contains("modify")) && 
            (lowerMessage.Contains("quantity") || lowerMessage.Contains("cart") || lowerMessage.Contains("item")))
        {
            var cartModResponse = await HandleCartModificationAsync(userMessage, userId);
            if (!string.IsNullOrEmpty(cartModResponse))
            {
                return $"{aiResponse}\n\n{cartModResponse}";
            }
        }
        
        // Check for shipping estimates
        if (lowerMessage.Contains("shipping") || lowerMessage.Contains("arrive") || 
            lowerMessage.Contains("delivery") || lowerMessage.Contains("when will"))
        {
            var shippingResponse = await HandleShippingEstimateQueryAsync(userMessage, userId);
            if (!string.IsNullOrEmpty(shippingResponse))
            {
                return $"{aiResponse}\n\n{shippingResponse}";
            }
        }
        
        // Check for smart suggestions
        if (lowerMessage.Contains("suggest") || lowerMessage.Contains("recommend") || 
            lowerMessage.Contains("might also need") || lowerMessage.Contains("complement"))
        {
            var suggestionResponse = await HandleSmartSuggestionsAsync(userMessage, userId);
            if (!string.IsNullOrEmpty(suggestionResponse))
            {
                return $"{aiResponse}\n\n{suggestionResponse}";
            }
        }
        
        return aiResponse;
    }

    private (int? ProductId, string? ProductName) ExtractProductInfo(string message)
    {
        // Try to extract product ID (e.g., "product 1", "product ID 5", "id 1")
        var idMatch = System.Text.RegularExpressions.Regex.Match(message, @"(?:product\s*)?(?:id\s*)?(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (idMatch.Success && int.TryParse(idMatch.Groups[1].Value, out var productId))
        {
            _logger.LogDebug("Extracted product ID: {ProductId}", productId);
            return (productId, null);
        }
        
        // Try to extract product name - multiple patterns
        // Pattern 1: "add [product name] to cart"
        var addMatch1 = System.Text.RegularExpressions.Regex.Match(message, @"add\s+(.+?)(?:\s+to\s+(?:the\s+)?cart|\s+cart|$)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (addMatch1.Success)
        {
            var productName = addMatch1.Groups[1].Value.Trim();
            // Remove quantity if present (e.g., "2 chai" -> "chai")
            productName = System.Text.RegularExpressions.Regex.Replace(productName, @"^\d+\s+(?:x\s+|of\s+)?", "");
            // Remove common words
            productName = System.Text.RegularExpressions.Regex.Replace(productName, @"\b(?:the|a|an|product|item)\b\s*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            productName = productName.Trim();
            
            if (!string.IsNullOrWhiteSpace(productName))
            {
                _logger.LogDebug("Extracted product name: {ProductName}", productName);
                return (null, productName);
            }
        }
        
        // Pattern 2: "add [quantity] [product name]" or "add [product name] [quantity]"
        var addMatch2 = System.Text.RegularExpressions.Regex.Match(message, @"add\s+(?:(\d+)\s+)?(.+?)(?:\s+to\s+cart|$)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (addMatch2.Success)
        {
            var productName = addMatch2.Groups[2].Value.Trim();
            // Remove common words
            productName = System.Text.RegularExpressions.Regex.Replace(productName, @"\b(?:the|a|an|product|item|to|cart)\b\s*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            productName = productName.Trim();
            
            if (!string.IsNullOrWhiteSpace(productName))
            {
                _logger.LogDebug("Extracted product name (pattern 2): {ProductName}", productName);
                return (null, productName);
            }
        }
        
        // Pattern 3: Just look for words after "add" that aren't common words
        var words = message.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var addIndex = Array.FindIndex(words, w => w.Equals("add", StringComparison.OrdinalIgnoreCase));
        if (addIndex >= 0 && addIndex < words.Length - 1)
        {
            var potentialProductName = string.Join(" ", words.Skip(addIndex + 1)
                .TakeWhile(w => !w.Equals("to", StringComparison.OrdinalIgnoreCase) && 
                               !w.Equals("cart", StringComparison.OrdinalIgnoreCase) &&
                               !w.Equals("the", StringComparison.OrdinalIgnoreCase) &&
                               !w.Equals("in", StringComparison.OrdinalIgnoreCase) &&
                               !int.TryParse(w, out _)));
            
            if (!string.IsNullOrWhiteSpace(potentialProductName))
            {
                // Remove quantity if it's a number at the start
                potentialProductName = System.Text.RegularExpressions.Regex.Replace(potentialProductName, @"^\d+\s+", "");
                potentialProductName = potentialProductName.Trim();
                
                if (!string.IsNullOrWhiteSpace(potentialProductName))
                {
                    _logger.LogDebug("Extracted product name (pattern 3): {ProductName}", potentialProductName);
                    return (null, potentialProductName);
                }
            }
        }
        
        // Pattern 4: "I want X" or "get me X" or "I need X"
        var wantMatch = System.Text.RegularExpressions.Regex.Match(message, @"(?:i\s+)?(?:want|need|get|give)\s+(?:me\s+)?(.+?)(?:\s+to\s+cart|\s+cart|$)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (wantMatch.Success)
        {
            var productName = wantMatch.Groups[1].Value.Trim();
            // Remove common words and quantity
            productName = System.Text.RegularExpressions.Regex.Replace(productName, @"^\d+\s+(?:x\s+|of\s+)?", "");
            productName = System.Text.RegularExpressions.Regex.Replace(productName, @"\b(?:the|a|an|product|item|please)\b\s*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            productName = productName.Trim();
            
            if (!string.IsNullOrWhiteSpace(productName) && productName.Length > 1)
            {
                _logger.LogDebug("Extracted product name (pattern 4): {ProductName}", productName);
                return (null, productName);
            }
        }
        
        _logger.LogWarning("Could not extract product info from message: {Message}", message);
        return (null, null);
    }

    private int? ExtractQuantity(string message)
    {
        // Pattern 1: "2 x product", "3 × item"
        var quantityMatch = System.Text.RegularExpressions.Regex.Match(message, @"(\d+)\s*(?:x|×)\s*(?:product|item|unit|of)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (quantityMatch.Success && int.TryParse(quantityMatch.Groups[1].Value, out var quantity))
        {
            return quantity;
        }
        
        // Pattern 2: "2 of product" or "3 of chai"
        var ofMatch = System.Text.RegularExpressions.Regex.Match(message, @"(\d+)\s+of", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (ofMatch.Success && int.TryParse(ofMatch.Groups[1].Value, out var qty))
        {
            return qty;
        }
        
        // Pattern 3: "add 2 chai" - number right after "add"
        var addNumberMatch = System.Text.RegularExpressions.Regex.Match(message, @"add\s+(\d+)\s+", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (addNumberMatch.Success && int.TryParse(addNumberMatch.Groups[1].Value, out var qty2))
        {
            return qty2;
        }
        
        return null;
    }

    private string? ExtractSearchTerm(string message)
    {
        var searchMatch = System.Text.RegularExpressions.Regex.Match(message, @"(?:search|find|look\s+for)\s+(?:for\s+)?['""]?([^'""]+?)['""]?(?:\s|$)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (searchMatch.Success)
        {
            return searchMatch.Groups[1].Value.Trim();
        }
        
        // Fallback: extract text after search/find
        var fallbackMatch = System.Text.RegularExpressions.Regex.Match(message, @"(?:search|find)\s+(.+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (fallbackMatch.Success)
        {
            return fallbackMatch.Groups[1].Value.Trim();
        }
        
        return null;
    }

    private async Task<List<ProductDto>> SearchProductsAsync(string searchTerm)
    {
        // Try vector search first (RAG) if available
        if (_embeddingService != null)
        {
            try
            {
                var vectorResults = await _embeddingService.SearchProductsAsync(searchTerm, 5);
                if (vectorResults.Any())
                {
                    _logger.LogDebug("Found {Count} products using vector search", vectorResults.Count);
                    return vectorResults;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Vector search failed, falling back to text search");
            }
        }
        
        // Fallback to traditional text search
        if (_catalogService == null)
            return new List<ProductDto>();
        
        try
        {
            var result = await _catalogService.GetProductsAsync(1, 10, null, null, false, true, searchTerm);
            return result.Items.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products");
            return new List<ProductDto>();
        }
    }

    private async Task<(bool Success, string ProductName, string? Description, string? ErrorMessage)> AddToCartAsync(int productId, int quantity, string? userId = null)
    {
        var currentUserId = userId ?? _userId;
        _logger.LogInformation("AddToCartAsync called - ProductId: {ProductId}, Quantity: {Quantity}, UserId: {UserId}", 
            productId, quantity, currentUserId);
        
        if (_cartService == null)
        {
            _logger.LogError("Cart service is null");
            return (false, string.Empty, null, "Cart service not available");
        }
        
        if (string.IsNullOrEmpty(currentUserId))
        {
            _logger.LogError("User ID is null or empty");
            return (false, string.Empty, null, "User ID not available");
        }
        
        try
        {
            // Get product info for the response
            ProductDto? product = null;
            if (_catalogService != null)
            {
                product = await _catalogService.GetProductByIdAsync(productId);
                _logger.LogDebug("Product found: {ProductName}", product?.ProductName);
            }
            
            var success = await _cartService.AddToCartAsync(currentUserId, productId, (short)quantity);
            _logger.LogInformation("AddToCartAsync result: {Success}", success);
            
            if (success)
            {
                return (true, product?.ProductName ?? $"Product {productId}", product?.Description, null);
            }
            else
            {
                _logger.LogWarning("Failed to add product {ProductId} to cart for user {UserId}", productId, currentUserId);
                return (false, string.Empty, null, "Failed to add product to cart. Product may be discontinued or out of stock.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding product {ProductId} to cart for user {UserId}", productId, currentUserId);
            return (false, string.Empty, null, ex.Message);
        }
    }

    private async Task<string> HandleOrderHistoryQueryAsync(string userMessage, string? userId)
    {
        if (_orderService == null || string.IsNullOrEmpty(userId))
        {
            return string.Empty;
        }
        
        try
        {
            var customerId = await GetCustomerIdFromUserIdAsync(userId);
            if (string.IsNullOrEmpty(customerId))
            {
                return "❌ Could not find your customer information.";
            }
            
            // Extract limit from message (e.g., "last 3 orders", "show 5 orders")
            var limit = ExtractNumberFromMessage(userMessage) ?? 5;
            if (limit > 20) limit = 20; // Cap at 20
            
            var orders = await _orderService.GetOrdersByCustomerIdAsync(customerId);
            var recentOrders = orders.OrderByDescending(o => o.OrderDate).Take(limit).ToList();
            
            if (!recentOrders.Any())
            {
                return "📋 You don't have any orders yet.";
            }
            
            var orderList = string.Join("\n\n", recentOrders.Select((o, idx) => 
                $"**Order #{o.OrderId}** - {o.OrderDate:MMM dd, yyyy}\n" +
                $"Status: {o.CurrentStatus}\n" +
                $"Items: {o.OrderDetails.Count} products\n" +
                $"Total: ${o.Total:F2}\n" +
                (o.ShippedDate.HasValue ? $"Shipped: {o.ShippedDate:MMM dd, yyyy}\n" : "") +
                (o.TrackingNumber != null ? $"Tracking: {o.TrackingNumber}" : "")));
            
            return $"📋 Your last {recentOrders.Count} order(s):\n\n{orderList}\n\nYou can ask me to reorder any of these!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order history");
            return "❌ Could not retrieve your order history. Please try again.";
        }
    }
    
    private async Task<string> HandleReorderRequestAsync(string userMessage, string? userId)
    {
        if (_orderService == null || _cartService == null || string.IsNullOrEmpty(userId))
        {
            return string.Empty;
        }
        
        try
        {
            var customerId = await GetCustomerIdFromUserIdAsync(userId);
            if (string.IsNullOrEmpty(customerId))
            {
                return "❌ Could not find your customer information.";
            }
            
            // Extract order ID or time period
            var orderId = ExtractOrderIdFromMessage(userMessage);
            var timePeriod = ExtractTimePeriodFromMessage(userMessage); // "last month", "last week", etc.
            
            IEnumerable<OrderDto> orders;
            if (orderId.HasValue)
            {
                var order = await _orderService.GetOrderByIdAsync(orderId.Value, customerId);
                orders = order != null ? new[] { order } : Enumerable.Empty<OrderDto>();
            }
            else if (timePeriod.HasValue)
            {
                var allOrders = await _orderService.GetOrdersByCustomerIdAsync(customerId);
                var cutoffDate = DateTime.UtcNow.AddDays(-timePeriod.Value);
                orders = allOrders.Where(o => o.OrderDate >= cutoffDate).OrderByDescending(o => o.OrderDate);
            }
            else
            {
                // Default to most recent order
                var allOrders = await _orderService.GetOrdersByCustomerIdAsync(customerId);
                orders = allOrders.OrderByDescending(o => o.OrderDate).Take(1);
            }
            
            var ordersList = orders.ToList();
            if (!ordersList.Any())
            {
                return "❌ No orders found matching your request.";
            }
            
            var addedCount = 0;
            var failedProducts = new List<string>();
            
            foreach (var order in ordersList)
            {
                foreach (var detail in order.OrderDetails)
                {
                    var result = await AddToCartAsync(detail.ProductId, detail.Quantity, userId);
                    if (result.Success)
                    {
                        addedCount++;
                    }
                    else
                    {
                        failedProducts.Add($"{detail.ProductName} (may be discontinued or out of stock)");
                    }
                }
            }
            
            var response = $"✅ Successfully added {addedCount} item(s) to your cart from {ordersList.Count} order(s)!";
            if (failedProducts.Any())
            {
                response += $"\n\n⚠️ Could not add: {string.Join(", ", failedProducts.Take(3))}";
            }
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing reorder request");
            return "❌ Could not process your reorder request. Please try again.";
        }
    }
    
    private async Task<string> HandleCartModificationAsync(string userMessage, string? userId)
    {
        if (_cartService == null || string.IsNullOrEmpty(userId))
        {
            return string.Empty;
        }
        
        try
        {
            var cart = await _cartService.GetCartAsync(userId);
            if (cart == null || !cart.Lines.Any())
            {
                return "🛒 Your cart is empty.";
            }
            
            // Extract product info and new quantity
            var productInfo = ExtractProductInfo(userMessage);
            var newQuantity = ExtractQuantity(userMessage);
            
            if (!newQuantity.HasValue)
            {
                return "❌ Please specify the new quantity (e.g., 'change quantity of chai to 5').";
            }
            
            if (productInfo.ProductId.HasValue)
            {
                var cartLine = cart.Lines.FirstOrDefault(l => l.ProductId == productInfo.ProductId.Value);
                if (cartLine != null)
                {
                    var success = await _cartService.UpdateQuantityAsync(userId, cartLine.Id, (short)newQuantity.Value);
                    if (success)
                    {
                        return $"✅ Updated {cartLine.ProductName} quantity to {newQuantity.Value}.";
                    }
                }
            }
            else if (!string.IsNullOrEmpty(productInfo.ProductName))
            {
                // Find product by name
                var cartLine = cart.Lines.FirstOrDefault(l => 
                    l.ProductName.Contains(productInfo.ProductName, StringComparison.OrdinalIgnoreCase));
                if (cartLine != null)
                {
                    var success = await _cartService.UpdateQuantityAsync(userId, cartLine.Id, (short)newQuantity.Value);
                    if (success)
                    {
                        return $"✅ Updated {cartLine.ProductName} quantity to {newQuantity.Value}.";
                    }
                }
                else
                {
                    return $"❌ Product '{productInfo.ProductName}' not found in your cart.";
                }
            }
            else
            {
                return "❌ Could not identify which product to update. Please specify the product name or ID.";
            }
            
            return "❌ Could not update cart item.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error modifying cart");
            return "❌ Could not modify your cart. Please try again.";
        }
    }
    
    private async Task<string> HandleShippingEstimateQueryAsync(string userMessage, string? userId)
    {
        if (_orderService == null || string.IsNullOrEmpty(userId))
        {
            return string.Empty;
        }
        
        try
        {
            var customerId = await GetCustomerIdFromUserIdAsync(userId);
            if (string.IsNullOrEmpty(customerId))
            {
                return "❌ Could not find your customer information.";
            }
            
            // Extract order ID from message
            var orderId = ExtractOrderIdFromMessage(userMessage);
            if (!orderId.HasValue)
            {
                // Check for "my order" or "current order" - get most recent
                var orders = await _orderService.GetOrdersByCustomerIdAsync(customerId);
                var recentOrder = orders.OrderByDescending(o => o.OrderDate).FirstOrDefault();
                if (recentOrder != null)
                {
                    orderId = recentOrder.OrderId;
                }
            }
            
            if (!orderId.HasValue)
            {
                return "❌ Could not find the order. Please specify an order ID.";
            }
            
            var order = await _orderService.GetOrderByIdAsync(orderId.Value, customerId);
            if (order == null)
            {
                return "❌ Order not found.";
            }
            
            var response = $"📦 **Order #{order.OrderId}**\n";
            response += $"Status: {order.CurrentStatus}\n";
            
            if (order.ShippedDate.HasValue)
            {
                response += $"✅ Shipped on: {order.ShippedDate:MMM dd, yyyy}\n";
                if (order.RequiredDate.HasValue)
                {
                    var daysUntilRequired = (order.RequiredDate.Value - order.ShippedDate.Value).Days;
                    response += $"Expected delivery: {order.RequiredDate:MMM dd, yyyy} ({daysUntilRequired} days)\n";
                }
            }
            else if (order.RequiredDate.HasValue)
            {
                var daysUntilRequired = (order.RequiredDate.Value - DateTime.UtcNow).Days;
                response += $"Expected shipping: {order.RequiredDate:MMM dd, yyyy} ({daysUntilRequired} days)\n";
            }
            else
            {
                response += "⏳ Shipping date not yet determined.\n";
            }
            
            if (!string.IsNullOrEmpty(order.TrackingNumber))
            {
                response += $"📮 Tracking: {order.TrackingNumber}\n";
            }
            
            if (!string.IsNullOrEmpty(order.ShipperName))
            {
                response += $"🚚 Carrier: {order.ShipperName}";
            }
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shipping estimate");
            return "❌ Could not retrieve shipping information. Please try again.";
        }
    }
    
    private async Task<string> HandleSmartSuggestionsAsync(string userMessage, string? userId)
    {
        if (_catalogService == null || _cartService == null || string.IsNullOrEmpty(userId))
        {
            return string.Empty;
        }
        
        try
        {
            // Extract product ID or name from message
            var productInfo = ExtractProductInfo(userMessage);
            int? productId = null;
            
            if (productInfo.ProductId.HasValue)
            {
                productId = productInfo.ProductId.Value;
            }
            else if (!string.IsNullOrEmpty(productInfo.ProductName))
            {
                var products = await SearchProductsAsync(productInfo.ProductName);
                if (products.Any())
                {
                    productId = products.First().ProductId;
                }
            }
            else
            {
                // Get from cart - suggest for items in cart
                var cart = await _cartService.GetCartAsync(userId);
                if (cart != null && cart.Lines.Any())
                {
                    productId = cart.Lines.First().ProductId;
                }
            }
            
            if (!productId.HasValue)
            {
                return "❌ Could not identify the product. Please specify a product name or ID.";
            }
            
            // Get product details
            var product = await _catalogService.GetProductByIdAsync(productId.Value);
            if (product == null)
            {
                return "❌ Product not found.";
            }
            
            // Find complementary products (same category, different products)
            var complementaryProducts = await _catalogService.GetProductsAsync(1, 5, product.CategoryId, null, false, true, null);
            var suggestions = complementaryProducts.Items
                .Where(p => p.ProductId != productId.Value)
                .Take(3)
                .ToList();
            
            if (!suggestions.Any())
            {
                return $"💡 No complementary products found for {product.ProductName}.";
            }
            
            var suggestionList = string.Join("\n", suggestions.Select(p => 
                $"• {p.ProductName} - ${p.UnitPrice:F2} - {p.UnitsInStock} in stock"));
            
            return $"💡 **You might also like these products with {product.ProductName}:**\n\n{suggestionList}\n\nJust ask me to add any of them to your cart!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting smart suggestions");
            return "❌ Could not generate suggestions. Please try again.";
        }
    }
    
    private int? ExtractNumberFromMessage(string message)
    {
        var match = System.Text.RegularExpressions.Regex.Match(message, @"(\d+)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out var number))
        {
            return number;
        }
        return null;
    }
    
    private int? ExtractOrderIdFromMessage(string message)
    {
        // Patterns: "order 123", "order #123", "order ID 123"
        var match = System.Text.RegularExpressions.Regex.Match(message, 
            @"order\s*(?:#|id\s*)?(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var orderId))
        {
            return orderId;
        }
        return null;
    }
    
    private int? ExtractTimePeriodFromMessage(string message)
    {
        var lower = message.ToLowerInvariant();
        
        if (lower.Contains("last month") || lower.Contains("past month"))
            return 30;
        if (lower.Contains("last week") || lower.Contains("past week"))
            return 7;
        if (lower.Contains("last 2 months") || lower.Contains("past 2 months"))
            return 60;
        if (lower.Contains("last 3 months") || lower.Contains("past 3 months"))
            return 90;
        
        // Extract number of days
        var match = System.Text.RegularExpressions.Regex.Match(message, 
            @"last\s+(\d+)\s+days?", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var days))
        {
            return days;
        }
        
        return null;
    }

    private List<ChatAction> GetActionsFromResponse(string response)
    {
        var actions = new List<ChatAction>();
        
        // Check if response indicates a product was added
        if (response.Contains("✅") && response.Contains("added") && response.Contains("cart"))
        {
            var action = new ChatAction
            {
                Type = "product_added",
                Message = "Product added to cart successfully"
            };
            
            // Include product details if available
            if (_lastAddedProduct != null)
            {
                var productData = new Dictionary<string, object>();
                var productType = _lastAddedProduct.GetType();
                var nameProp = productType.GetProperty("productName");
                var descProp = productType.GetProperty("description");
                
                if (nameProp != null)
                    productData["productName"] = nameProp.GetValue(_lastAddedProduct)?.ToString() ?? "";
                if (descProp != null)
                    productData["description"] = descProp.GetValue(_lastAddedProduct)?.ToString() ?? "";
                
                action.Data = productData;
                _lastAddedProduct = null; // Clear after use
            }
            
            actions.Add(action);
        }
        
        // Check if items were reordered
        if (response.Contains("✅") && response.Contains("reorder"))
        {
            actions.Add(new ChatAction
            {
                Type = "items_reordered",
                Message = "Items reordered successfully"
            });
        }
        
        return actions;
    }
}
