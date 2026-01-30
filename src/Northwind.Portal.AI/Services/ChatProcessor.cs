using Microsoft.Extensions.Logging;
using Bipins.AI.LLM;
using Northwind.Portal.Domain.Services;
using Northwind.Portal.Domain.DTOs;

namespace Northwind.Portal.AI.Services;

public class ChatProcessor : IChatProcessor
{
    private readonly ILogger<ChatProcessor> _logger;
    private readonly IChatService? _chatService;
    private readonly ICatalogService? _catalogService;
    private readonly ICartService? _cartService;
    private readonly string? _userId;

    public ChatProcessor(
        ILogger<ChatProcessor> logger, 
        IChatService? chatService = null,
        ICatalogService? catalogService = null,
        ICartService? cartService = null,
        string? userId = null)
    {
        _logger = logger;
        _chatService = chatService;
        _catalogService = catalogService;
        _cartService = cartService;
        _userId = userId;
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
        return @"You are a helpful AI assistant for the Northwind Portal. You can help customers:
1. Search for products by name
2. Add products to their shopping cart

When a customer asks to add a product to their cart, you should:
1. First search for the product by name using the search_products function
2. If found, use the add_to_cart function with the product ID and quantity

Available functions:
- search_products(searchTerm): Search for products by name. Returns a list of products with ProductId, ProductName, UnitPrice, and UnitsInStock.
- add_to_cart(productId, quantity): Add a product to the customer's cart. Returns success status.

When you successfully add a product to cart, confirm it to the user with the product name and quantity.
Always be helpful and friendly.";
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

    private async Task<(bool Success, string ProductName, string? ErrorMessage)> AddToCartAsync(int productId, int quantity, string? userId = null)
    {
        var currentUserId = userId ?? _userId;
        _logger.LogInformation("AddToCartAsync called - ProductId: {ProductId}, Quantity: {Quantity}, UserId: {UserId}", 
            productId, quantity, currentUserId);
        
        if (_cartService == null)
        {
            _logger.LogError("Cart service is null");
            return (false, string.Empty, "Cart service not available");
        }
        
        if (string.IsNullOrEmpty(currentUserId))
        {
            _logger.LogError("User ID is null or empty");
            return (false, string.Empty, "User ID not available");
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
                return (true, product?.ProductName ?? $"Product {productId}", null);
            }
            else
            {
                _logger.LogWarning("Failed to add product {ProductId} to cart for user {UserId}", productId, currentUserId);
                return (false, string.Empty, "Failed to add product to cart. Product may be discontinued or out of stock.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding product {ProductId} to cart for user {UserId}", productId, currentUserId);
            return (false, string.Empty, ex.Message);
        }
    }

    private List<ChatAction> GetActionsFromResponse(string response)
    {
        var actions = new List<ChatAction>();
        
        // Check if response indicates a product was added
        if (response.Contains("✅") && response.Contains("added") && response.Contains("cart"))
        {
            actions.Add(new ChatAction
            {
                Type = "product_added",
                Message = "Product added to cart successfully"
            });
        }
        
        return actions;
    }
}
