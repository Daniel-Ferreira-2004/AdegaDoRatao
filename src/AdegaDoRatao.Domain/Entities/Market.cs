using AdegaDoRatao.Domain.Common;

namespace AdegaDoRatao.Domain.Entities;

/// <summary>
/// Mercado/loja física do catálogo de comparação (redes Veran, Shibata,
/// Atacadão, Semar etc.). Guarda as coordenadas geográficas usadas pelo
/// filtro de "mercados próximos" (cálculo de distância via Haversine).
/// </summary>
public class Market : BaseAuditableEntity
{
    public string Name { get; private set; } = string.Empty;

    /// <summary>Rede à qual a loja pertence (ex.: Veran, Shibata, Atacadão, Semar).</summary>
    public string Rede { get; private set; } = string.Empty;

    public string Address { get; private set; } = string.Empty;
    public string? City { get; private set; }
    public string? State { get; private set; }
    public string? ZipCode { get; private set; }

    /// <summary>Latitude em graus decimais (-90 a 90).</summary>
    public double Latitude { get; private set; }

    /// <summary>Longitude em graus decimais (-180 a 180).</summary>
    public double Longitude { get; private set; }

    private Market()
    {
    }

    public Market(string name, string rede, string address, string? city, string? state,
        string? zipCode, double latitude, double longitude)
    {
        SetName(name);
        SetRede(rede);
        SetAddress(address);
        City = string.IsNullOrWhiteSpace(city) ? null : city.Trim();
        State = string.IsNullOrWhiteSpace(state) ? null : state.Trim().ToUpperInvariant();
        ZipCode = string.IsNullOrWhiteSpace(zipCode) ? null : zipCode.Trim();
        DefinirCoordenadas(latitude, longitude);
    }

    public void AtualizarDados(string name, string rede, string address, string? city,
        string? state, string? zipCode, double latitude, double longitude)
    {
        SetName(name);
        SetRede(rede);
        SetAddress(address);
        City = string.IsNullOrWhiteSpace(city) ? null : city.Trim();
        State = string.IsNullOrWhiteSpace(state) ? null : state.Trim().ToUpperInvariant();
        ZipCode = string.IsNullOrWhiteSpace(zipCode) ? null : zipCode.Trim();
        DefinirCoordenadas(latitude, longitude);
        MarcarComoAtualizada();
    }

    public void DefinirCoordenadas(double latitude, double longitude)
    {
        if (latitude is < -90 or > 90)
        {
            throw new ArgumentException("A latitude deve estar entre -90 e 90.", nameof(latitude));
        }

        if (longitude is < -180 or > 180)
        {
            throw new ArgumentException("A longitude deve estar entre -180 e 180.", nameof(longitude));
        }

        Latitude = latitude;
        Longitude = longitude;
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("O nome do mercado é obrigatório.", nameof(name));
        }

        Name = name.Trim();
    }

    private void SetRede(string rede)
    {
        if (string.IsNullOrWhiteSpace(rede))
        {
            throw new ArgumentException("A rede do mercado é obrigatória.", nameof(rede));
        }

        Rede = rede.Trim();
    }

    private void SetAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            throw new ArgumentException("O endereço do mercado é obrigatório.", nameof(address));
        }

        Address = address.Trim();
    }
}
