# C-SQL-INTEGRACOES
Teste de conhecimento Técnico. 


PROCESSO DE TESTE TÉCNICO – DESENVOLVEDOR C# / SQL / INTEGRAÇÕES 

 

Contexto Uma transportadora precisa registrar ocorrências de entrega 

associadas a documentos fiscais. O candidato deverá implementar algumas 

funcionalidades comuns em sistemas logísticos. 

 

Tecnologias sugeridas: - C# - ASP.NET MVC ou Web API - SQL Server - 

JavaScript básico (AJAX) 

 

========================PARTE 1 – BANCO DE DADOS======================== 

 

Script de criação: 

 

CREATE TABLE Clientes ( ClienteId INT IDENTITY PRIMARY KEY, Nome 

VARCHAR(200), CpfCnpj VARCHAR(14) ); 

 

CREATE TABLE Documentos ( DocId INT IDENTITY PRIMARY KEY, ClienteId INT, 

NumeroDocumento VARCHAR(50), Valor DECIMAL(10,2), DataEmissao DATETIME 

); 

 

CREATE TABLE OcorrenciasEntrega ( OcorrenciaId INT IDENTITY PRIMARY KEY, 

DocId INT, DataOcorrencia DATETIME, Latitude DECIMAL(10,6), Longitude 

DECIMAL(10,6), StatusEntrega INT ); 

 

EXERCÍCIO 1 – CONSULTA SQL 

 

Criar uma consulta que retorne: 

-   Nome do cliente 

-   Quantidade de documentos 

-   Valor total dos documentos 

-   Quantidade de ocorrências registradas 

 

Ordenar pelo maior valor total de documentos. 


Resposta --  
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
 

EXERCÍCIO 2 – PERFORMANCE 

Criar índices para melhorar a consulta anterior. 

Explicar: - quais índices foram criados - por que foram escolhidos 

Resposta -- 

Para otimizar essa consulta específica, precisamos focar nas chaves estrangeiras (usadas nos JOINs) e nas colunas de agregação para evitar Key Lookups.
-- Índice 1: Para o JOIN entre Clientes e Documentos, cobrindo o Valor para o SUM
CREATE NONCLUSTERED INDEX IX_Documentos_ClienteId 
ON Documentos (ClienteId) INCLUDE (Valor);

-- Índice 2: Para o JOIN entre Documentos e OcorrenciasEntrega
CREATE NONCLUSTERED INDEX IX_OcorrenciasEntrega_DocId 
ON OcorrenciasEntrega (DocId);
Explicação --
IX_Documentos_ClienteId: Escolhido porque a tabela Documentos é filtrada pelo ClienteId no JOIN. O INCLUDE (Valor) transforma este num índice coberto (Covering Index), permitindo que o SQL Server faça o SUM(Valor) diretamente pelo índice, sem precisar ler a tabela principal.

IX_OcorrenciasEntrega_DocId: Escolhido para acelerar o JOIN da tabela de ocorrências através do DocId. Sem ele, o banco faria um Table Scan para cada documento.

====================== PARTE 2 – PROCESSAMENTO C#======================= 

Classe base:  

public class OcorrenciaEntrega { public int DocId { get; set; } public 

DateTime DataOcorrencia { get; set; } public string HoraOcorrencia { 

get; set; } } 
 

Criar um método que junte DataOcorrencia + HoraOcorrencia retornando um 

DateTime completo. 

 

Exemplo: 

Data: 13/03/2026 Hora: “11:28” 

Resultado esperado: 

13/03/2026 11:28:00 

Resposta -- 

A maneira mais segura de juntar a data com a hora em formato texto é extraindo a parte da data e somando com um TimeSpan.

public class OcorrenciaEntrega 
{ 
    public int DocId { get; set; } 
    public DateTime DataOcorrencia { get; set; } 
    public string HoraOcorrencia { get; set; } 

    public DateTime ObterDataHoraCompleta()
    {
        if (TimeSpan.TryParse(HoraOcorrencia, out TimeSpan horaFormatada))
        {
            return DataOcorrencia.Date.Add(horaFormatada);
        }        
        throw new FormatException("O formato da HoraOcorrencia é inválido.");
    }
}

 

====================== PARTE 3 – PARSER DE CHAVE NFE====================== 

Criar método: 

ParseChaveNFe(string chave) 

Entrada: 

35240312345678000190550010000012341000012345 

Retornar: 

-   UF 

-   Ano 

-   Mês 

-   CNPJ 

-   Modelo 

-   Série 

-   Número 

Resposta -- 

A chave da NFe tem 44 posições fixas. Um simples Substring resolve de forma eficiente.
 
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

====================== PARTE 4 – INTEGRAÇÃO API CEP======================== 

Criar um serviço que consulte: 

