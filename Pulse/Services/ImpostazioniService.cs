using Microsoft.EntityFrameworkCore;
using Pulse.Models;

namespace Pulse.Services;

public class ImpostazioniService : IImpostazioniService
{
    private readonly PulseContext _context;

    public ImpostazioniService(PulseContext context)
    {
        _context = context;
    }

    public async Task<Impostazioni> GetImpostazioniAsync()
    {
        var impostazioni = await _context.Impostazionis.FirstOrDefaultAsync();

        if (impostazioni == null)
        {
            impostazioni = new Impostazioni
            {
                OrarioScaglionato = 0,
                StampaRicevutaCortesia = 1,
                StampaDocumentoPrivacy = 1,
                NumeroColoriCorsi = 6,
                EmailUseSsl = 1
            };

            _context.Impostazionis.Add(impostazioni);
            await _context.SaveChangesAsync();
        }

        return impostazioni;
    }

    public async Task<bool> SalvaImpostazioniAsync(Impostazioni impostazioni)
    {
        _context.Impostazionis.Update(impostazioni);
        return await _context.SaveChangesAsync() > 0;
    }
}