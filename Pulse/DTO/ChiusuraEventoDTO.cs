using Pulse.Models;
using System;

namespace Pulse.DTO
{
    public class ChiusuraEventoDTO
    {
        public CalendarioChiusure Chiusura { get; }

        public ChiusuraEventoDTO(CalendarioChiusure chiusura)
        {
            Chiusura = chiusura;
        }

        public int Id => Chiusura.Id;
        public string Tipo => string.IsNullOrWhiteSpace(Chiusura.Tipo) ? "Chiusura" : Chiusura.Tipo!;
        public bool IsEvento => string.Equals(Tipo, "Evento", StringComparison.OrdinalIgnoreCase);
        public string Motivo => Chiusura.Motivo ?? string.Empty;
        public bool EmailGiaInviata => Chiusura.EmailInviata == 1;

        public DateTime? DataInizioDate => DateTime.TryParse(Chiusura.DataInizio, out var d) ? d : null;
        public DateTime? DataFineDate => DateTime.TryParse(Chiusura.DataFine, out var d) ? d : null;

        public string PeriodoVisualizzato
        {
            get
            {
                var inizio = DataInizioDate;
                var fine = DataFineDate;
                if (inizio == null || fine == null) return "-";

                return inizio.Value.Date == fine.Value.Date
                    ? inizio.Value.ToString("dd/MM/yyyy")
                    : $"{inizio.Value:dd/MM/yyyy} → {fine.Value:dd/MM/yyyy}";
            }
        }

        public int GiorniTotali
        {
            get
            {
                var inizio = DataInizioDate;
                var fine = DataFineDate;
                if (inizio == null || fine == null) return 0;
                return (fine.Value.Date - inizio.Value.Date).Days + 1;
            }
        }

        public string IconaTipo => IsEvento ? "🎉" : "🚫";
        public string ColoreTipo => IsEvento ? "#8B5CF6" : "#EF4444";
        public string EtichettaTipo => IsEvento ? "Evento" : "Chiusura";
    }
}