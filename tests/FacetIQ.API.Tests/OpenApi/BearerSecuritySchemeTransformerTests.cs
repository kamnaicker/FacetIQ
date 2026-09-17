using FacetIQ.API.OpenApi;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace FacetIQ.API.Tests.OpenApi;

public class BearerSecuritySchemeTransformerTests
{
    [Fact]
    public async Task Document_DeclaresBearerScheme()
    {
        var document = await Transformed();

        var scheme = Assert.IsType<OpenApiSecurityScheme>(document.Components!.SecuritySchemes!["Bearer"]);
        Assert.Equal(SecuritySchemeType.Http, scheme.Type);
        Assert.Equal("bearer", scheme.Scheme);
    }

    [Fact]
    public async Task Document_RequiresBearerScheme()
    {
        var document = await Transformed();

        var requirement = Assert.Single(document.Security!);
        var reference = Assert.Single(requirement.Keys);
        Assert.Equal("Bearer", reference.Reference.Id);
    }

    private static async Task<OpenApiDocument> Transformed()
    {
        var document = new OpenApiDocument();
        var context = new OpenApiDocumentTransformerContext
        {
            DocumentName = "v1",
            DescriptionGroups = [],
            ApplicationServices = new ServiceCollection().BuildServiceProvider()
        };

        await new BearerSecuritySchemeTransformer().TransformAsync(document, context, CancellationToken.None);

        return document;
    }
}
