using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GLMS.Shared.Dtos;
using GLMS.Shared.Models;
using Microsoft.AspNetCore.Http;

namespace EAPD7111_PART2.Services.Api;

public class GlmsApiClient : IGlmsApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public GlmsApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", request, cancellationToken);
        return await ReadRequiredAsync<LoginResponse>(response, cancellationToken);
    }

    public async Task<IReadOnlyList<Client>> GetClientsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/clients", cancellationToken);
        return await ReadRequiredAsync<List<Client>>(response, cancellationToken);
    }

    public async Task<Client?> GetClientAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/clients/{id}", cancellationToken);
        return await ReadOptionalAsync<Client>(response, cancellationToken);
    }

    public async Task<Client> CreateClientAsync(Client client, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/clients", client, cancellationToken);
        return await ReadRequiredAsync<Client>(response, cancellationToken);
    }

    public async Task<Client> UpdateClientAsync(int id, Client client, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/clients/{id}", client, cancellationToken);
        return await ReadRequiredAsync<Client>(response, cancellationToken);
    }

    public async Task DeleteClientAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/clients/{id}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<IReadOnlyList<Contract>> GetContractsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        ContractStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (startDate.HasValue)
        {
            query.Add($"startDate={Uri.EscapeDataString(startDate.Value.ToString("yyyy-MM-dd"))}");
        }

        if (endDate.HasValue)
        {
            query.Add($"endDate={Uri.EscapeDataString(endDate.Value.ToString("yyyy-MM-dd"))}");
        }

        if (status.HasValue)
        {
            query.Add($"status={status.Value}");
        }

        var path = query.Count == 0 ? "api/contracts" : $"api/contracts?{string.Join("&", query)}";
        var response = await _httpClient.GetAsync(path, cancellationToken);
        return await ReadRequiredAsync<List<Contract>>(response, cancellationToken);
    }

    public async Task<Contract?> GetContractAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/contracts/{id}", cancellationToken);
        return await ReadOptionalAsync<Contract>(response, cancellationToken);
    }

    public async Task<Contract> CreateContractAsync(
        CreateContractDto contract,
        IFormFile? signedAgreement,
        CancellationToken cancellationToken = default)
    {
        using var content = BuildContractFormContent(contract, signedAgreement);
        var response = await _httpClient.PostAsync("api/contracts", content, cancellationToken);
        return await ReadRequiredAsync<Contract>(response, cancellationToken);
    }

    public async Task<Contract> UpdateContractAsync(
        int id,
        Contract contract,
        IFormFile? signedAgreement,
        CancellationToken cancellationToken = default)
    {
        using var content = BuildContractFormContent(contract, signedAgreement);
        var response = await _httpClient.PutAsync($"api/contracts/{id}", content, cancellationToken);
        return await ReadRequiredAsync<Contract>(response, cancellationToken);
    }

    public async Task DeleteContractAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/contracts/{id}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<(byte[] Content, string FileName, string ContentType)> DownloadContractAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/contracts/{id}/download", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/pdf";
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            ?? $"Contract_{id}.pdf";

        return (content, fileName, contentType);
    }

    public async Task<IReadOnlyList<ServiceRequest>> GetServiceRequestsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/servicerequests", cancellationToken);
        return await ReadRequiredAsync<List<ServiceRequest>>(response, cancellationToken);
    }

    public async Task<ServiceRequest?> GetServiceRequestAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/servicerequests/{id}", cancellationToken);
        return await ReadOptionalAsync<ServiceRequest>(response, cancellationToken);
    }

    public async Task<ServiceRequest> CreateServiceRequestAsync(
        CreateServiceRequestDto serviceRequest,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/servicerequests", serviceRequest, cancellationToken);
        return await ReadRequiredAsync<ServiceRequest>(response, cancellationToken);
    }

    public async Task<ServiceRequest> UpdateServiceRequestAsync(
        int id,
        ServiceRequest serviceRequest,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/servicerequests/{id}", serviceRequest, cancellationToken);
        return await ReadRequiredAsync<ServiceRequest>(response, cancellationToken);
    }

    public async Task DeleteServiceRequestAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/servicerequests/{id}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<ExchangeRateResponse> GetExchangeRateAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/servicerequests/exchange-rate", cancellationToken);
        return await ReadRequiredAsync<ExchangeRateResponse>(response, cancellationToken);
    }

    public async Task<bool> IsApiReachableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            var response = await _httpClient.GetAsync("api/health", cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static MultipartFormDataContent BuildContractFormContent(object contract, IFormFile? signedAgreement)
    {
        var content = new MultipartFormDataContent();

        foreach (var property in contract.GetType().GetProperties())
        {
            var value = property.GetValue(contract);
            if (value == null)
            {
                continue;
            }

            content.Add(new StringContent(ConvertValue(value)), property.Name);
        }

        if (signedAgreement is { Length: > 0 })
        {
            var streamContent = new StreamContent(signedAgreement.OpenReadStream());
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                signedAgreement.ContentType ?? "application/pdf");
            content.Add(streamContent, "signedAgreement", signedAgreement.FileName);
        }

        return content;
    }

    private static string ConvertValue(object value)
    {
        return value switch
        {
            DateTime dateTime => dateTime.ToString("O"),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O"),
            Enum enumValue => enumValue.ToString(),
            _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty
        };
    }

    private static async Task<T> ReadRequiredAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        await EnsureSuccessAsync(response, cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
        if (result == null)
        {
            throw new ApiClientException(response.StatusCode, "The API returned an empty response body.");
        }

        return result;
    }

    private static async Task<T?> ReadOptionalAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
        where T : class
    {
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new ApiClientException(response.StatusCode, body);
    }
}
