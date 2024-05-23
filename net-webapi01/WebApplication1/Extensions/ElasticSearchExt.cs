using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Elasticsearch.Net;
using Nest;
using WebApi.Models;

namespace WebApi.Extensions;

public static class ElasticSearchExtensions
{
    public static void AddElasticsearch(
        this IServiceCollection services, IConfiguration configuration)
    {
        var url = configuration["ELKConfiguration:url"]!;
        var defaultIndex = configuration["ELKConfiguration:index"]!;

        var uri = new Uri(url);
        var settings = new ConnectionSettings(uri)
            .CertificateFingerprint("a46a8b579a66ac5f82f5dbde44acc03d14eb1a4116dc6d6b3c9f975616959451")
            .BasicAuthentication("elastic", "YKW4ZPna=uteS5jl97D6")
            .PrettyJson()
            .DefaultIndex(defaultIndex);

        AddDefaultMappings(settings);

        var client = new ElasticClient(settings);
        services.AddSingleton<IElasticClient>(client);

        CreateIndex(client, defaultIndex);
        
    }

    private static void AddDefaultMappings(ConnectionSettings settings)
    {
        settings
            .DefaultMappingFor<Book>(m => m
                .Ignore(p => p.Price)
                .Ignore(p => p.CoverPicture)
            );
    }

    private static void CreateIndex(IElasticClient client, string indexName)
    {
        var createIndexResponse = client.Indices.Create(indexName,
            index => index.Map<Book>(x => x.AutoMap())
        );
    }
}