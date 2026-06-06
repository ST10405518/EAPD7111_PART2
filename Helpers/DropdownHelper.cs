using GLMS.Shared.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EAPD7111_PART2.Helpers;

public static class DropdownHelper
{
    public static List<SelectListItem> BuildClientList(IEnumerable<Client> clients, int? selectedId = null)
    {
        var items = new List<SelectListItem>
        {
            new()
            {
                Value = string.Empty,
                Text = "-- Select Client --",
                Selected = !selectedId.HasValue || selectedId.Value == 0
            }
        };

        items.AddRange(clients.Select(c => new SelectListItem
        {
            Value = c.ClientId.ToString(),
            Text = c.Name,
            Selected = selectedId.HasValue && selectedId.Value == c.ClientId
        }));

        return items;
    }

    public static List<SelectListItem> BuildContractList(IEnumerable<Contract> contracts, int? selectedId = null)
    {
        var items = new List<SelectListItem>
        {
            new()
            {
                Value = string.Empty,
                Text = "-- Select Contract --",
                Selected = !selectedId.HasValue || selectedId.Value == 0
            }
        };

        items.AddRange(contracts.Select(c => new SelectListItem
        {
            Value = c.ContractId.ToString(),
            Text = $"{c.ContractNumber} ({c.Client?.Name ?? "Client"})",
            Selected = selectedId.HasValue && selectedId.Value == c.ContractId
        }));

        return items;
    }
}
