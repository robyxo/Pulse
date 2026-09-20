using Microsoft.EntityFrameworkCore;
using Pulse.Models;

namespace Pulse.Services;

public class ImpostazioniService : IImpostazioniService
{
    private readonly IDbContextFactory<PulseContext> _contextFactory;

    public ImpostazioniService(IDbContextFactory<PulseContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Impostazioni> GetImpostazioniAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var impostazioni = await context.Impostazionis.FirstOrDefaultAsync();

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

            context.Impostazionis.Add(impostazioni);
            await context.SaveChangesAsync();
        }

        return impostazioni;
    }

    public async Task<bool> SalvaImpostazioniAsync(Impostazioni impostazioni)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        context.Impostazionis.Update(impostazioni);
        return await context.SaveChangesAsync() > 0;
    }
}