https://brasilapi.com.br/api/cep/v1/{cep} 

Retornar um objeto: 

Endereco { Cep Logradouro Cidade Estado } 

Se ocorrer erro na API: retornar mensagem amigável. 

Resposta -- 

Usando as boas práticas modernas do .NET (evitando o erro do código de análise que veremos abaixo).

using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public class Endereco
{
    public string Cep { get; set; }
    public string Street { get; set; }
    public string City { get; set; }
    public string State { get; set; }
}

public class CepService
{
    private readonly HttpClient _httpClient;

    // Idealmente, injetar via IHttpClientFactory no construtor
    public CepService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Endereco> BuscarCepAsync(string cep)
    {
        try
        {
            var response = await _httpClient.GetAsync($"https://brasilapi.com.br/api/cep/v1/{cep}");
            
            if (!response.IsSuccessStatusCode)
            {
                // Tratar 404 ou outros erros específicos
                return null; 
            }

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<Endereco>(json, options);
        }
        catch (HttpRequestException)
        {
            throw new Exception("Não foi possível conectar ao serviço de CEP no momento. Tente novamente mais tarde.");
        }
    }
}

======================== PARTE 5 – FRONTEND ======================== 

Criar uma página simples contendo: 

Campo: CEP Botão: Buscar 

Ao clicar em buscar: 

-   consultar a API criada 

-   exibir Rua, Cidade e Estado na tela 

Pode usar: - JavaScript puro - ou jQuery 

Resposta -- 

Uma página simples em HTML com JavaScript puro (fetch API), bem leve para rodar no navegador.

<!DOCTYPE html>
<html lang="pt-BR">
<head>
    <meta charset="UTF-8">
    <title>Consulta CEP</title>
</head>
<body>
    <h2>Buscar Endereço</h2>
    <input type="text" id="cepInput" placeholder="Digite o CEP (só números)">
    <button onclick="buscarCep()">Buscar</button>

    <div id="resultado" style="margin-top: 20px;"></div>

    <script>
        async function buscarCep() {
            const cep = document.getElementById('cepInput').value;
            const divResultado = document.getElementById('resultado');
            
            if(!cep) {
                divResultado.innerHTML = "Por favor, digite um CEP.";
                return;
            }

            divResultado.innerHTML = "Buscando...";

            try {
                // Aqui você chamaria a sua API C# que criamos na Parte 4
                // Exemplo simulado chamando direto a BrasilAPI para a prova:
                const response = await fetch(`https://brasilapi.com.br/api/cep/v1/${cep}`);
                
                if (!response.ok) {
                    throw new Error("CEP não encontrado ou erro na API.");
                }

                const data = await response.json();
                divResultado.innerHTML = `
                    <p><strong>Rua:</strong> ${data.street}</p>
                    <p><strong>Cidade:</strong> ${data.city}</p>
                    <p><strong>Estado:</strong> ${data.state}</p>
                `;
            } catch (error) {
                divResultado.innerHTML = `<span style="color:red;">${error.message}</span>`;
            }
        }
    </script>
</body>
</html>

====================== PERGUNTAS DE ENTREVISTA======================== 

Anexar em arquivo texto as respostas no Git. 

1)  Qual a diferença entre JOIN, EXISTS e IN? 

JOIN: Usado para combinar linhas de duas ou mais tabelas baseadas em uma coluna comum. Ele retorna os dados combinados e pode multiplicar as linhas se houver relação 1 para N.

EXISTS: É um operador lógico (retorna booleano). É altamente performático pois o banco de dados para a busca na subquery assim que encontra a primeira ocorrência (curto-circuito). Usado apenas para verificar se um registro existe, sem trazer os dados dele.

IN: Compara um valor com uma lista estática ou resultado de uma subquery. Geralmente é menos performático que o EXISTS em tabelas grandes, pois o SQL Server pode precisar processar toda a subquery antes de fazer a comparação.

2)  Como você identifica uma query lenta em produção? 

Uso ferramentas como o Query Store do SQL Server (excelente para ver regressões de plano de execução), as Dynamic Management Views (DMVs) como sys.dm_exec_query_stats cruzado com sys.dm_exec_sql_text, e ferramentas de APM (Application Performance Monitoring) configuradas na aplicação. O Extended Events também é útil para rastrear queries que demoram mais de X milissegundos.

3)  Qual a diferença entre Task, Thread e async/await? 

Thread: É uma unidade de execução a nível de Sistema Operacional. Criar threads é custoso (consome memória e CPU para troca de contexto).

Task: É uma abstração de alto nível do C# para uma operação assíncrona (uma promessa de que algo vai terminar no futuro). Uma Task não necessariamente cria uma nova Thread; ela pode rodar na mesma Thread usando I/O assíncrono.

