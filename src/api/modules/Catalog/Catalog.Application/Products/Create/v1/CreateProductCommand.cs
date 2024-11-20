using System.ComponentModel;
using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Products.Create.v1;
public sealed record CreateProductCommand(
    string Name = "Sample Product",
    decimal Price = 10,
    string? Description = "Descriptive Description")
    : IRequest<CreateProductResponse>;
