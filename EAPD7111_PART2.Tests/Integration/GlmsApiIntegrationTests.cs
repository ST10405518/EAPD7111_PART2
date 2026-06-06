using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GLMS.Api;
using GLMS.Shared.Dtos;
using GLMS.Shared.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EAPD7111_PART2.Tests.Integration;

public class GlmsApiIntegrationTests : IClassFixture<GlmsApiWebApplicationFactory>
{
    private readonly GlmsApiWebApplicationFactory _factory;

    public GlmsApiIntegrationTests(GlmsApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetContracts_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/contracts");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Username = "admin",
            Password = "Admin@123"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
    }

    [Fact]
    public async Task GetContracts_WithToken_ReturnsOkAndJson()
    {
        var client = await CreateAuthenticatedClientAsync();
        var response = await client.GetAsync("/api/contracts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var contracts = await response.Content.ReadFromJsonAsync<List<Contract>>();
        Assert.NotNull(contracts);
    }

    [Fact]
    public async Task CreateClient_ThenGetById_ReturnsCreatedClient()
    {
        var client = await CreateAuthenticatedClientAsync();
        var newClient = new Client
        {
            Name = "Integration Test Client",
            Email = "integration@test.com",
            PhoneNumber = "078 840 8161",
            Address = "Test Address",
            Region = "Gauteng"
        };

        var createResponse = await client.PostAsJsonAsync("/api/clients", newClient);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<Client>();
        Assert.NotNull(created);
        Assert.True(created!.ClientId > 0);

        var getResponse = await client.GetAsync($"/api/clients/{created.ClientId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var fetched = await getResponse.Content.ReadFromJsonAsync<Client>();
        Assert.NotNull(fetched);
        Assert.Equal(newClient.Name, fetched!.Name);
    }

    [Fact]
    public async Task CreateContract_AndPatchStatus_Works()
    {
        var client = await CreateAuthenticatedClientAsync();

        var clientResponse = await client.PostAsJsonAsync("/api/clients", new Client
        {
            Name = "Contract Test Client",
            Email = "contract@test.com",
            PhoneNumber = "078 840 8161",
            Address = "123 Test",
            Region = "WC"
        });
        var createdClient = await clientResponse.Content.ReadFromJsonAsync<Client>();

        var contractDto = new CreateContractDto
        {
            ClientId = createdClient!.ClientId,
            ContractNumber = "INT-TEST-001",
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddYears(1),
            Status = ContractStatus.Draft,
            ServiceLevel = "Gold SLA"
        };

        var createContract = await client.PostAsJsonAsync("/api/contracts", contractDto);
        Assert.Equal(HttpStatusCode.Created, createContract.StatusCode);

        var contract = await createContract.Content.ReadFromJsonAsync<Contract>();
        Assert.NotNull(contract);

        var patchResponse = await client.PatchAsJsonAsync(
            $"/api/contracts/{contract!.ContractId}/status",
            new UpdateContractStatusDto { Status = ContractStatus.Active });

        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);
        var updated = await patchResponse.Content.ReadFromJsonAsync<Contract>();
        Assert.Equal(ContractStatus.Active, updated!.Status);
    }

    [Fact]
    public async Task GetExchangeRate_ReturnsPositiveRate()
    {
        var client = await CreateAuthenticatedClientAsync();
        var response = await client.GetAsync("/api/servicerequests/exchange-rate");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rate = await response.Content.ReadFromJsonAsync<ExchangeRateResponse>();
        Assert.NotNull(rate);
        Assert.True(rate!.Rate > 0);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Username = "admin",
            Password = "Admin@123"
        });
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login!.Token);
        return client;
    }
}
