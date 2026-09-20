using Microsoft.AspNetCore.Mvc;
using TaskManagement.Core.Users;

namespace TaskManagement.Api.Users;

[ApiController]
[Route("api/users")]
public sealed class UsersController(UserService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<UserResponse>> CreateAsync(
        CreateUserRequest request, CancellationToken cancellationToken)
    {
        var (name, email) = request.ToServiceInput();
        var user = await service.CreateAsync(name, email, cancellationToken);
        return CreatedAtAction("GetById", new { id = user.Id }, user.ToResponse());
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> ListAsync(CancellationToken cancellationToken)
    {
        var users = await service.ListAsync(cancellationToken);
        return Ok(users.Select(user => user.ToResponse()).ToArray());
    }

    [HttpGet("{id:int}")]
    [ActionName("GetById")]
    public async Task<ActionResult<UserResponse>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var user = await service.GetByIdAsync(id, cancellationToken);
        return Ok(user.ToResponse());
    }
}
