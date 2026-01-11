using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IcsApi.Swagger;

public sealed class ProblemDetailsOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var schema = context.SchemaGenerator.GenerateSchema(typeof(ProblemDetails), context.SchemaRepository);

        var allowAnonymous =
            context.MethodInfo.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any() ||
            context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any() == true;

        if (!allowAnonymous)
        {
            AddProblemDetailsResponse(operation, "401", "Unauthorized", schema);
            AddProblemDetailsResponse(operation, "403", "Forbidden", schema);
        }

        AddProblemDetailsResponse(operation, "500", "Internal Server Error", schema);
    }

    private static void AddProblemDetailsResponse(
        OpenApiOperation operation,
        string statusCode,
        string description,
        Microsoft.OpenApi.Models.OpenApiSchema schema)
    {
        if (operation.Responses.ContainsKey(statusCode))
        {
            return;
        }

        operation.Responses[statusCode] = new OpenApiResponse
        {
            Description = description,
            Content =
            {
                ["application/problem+json"] = new OpenApiMediaType { Schema = schema },
                ["application/json"] = new OpenApiMediaType { Schema = schema }
            }
        };
    }
}
