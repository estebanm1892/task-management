using TaskManagement.Api.Users;
using TaskManagement.Core.Users;

namespace TaskManagement.Tests.Api;

public sealed class UserDtoTests
{
    [Fact]
    public void Create_request_maps_nullable_contract_fields_to_service_input()
    {
        var request = new CreateUserRequest("  Ana  ", " ANA@example.com ");

        Assert.Equal(("  Ana  ", " ANA@example.com "), request.ToServiceInput());
    }

    [Fact]
    public void Response_mapping_exposes_public_fields_without_normalized_email()
    {
        var response = new User(7, "Ana", "ana@example.com", "ana@example.com").ToResponse();

        Assert.Equal((7, "Ana", "ana@example.com"), (response.Id, response.Name, response.Email));
    }
}
