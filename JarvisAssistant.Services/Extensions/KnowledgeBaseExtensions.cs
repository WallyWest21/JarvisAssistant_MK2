using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Services.DocumentProcessors;
using JarvisAssistant.Services.VectorStores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JarvisAssistant.Services.KnowledgeBase.Extensions
{
    /// <summary>
    /// Extension methods for registering knowledge base services in the dependency injection container.
    /// </summary>
    public static class KnowledgeBaseExtensions
    {
        /// <summary>
        /// Adds knowledge base services to the service collection with in-memory storage.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional configuration for knowledge base options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddKnowledgeBase(
            this IServiceCollection services,
            Action<KnowledgeBaseOptions>? configureOptions = null)
        {
            var options = new KnowledgeBaseOptions();
            configureOptions?.Invoke(options);
            services.AddSingleton(options);

            // Register document processors
            services.AddSingleton<IDocumentProcessor>(serviceProvider =>
            {
                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                return new DocumentProcessorFactory(
                    loggerFactory.CreateLogger<DocumentProcessorFactory>(),
                    loggerFactory);
            });

            // Register text chunking service
            services.AddSingleton<ITextChunker, TextChunkingService>();

            // Register embedding service (Ollama by default)
            services.AddHttpClient<OllamaEmbeddingService>();
            services.AddSingleton<IEmbeddingService>(serviceProvider =>
            {
                var httpClient = serviceProvider.GetRequiredService<HttpClient>();
                var logger = serviceProvider.GetRequiredService<ILogger<OllamaEmbeddingService>>();
                return new OllamaEmbeddingService(httpClient, logger, options.OllamaBaseUrl, options.EmbeddingModel);
            });

            // Register vector store (in-memory by default)
            services.AddSingleton<IVectorStore, InMemoryVectorStore>();

            // Register main knowledge base service
            services.AddSingleton<IKnowledgeBaseService, KnowledgeBaseService>();

            return services;
        }

        /// <summary>
        /// Adds knowledge base services with custom embedding service.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="embeddingServiceFactory">Factory for creating the embedding service.</param>
        /// <param name="configureOptions">Optional configuration for knowledge base options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddKnowledgeBaseWithCustomEmbedding<TEmbeddingService>(
            this IServiceCollection services,
            Func<IServiceProvider, TEmbeddingService> embeddingServiceFactory,
            Action<KnowledgeBaseOptions>? configureOptions = null)
            where TEmbeddingService : class, IEmbeddingService
        {
            var options = new KnowledgeBaseOptions();
            configureOptions?.Invoke(options);
            services.AddSingleton(options);

            // Register document processors
            services.AddSingleton<IDocumentProcessor>(serviceProvider =>
            {
                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                return new DocumentProcessorFactory(
                    loggerFactory.CreateLogger<DocumentProcessorFactory>(),
                    loggerFactory);
            });

            // Register text chunking service
            services.AddSingleton<ITextChunker, TextChunkingService>();

            // Register custom embedding service
            services.AddSingleton<IEmbeddingService>(embeddingServiceFactory);

            // Register vector store
            services.AddSingleton<IVectorStore, InMemoryVectorStore>();

            // Register main knowledge base service
            services.AddSingleton<IKnowledgeBaseService, KnowledgeBaseService>();

            return services;
        }

        /// <summary>
        /// Adds knowledge base services with custom vector store.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="vectorStoreFactory">Factory for creating the vector store.</param>
        /// <param name="configureOptions">Optional configuration for knowledge base options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddKnowledgeBaseWithCustomVectorStore<TVectorStore>(
            this IServiceCollection services,
            Func<IServiceProvider, TVectorStore> vectorStoreFactory,
            Action<KnowledgeBaseOptions>? configureOptions = null)
            where TVectorStore : class, IVectorStore
        {
            var options = new KnowledgeBaseOptions();
            configureOptions?.Invoke(options);
            services.AddSingleton(options);

            // Register document processors
            services.AddSingleton<IDocumentProcessor>(serviceProvider =>
            {
                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                return new DocumentProcessorFactory(
                    loggerFactory.CreateLogger<DocumentProcessorFactory>(),
                    loggerFactory);
            });

            // Register text chunking service
            services.AddSingleton<ITextChunker, TextChunkingService>();

            // Register embedding service
            services.AddHttpClient<OllamaEmbeddingService>();
            services.AddSingleton<IEmbeddingService>(serviceProvider =>
            {
                var httpClient = serviceProvider.GetRequiredService<HttpClient>();
                var logger = serviceProvider.GetRequiredService<ILogger<OllamaEmbeddingService>>();
                return new OllamaEmbeddingService(httpClient, logger, options.OllamaBaseUrl, options.EmbeddingModel);
            });

            // Register custom vector store
            services.AddSingleton<IVectorStore>(vectorStoreFactory);

            // Register main knowledge base service
            services.AddSingleton<IKnowledgeBaseService, KnowledgeBaseService>();

            return services;
        }

        /// <summary>
        /// Validates that all required knowledge base services are properly registered.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>A list of validation errors, empty if valid.</returns>
        public static List<string> ValidateKnowledgeBaseServices(this IServiceCollection services)
        {
            var errors = new List<string>();

            var requiredServices = new[]
            {
                typeof(IDocumentProcessor),
                typeof(ITextChunker),
                typeof(IEmbeddingService),
                typeof(IVectorStore),
                typeof(IKnowledgeBaseService)
            };

            foreach (var serviceType in requiredServices)
            {
                if (!services.Any(s => s.ServiceType == serviceType))
                {
                    errors.Add($"Required service {serviceType.Name} is not registered");
                }
            }

            return errors;
        }
    }

    /// <summary>
    /// Configuration options for the knowledge base system.
    /// </summary>
    public class KnowledgeBaseOptions
    {
        /// <summary>
        /// Gets or sets the Ollama base URL for embedding generation.
        /// </summary>
        public string OllamaBaseUrl { get; set; } = "http://localhost:11434";

        /// <summary>
        /// Gets or sets the embedding model name to use.
        /// </summary>
        public string EmbeddingModel { get; set; } = "nomic-embed-text";

        /// <summary>
        /// Gets or sets the maximum file size allowed for document upload (in bytes).
        /// </summary>
        public long MaxFileSizeBytes { get; set; } = 100 * 1024 * 1024; // 100MB

        /// <summary>
        /// Gets or sets the default chunking configuration.
        /// </summary>
        public ChunkingOptions DefaultChunking { get; set; } = new();

        /// <summary>
        /// Gets or sets the search configuration.
        /// </summary>
        public SearchConfiguration Search { get; set; } = new();

        /// <summary>
        /// Gets or sets whether to enable automatic model pulling for Ollama.
        /// </summary>
        public bool EnableAutoModelPull { get; set; } = true;

        /// <summary>
        /// Gets or sets the connection string for PostgreSQL (if using database storage).
        /// </summary>
        public string? PostgreSQLConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the ChromaDB configuration (if using ChromaDB vector store).
        /// </summary>
        public ChromaDbOptions? ChromaDb { get; set; }
    }

    /// <summary>
    /// Default chunking configuration options.
    /// </summary>
    public class ChunkingOptions
    {
        /// <summary>
        /// Gets or sets the default maximum chunk size.
        /// </summary>
        public int DefaultMaxChunkSize { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the default overlap size.
        /// </summary>
        public int DefaultOverlapSize { get; set; } = 100;

        /// <summary>
        /// Gets or sets the default minimum chunk size.
        /// </summary>
        public int DefaultMinChunkSize { get; set; } = 100;

        /// <summary>
        /// Gets or sets the default chunking strategy.
        /// </summary>
        public Core.Models.ChunkingStrategy DefaultStrategy { get; set; } = Core.Models.ChunkingStrategy.Sentence;
    }

    /// <summary>
    /// Search configuration options.
    /// </summary>
    public class SearchConfiguration
    {
        /// <summary>
        /// Gets or sets the default similarity threshold for semantic search.
        /// </summary>
        public float DefaultSimilarityThreshold { get; set; } = 0.1f;

        /// <summary>
        /// Gets or sets the default maximum number of search results.
        /// </summary>
        public int DefaultMaxResults { get; set; } = 10;

        /// <summary>
        /// Gets or sets whether to enable highlighting by default.
        /// </summary>
        public bool DefaultEnableHighlighting { get; set; } = true;

        /// <summary>
        /// Gets or sets the timeout for search operations in milliseconds.
        /// </summary>
        public int SearchTimeoutMs { get; set; } = 30000; // 30 seconds
    }

    /// <summary>
    /// ChromaDB configuration options.
    /// </summary>
    public class ChromaDbOptions
    {
        /// <summary>
        /// Gets or sets the ChromaDB server URL.
        /// </summary>
        public string ServerUrl { get; set; } = "http://localhost:8000";

        /// <summary>
        /// Gets or sets the collection name for storing vectors.
        /// </summary>
        public string CollectionName { get; set; } = "jarvis_knowledge_base";

        /// <summary>
        /// Gets or sets the API key for ChromaDB authentication (if required).
        /// </summary>
        public string? ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the timeout for ChromaDB operations in milliseconds.
        /// </summary>
        public int TimeoutMs { get; set; } = 30000; // 30 seconds
    }
}
