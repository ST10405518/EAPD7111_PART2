using GLMS.Api.Repositories;
using GLMS.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GLMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClientsController : ControllerBase
{
    private readonly IClientRepository _clientRepository;

    public ClientsController(IClientRepository clientRepository)
    {
        _clientRepository = clientRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Client>>> GetAll(CancellationToken cancellationToken)
    {
        var clients = await _clientRepository.GetAllAsync(cancellationToken);
        return Ok(clients);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Client>> GetById(int id, CancellationToken cancellationToken)
    {
        var client = await _clientRepository.GetByIdWithContractsAsync(id, cancellationToken);
        if (client == null)
        {
            return NotFound(new { message = $"Client {id} not found." });
        }

        return Ok(client);
    }

    [HttpPost]
    public async Task<ActionResult<Client>> Create([FromBody] Client client, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        client.ClientId = 0;
        client.CreatedDate = DateTime.UtcNow;

        var created = await _clientRepository.AddAsync(client, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.ClientId }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Client>> Update(int id, [FromBody] Client client, CancellationToken cancellationToken)
    {
        if (id != client.ClientId)
        {
            return BadRequest(new { message = "Route id does not match client id." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!await _clientRepository.ExistsAsync(id, cancellationToken))
        {
            return NotFound(new { message = $"Client {id} not found." });
        }

        await _clientRepository.UpdateAsync(client, cancellationToken);
        return Ok(client);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var client = await _clientRepository.GetByIdAsync(id, cancellationToken);
        if (client == null)
        {
            return NotFound(new { message = $"Client {id} not found." });
        }

        await _clientRepository.DeleteAsync(client, cancellationToken);
        return NoContent();
    }
}
