public class OcorrenciaEntrega 
{ 
    public int DocId { get; set; } 
    public DateTime DataOcorrencia { get; set; } 
    public string HoraOcorrencia { get; set; } 

    public DateTime ObterDataHoraCompleta()
    {
        if (TimeSpan.TryParse(HoraOcorrencia, out TimeSpan horaFormatada))
        {
            //Pega apenas a data (ignorando qualquer hora que já estivesse lá) e soma o TimeSpan
            return DataOcorrencia.Date.Add(horaFormatada);
        }
        
        throw new FormatException(O formato da HoraOcorrencia é inválido.);
    }
}