-- Índice 1: Para o JOIN entre Clientes e Documentos, cobrindo o Valor para o SUM
CREATE NONCLUSTERED INDEX IX_Documentos_ClienteId 
ON Documentos (ClienteId) INCLUDE (Valor);

-- Índice 2: Para o JOIN entre Documentos e OcorrenciasEntrega
CREATE NONCLUSTERED INDEX IX_OcorrenciasEntrega_DocId 
ON OcorrenciasEntrega (DocId);