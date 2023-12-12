using Newtonsoft.Json;

namespace MFC.Models;

public class Price
{
    [JsonProperty("amount")] public string Amount { get; set; }

    [JsonProperty("currency")] public string Currency { get; set; }
}