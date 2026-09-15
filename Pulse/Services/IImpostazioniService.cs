using Pulse.Models;

namespace Pulse.Services;

public interface IImpostazioniService
{
    Task<Impostazioni> GetImpostazioniAsync();
    Task<bool> SalvaImpostazioniAsync(Impostazioni impostazioni);
}