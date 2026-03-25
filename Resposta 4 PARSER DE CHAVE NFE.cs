public class ChaveNFeInfo
{
    public string UF { get; set; }
    public string AnoMes { get; set; }
    public string CNPJ { get; set; }
    public string Modelo { get; set; }
    public string Serie { get; set; }
    public string Numero { get; set; }
}

public ChaveNFeInfo ParseChaveNFe(string chave)
{
    if (string.IsNullOrWhiteSpace(chave) || chave.Length != 44)
        throw new ArgumentException("Chave NFe inválida. Deve conter 44 caracteres.");

    return new ChaveNFeInfo
    {
        UF = chave.Substring(0, 2),
        AnoMes = chave.Substring(2, 4),
        CNPJ = chave.Substring(6, 14),
        Modelo = chave.Substring(20, 2),
        Serie = chave.Substring(22, 3),
        Numero = chave.Substring(25, 9)
    };
}