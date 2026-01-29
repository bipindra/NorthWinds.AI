namespace Northwind.Portal.Web.Models;

public class VectorDbOptions
{
    public const string SectionName = "VectorDbOptions";
    
    public string? Provider { get; set; }
    
    public QdrantOptions? Qdrant { get; set; }
    public PineconeOptions? Pinecone { get; set; }
    public MilvusOptions? Milvus { get; set; }
    public WeaviateOptions? Weaviate { get; set; }
}

public class QdrantOptions
{
    public string Endpoint { get; set; } = "http://localhost:6333";
    public string CollectionName { get; set; } = "northwind_portal";
    public bool CreateCollectionIfMissing { get; set; } = true;
}

public class PineconeOptions
{
    public string? ApiKey { get; set; }
    public string? Environment { get; set; }
    public string? IndexName { get; set; }
}

public class MilvusOptions
{
    public string? Endpoint { get; set; }
    public string? CollectionName { get; set; }
}

public class WeaviateOptions
{
    public string Endpoint { get; set; } = "http://localhost:8080";
    public string? ApiKey { get; set; }
    public string? ClassName { get; set; }
}
