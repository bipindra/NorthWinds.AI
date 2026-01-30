using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Northwind.Portal.Data.Contexts;
using Northwind.Portal.Data.Repositories;
using Northwind.Portal.Domain.DTOs;
using Northwind.Portal.Domain.Services;
using Northwind.Portal.Web.Models;
using Bipins.AI;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add User Secrets for development (connection strings, etc.)
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

// Logging configuration
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
if (builder.Environment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
}
else
{
    builder.Logging.SetMinimumLevel(LogLevel.Information);
}

// Configuration
var databaseOptions = builder.Configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() 
    ?? new DatabaseOptions { DbProvider = "SqlServer" };

var connectionString = builder.Configuration.GetConnectionString("NorthwindsDb") 
    ?? throw new InvalidOperationException("Connection string 'NorthwindsDb' not found. Please set it in User Secrets (Development) or appsettings.json (Production).");

// Database setup
if (databaseOptions.DbProvider == "Sqlite")
{
    builder.Services.AddDbContext<NorthwindDbContext>(options =>
        options.UseSqlite(connectionString));
    
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(connectionString));
}
else
{
    builder.Services.AddDbContext<NorthwindDbContext>(options =>
        options.UseSqlServer(connectionString));
    
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));
}

// Identity
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// Ensure roles are included in claims
builder.Services.AddScoped<IUserClaimsPrincipalFactory<IdentityUser>, UserClaimsPrincipalFactory<IdentityUser, IdentityRole>>();

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CustomerArea", policy => policy.RequireRole("CustomerUser", "CustomerApprover", "SuperAdmin"));
    options.AddPolicy("AdminArea", policy => policy.RequireRole("AdminOps", "AdminCatalog", "AdminFulfillment", "SuperAdmin"));
    options.AddPolicy("RequireAdminOps", policy => policy.RequireRole("AdminOps", "SuperAdmin"));
    options.AddPolicy("RequireCatalogAdmin", policy => policy.RequireRole("AdminCatalog", "SuperAdmin"));
    options.AddPolicy("RequireFulfillmentAdmin", policy => policy.RequireRole("AdminFulfillment", "SuperAdmin"));
});

// Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, Northwind.Portal.Data.Services.TenantContext>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<ICatalogService, Northwind.Portal.Data.Services.CatalogService>();
builder.Services.AddScoped<IOrderService, Northwind.Portal.Data.Services.OrderService>();
builder.Services.AddScoped<ICartService, Northwind.Portal.Data.Services.CartService>();

// AI Services
// Load LLM configuration
var llmOptions = builder.Configuration.GetSection("LLMOptions").Get<LLMOptions>();

// Register Bipins.AI LLM Provider (must be registered before IChatService)
if (llmOptions != null && !string.IsNullOrEmpty(llmOptions.Provider))
{
    // Register Bipins.AI services and LLM provider via IBipinsAIBuilder
    var aiBuilder = builder.Services.AddBipinsAI(_ => { });
    
    switch (llmOptions.Provider)
    {
        case "OpenAI":
            aiBuilder.AddOpenAI(options =>
            {
                // Read API key from configuration (User Secrets or Environment Variables)
                // Expected key: "LLMOptions:OpenAI:ApiKey" or "OpenAI:ApiKey"
                var apiKey = builder.Configuration["LLMOptions:OpenAI:ApiKey"] 
                    ?? builder.Configuration["OpenAI:ApiKey"] 
                    ?? string.Empty;
                options.ApiKey = apiKey;
            });
            break;
            
        case "Anthropic":
            aiBuilder.AddAnthropic(options =>
            {
                // Read API key from configuration (User Secrets or Environment Variables)
                // Expected key: "LLMOptions:Anthropic:ApiKey" or "Anthropic:ApiKey"
                var apiKey = builder.Configuration["LLMOptions:Anthropic:ApiKey"] 
                    ?? builder.Configuration["Anthropic:ApiKey"] 
                    ?? string.Empty;
                options.ApiKey = apiKey;
            });
            break;
            
        case "AzureOpenAI":
            aiBuilder.AddAzureOpenAI(options =>
            {
                // Read configuration from User Secrets or Environment Variables
                // Expected keys: "LLMOptions:AzureOpenAI:Endpoint", "LLMOptions:AzureOpenAI:ApiKey", etc.
                var endpoint = builder.Configuration["LLMOptions:AzureOpenAI:Endpoint"] 
                    ?? builder.Configuration["AzureOpenAI:Endpoint"] 
                    ?? string.Empty;
                var apiKey = builder.Configuration["LLMOptions:AzureOpenAI:ApiKey"] 
                    ?? builder.Configuration["AzureOpenAI:ApiKey"] 
                    ?? string.Empty;
                
                options.Endpoint = endpoint;
                options.ApiKey = apiKey;
                
                var apiVersion = builder.Configuration["LLMOptions:AzureOpenAI:ApiVersion"] 
                    ?? builder.Configuration["AzureOpenAI:ApiVersion"];
                if (!string.IsNullOrEmpty(apiVersion))
                {
                    options.ApiVersion = apiVersion;
                }
            });
            break;
    }
}

