using Agendamentos.Data;
using Agendamentos.Entities;
using Microsoft.EntityFrameworkCore;
using NCrontab;
using System.Net;

namespace Agendamentos;

public class Worker(ILogger<Worker> logger, IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    private readonly ILogger<Worker> _logger = logger;
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;

    private readonly HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(30) };
    private const int UM_SEGUNDO = 1000;
    private const int MAX_RETRY_ATTEMPTS = 3;
    private const int RETRY_DELAY_MS = 5000;
    private CrontabSchedule? _schedule;
    private bool _firstRun = true;
    private DateTime _lastDbCheck = DateTime.MinValue;
    private readonly TimeSpan _dbCheckInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var dataHoraAtual = DateTime.Now;
        var agendamentos = new List<Agendamento>();

        _logger.LogInformation("Worker iniciado às {Time}", DateTime.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Verifica se precisa recarregar agendamentos do banco
                if (_firstRun || (DateTime.Now - _lastDbCheck) > _dbCheckInterval || dataHoraAtual.Second == 0)
                {
                    var novosAgendamentos = await LoadAgendamentosWithRetryAsync(stoppingToken);
                    // TODO: se o ambiente for o de desenvolvimento, substituir ct_dados, ct_sugestoes e ct_negociacoes por localhost
                    if (novosAgendamentos != null)
                    {
                        agendamentos = SyncAgendamentos(agendamentos, novosAgendamentos);
                        _lastDbCheck = DateTime.Now;
                        _firstRun = false;
                    }
                }

                if (agendamentos.Count == 0)
                {
                    await Task.Delay(UM_SEGUNDO, stoppingToken);
                    dataHoraAtual = DateTime.Now;
                    continue;
                }

                // Processa agendamentos
                await ProcessAgendamentosAsync(agendamentos, dataHoraAtual, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Worker cancelado");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro não tratado no Worker");
                await Task.Delay(RETRY_DELAY_MS, stoppingToken);
            }

            await Task.Delay(UM_SEGUNDO, stoppingToken);
            dataHoraAtual = DateTime.Now;
        }

        _logger.LogInformation("Worker finalizado às {Time}", DateTime.Now);
    }

    private async Task<List<Agendamento>?> LoadAgendamentosWithRetryAsync(CancellationToken cancellationToken)
    {
        for (int attempt = 1; attempt <= MAX_RETRY_ATTEMPTS; attempt++)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<CryptoTradingDbContext>();
                
                var agendamentos = await context.Agendamentos
                    .Where(x => x.IsActive)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("Carregados {Count} agendamentos ativos do banco", agendamentos.Count);
                return agendamentos;
            }
            catch (Exception ex) when (IsTransientError(ex))
            {
                _logger.LogWarning(ex, "Tentativa {Attempt}/{MaxAttempts} falhou ao carregar agendamentos. Tentando novamente em {Delay}ms", 
                    attempt, MAX_RETRY_ATTEMPTS, RETRY_DELAY_MS);
                
                if (attempt < MAX_RETRY_ATTEMPTS)
                {
                    await Task.Delay(RETRY_DELAY_MS, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro não recuperável ao carregar agendamentos");
                break;
            }
        }

        return null;
    }

    private List<Agendamento> SyncAgendamentos(List<Agendamento> agendamentosAtuais, List<Agendamento> novosAgendamentos)
    {
        var agendamentosSync = new List<Agendamento>();

        foreach (var novo in novosAgendamentos)
        {
            var existente = agendamentosAtuais.FirstOrDefault(x => x.Id == novo.Id);
            if (existente != null)
            {
                // Atualiza agendamento existente
                existente.Cron = novo.Cron;
                existente.Route = novo.Route;
                existente.IsActive = novo.IsActive;
                agendamentosSync.Add(existente);
            }
            else
            {
                // Adiciona novo agendamento
                agendamentosSync.Add(novo);
            }
        }

        return agendamentosSync;
    }

    private async Task ProcessAgendamentosAsync(List<Agendamento> agendamentos, DateTime dataHoraAtual, CancellationToken cancellationToken)
    {
        foreach (var agendamento in agendamentos.ToList())
        {
            try
            {
                if (string.IsNullOrEmpty(agendamento.Cron))
                {
                    _logger.LogWarning("Agendamento {Id} tem Cron vazio", agendamento.Id);
                    continue;
                }

                _schedule = CrontabSchedule.Parse(agendamento.Cron);

                // Se é a primeira vez ou não tem próxima ocorrência definida
                if (agendamento.ProximaOcorrencia == default)
                {
                    agendamento.AdicionarProximaOcorrencia(_schedule.GetNextOccurrence(DateTime.Now));
                }

                // Verifica se é hora de executar
                if (dataHoraAtual.ToString("dd/MM/yyyy HH:mm") == agendamento.ProximaOcorrencia.ToString("dd/MM/yyyy HH:mm"))
                {
                    await ExecuteAgendamentoAsync(agendamento, cancellationToken);
                    agendamento.AdicionarProximaOcorrencia(_schedule.GetNextOccurrence(DateTime.Now));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar agendamento {Id}: {Route}", agendamento.Id, agendamento.Route);
            }
        }
    }

    private async Task ExecuteAgendamentoAsync(Agendamento agendamento, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Executando agendamento {Id}: {Route}", agendamento.Id, agendamento.Route);
            
            var response = await httpClient.GetAsync(agendamento.Route, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Agendamento {Id} executado com sucesso. Status: {StatusCode}", 
                    agendamento.Id, response.StatusCode);
            }
            else
            {
                _logger.LogWarning("Agendamento {Id} retornou status não sucesso: {StatusCode}", 
                    agendamento.Id, response.StatusCode);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro HTTP ao executar agendamento {Id}: {Route}", agendamento.Id, agendamento.Route);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError("Timeout ao executar agendamento {Id}: {Route}", agendamento.Id, agendamento.Route);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao executar agendamento {Id}: {Route}", agendamento.Id, agendamento.Route);
        }
    }

    private static bool IsTransientError(Exception ex)
    {
        return ex is TimeoutException ||
               ex is HttpRequestException ||
               (ex is InvalidOperationException && ex.Message.Contains("transient failure")) ||
               (ex.InnerException != null && IsTransientError(ex.InnerException));
    }

    public override void Dispose()
    {
        httpClient?.Dispose();
        base.Dispose();
    }
}