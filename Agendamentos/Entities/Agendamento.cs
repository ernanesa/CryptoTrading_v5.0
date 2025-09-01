using System.ComponentModel.DataAnnotations.Schema;

namespace Agendamentos.Entities
{
    public class Agendamento
    {
        public int Id { get; set; }
        public string? Cron { get; set; }
        public string? Route { get; set; }
        public bool IsActive { get; set; }

        [NotMapped]
        public DateTime ProximaOcorrencia { get; set; }
        public void AdicionarProximaOcorrencia(DateTime proximaOcorrencia)
        {
            ProximaOcorrencia = proximaOcorrencia;
        }
    }
}