// Register IChatService
if (llmOptions != null)
{
    builder.Services.AddSingleton<Bipins.AI.LLM.IChatService>(sp =>
    {
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
        var provider = sp.GetRequiredService<Bipins.AI.Providers.ILLMProvider>();
        
        // Configure ChatServiceOptions directly from app LLMOptions
        var options = new Bipins.AI.LLM.ChatServiceOptions
        {
            Model = llmOptions.Provider switch
            {
                "OpenAI" => llmOptions.OpenAI.Model,
                "Anthropic" => llmOptions.Anthropic.Model,
                "AzureOpenAI" => llmOptions.AzureOpenAI.DeploymentName,
                _ => "gpt-4"
            },
            Temperature = llmOptions.Provider switch
            {
                "OpenAI" => llmOptions.OpenAI.Temperature,
                "Anthropic" => llmOptions.Anthropic.Temperature,
                "AzureOpenAI" => llmOptions.AzureOpenAI.Temperature,
                _ => 0.7
            },
            MaxTokens = llmOptions.Provider switch
            {
                "OpenAI" => llmOptions.OpenAI.MaxTokens,
                "Anthropic" => llmOptions.Anthropic.MaxTokens,
                "AzureOpenAI" => llmOptions.AzureOpenAI.MaxTokens,
                _ => 2000
            },
            EmbeddingModel = llmOptions.Provider switch
            {
                "OpenAI" => llmOptions.OpenAI.EmbeddingModel ?? "text-embedding-3-small",
                "AzureOpenAI" => llmOptions.AzureOpenAI.EmbeddingDeploymentName ?? "text-embedding-3-small",
                _ => "text-embedding-3-small"
            }
        };

        return new Bipins.AI.LLM.ChatService(
            provider,
            options,
            loggerFactory.CreateLogger<Bipins.AI.LLM.ChatService>());
    });
}

// Register Vector Store
var vectorOptions = builder.Configuration.GetSection(VectorDbOptions.SectionName).Get<VectorDbOptions>();
RegisterVectorStore(builder.Services, vectorOptions);

// Helper method to register vector store services
static void RegisterVectorStore(IServiceCollection services, VectorDbOptions? vectorOptions)
{
    if (vectorOptions == null || string.IsNullOrEmpty(vectorOptions.Provider))
    {
        // Fallback to no-op implementation if vector store not configured
        // Note: If you need IVectorMemoryStore, create a NoOpVectorStore implementation
        return;
    }

    // Register Bipins.AI services and vector stores via IBipinsAIBuilder
    // AddBipinsAI returns IBipinsAIBuilder which can be chained with provider-specific Add methods
    var builder = services.AddBipinsAI(_ => { });

    switch (vectorOptions.Provider)
    {
        case "Qdrant":
            builder.AddQdrant(options =>
            {
                options.Endpoint = vectorOptions.Qdrant?.Endpoint ?? "http://localhost:6333";
                options.DefaultCollectionName = vectorOptions.Qdrant?.CollectionName ?? "northwind_portal";
                options.CreateCollectionIfMissing = vectorOptions.Qdrant?.CreateCollectionIfMissing ?? true;
            });
            break;

        case "Pinecone":
            builder.AddPinecone(options =>
            {
                // Configure Pinecone options
                // Note: Property names may need adjustment based on actual Bipins.AI API
                if (vectorOptions.Pinecone != null && !string.IsNullOrEmpty(vectorOptions.Pinecone.ApiKey))
                {
                    options.ApiKey = vectorOptions.Pinecone.ApiKey;
                }
                if (vectorOptions.Pinecone != null && !string.IsNullOrEmpty(vectorOptions.Pinecone.Environment))
                {
                    options.Environment = vectorOptions.Pinecone.Environment;
                }
            });
            break;

        case "Milvus":
            builder.AddMilvus(options =>
            {
                // Configure Milvus options
                // Note: Property names may need adjustment based on actual Bipins.AI API
                // For now, using basic configuration - adjust as needed
            });
            break;

        case "Weaviate":
            builder.AddWeaviate(options =>
            {
                // Configure Weaviate options
                if (vectorOptions.Weaviate != null)
                {
                    options.Endpoint = vectorOptions.Weaviate.Endpoint ?? "http://localhost:8080";
                    if (!string.IsNullOrEmpty(vectorOptions.Weaviate.ApiKey))
                    {
                        options.ApiKey = vectorOptions.Weaviate.ApiKey;
                    }
                }
            });
            break;

        default:
            // Fallback to no-op implementation for unsupported providers
            return;
    }
}

builder.Services.AddScoped<Northwind.Portal.AI.Services.IChatProcessor>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Northwind.Portal.AI.Services.ChatProcessor>>();
    var chatService = sp.GetService<Bipins.AI.LLM.IChatService>();
    var catalogService = sp.GetService<ICatalogService>();
    var cartService = sp.GetService<ICartService>();
    // Note: userId will be passed per-request, not injected
    return new Northwind.Portal.AI.Services.ChatProcessor(logger, chatService, catalogService, cartService);
});

// MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var northwindContext = services.GetRequiredService<NorthwindDbContext>();
        var identityContext = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var initializerLogger = services.GetRequiredService<ILogger<Northwind.Portal.Data.Seed.DbInitializer>>();

        var initializer = new Northwind.Portal.Data.Seed.DbInitializer(northwindContext, identityContext, userManager, roleManager, initializerLogger);
        await initializer.InitializeAsync();
        
        var programLogger = services.GetRequiredService<ILogger<Program>>();
        programLogger.LogInformation("Database initialized successfully");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database.");
    }
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// Custom error handling - only in production
if (!app.Environment.IsDevelopment())
{
    app.UseStatusCodePagesWithReExecute("/Error/{0}");
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .WithTags("Health");

// Areas routing
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// Default routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
