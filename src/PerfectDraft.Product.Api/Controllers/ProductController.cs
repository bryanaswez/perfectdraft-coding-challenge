using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Threading;

namespace PerfectDraft.Product.Api.Controllers;

[ApiController]
[Route("products")]
public sealed class ProductController : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return StatusCode(StatusCodes.Status501NotImplemented, new
        {
            message = "Implement product aggregation for GET /products/{id}.",
            requestedId = id
        });
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? search, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return StatusCode(StatusCodes.Status501NotImplemented, new
        {
            message = "Implement product aggregation for GET /products?search=.",
            search
        });
    }
}
