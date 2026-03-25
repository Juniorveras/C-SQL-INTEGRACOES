SELECT 
    c.Nome,
    COUNT(DISTINCT d.DocId) AS QuantidadeDocumentos,
    ISNULL(SUM(d.Valor), 0) AS ValorTotalDocumentos,
    COUNT(o.OcorrenciaId) AS QuantidadeOcorrencias
FROM Clientes c
LEFT JOIN Documentos d ON c.ClienteId = d.ClienteId
LEFT JOIN OcorrenciasEntrega o ON d.DocId = o.DocId
GROUP BY 
    c.ClienteId, 
    c.Nome
ORDER BY 
    ValorTotalDocumentos DESC;