async/await: É açúcar sintático (syntactic sugar) que cria uma máquina de estado por baixo dos panos. O await libera a Thread atual para fazer outras coisas enquanto a Task não termina, evitando o bloqueio da aplicação.

4)  Como tratar falha de API externa? 

Implementando resiliência. Em .NET, a biblioteca padrão ouro para isso é o Polly. Eu aplicaria o padrão de Retry (tentar novamente com backoff exponencial) para falhas transitórias e Circuit Breaker (abrir o circuito para não sobrecarregar a API caso ela caia de vez). Além disso, garantir bons Timeouts, logging estruturado da falha e, quando aplicável, retornar um valor de fallback (cache) para o usuário.

5)  O que pode causar deadlock no SQL Server? 

Ocorre quando a Transação A bloqueia o Recurso 1 e precisa do Recurso 2, enquanto a Transação B bloqueia o Recurso 2 e precisa do 1. Causas comuns:
Transações lendo/atualizando tabelas em ordens diferentes.
Falta de índices adequados (forçando o SQL a fazer Table Scans e dar lock na tabela inteira).
Transações muito longas segurando locks por muito tempo. 

Pergunta final:
Conte um bug difícil que você resolveu e como chegou à solução. 

Resposta -- 



================= ANÁLISE DE CÓDIGO (IDENTIFICAR ERROS)================== 
Código C#:  

public async Task BuscarCep(string cep) { HttpClient client = new 
HttpClient(); 
    var response = await client.GetAsync("https://brasilapi.com.br/api/cep/v1/" + cep); 
    var json = response.Content.ReadAsStringAsync().Result; 
    return json; 
} 

Pergunta: Quais são os problemas nesse código? 

Resposta -- 
Assinatura errada: O método retorna Task mas deveria retornar Task<string> já que ele tenta retornar a variável json.

Mistura de async com síncrono (Deadlock): O uso de .Result no meio de um método assíncrono (em vez de await response.Content.ReadAsStringAsync()) trava a thread atual e pode causar deadlocks graves, especialmente em contextos com SynchronizationContext.

HttpClient instanciado localmente: Criar um new HttpClient() a cada chamada leva ao esgotamento de sockets (Socket Exhaustion). 
Deve ser estático ou injetado via IHttpClientFactory.

Falta de tratamento de erro: Não há try/catch nem validação de response.IsSuccessStatusCode. 
Se o CEP for inválido, vai gerar exceção não tratada.

 

SQL:  

SELECT * FROM Documentos d JOIN Clientes c ON c.ClienteId = d.ClienteId 
WHERE YEAR(DataEmissao) = 2024  

Pergunta: Quais são os problemas de performance nesta consulta? 

Resposta -- 

SELECT *: Traz colunas desnecessárias, aumentando o tráfego de rede e uso de I/O do banco. Deve-se especificar apenas as colunas necessárias.

Não-SARGable (O maior problema): A cláusula WHERE YEAR(DataEmissao) = 2024 impede o uso de índices na coluna DataEmissao. 
O banco terá que varrer a tabela inteira e calcular a função YEAR() para cada linha.

Solução correta: WHERE DataEmissao >= '2024-01-01' AND DataEmissao < '2025-01-01'.
 

====================== PERGUNTA DE ARQUITETURA======================== 

Se o sistema recebesse 10.000 ocorrências de entrega por minuto, como 
você processaria esses dados sem travar o banco de dados? 
Descreva a arquitetura ou estratégia que utilizaria. 

Resposta -- 
Receber 10.000 ocorrências por minuto significa picos de escrita concorrentes. Se a API gravar direto no SQL Server, 
geraremos contenção de disco, possíveis deadlocks e estouro do pool de conexões.

Estratégia (Padrão Mensageria / CQRS inicial):
Ingestão via Fila (Desacoplamento): A API (Web API) não grava no banco. 
Ela apenas recebe o payload, valida o básico e publica a mensagem num Message Broker (como RabbitMQ, Azure Service Bus ou Kafka). 
A API retorna 202 Accepted imediatamente para o cliente.

Worker de Processamento: Um ou mais serviços em background (rodando isolados via Podman ou Docker, por exemplo) atuam como consumidores (Workers) lendo dessa fila.

Inserção em Lote (Batching): O Worker retira as mensagens da fila em blocos (ex: de 100 em 100) e faz a persistência no banco de dados usando SqlBulkCopy (no caso do C#) ou inserções agrupadas para reduzir drásticamente o número de idas e vindas (round-trips) ao banco.

Escalabilidade: Dessa forma, se o volume dobrar para 20.000, o SQL Server não sofre. A fila apenas cresce e os workers processam no próprio ritmo, mantendo a saúde do ecossistema.

