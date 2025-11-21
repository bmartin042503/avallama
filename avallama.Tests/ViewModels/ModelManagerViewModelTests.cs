using System.Collections.Generic;
using System.Threading.Tasks;
using avallama.Models;
using avallama.Tests.Fixtures;
using avallama.ViewModels;
using Moq;
using Xunit;

namespace avallama.Tests.ViewModels;

public class ModelManagerViewModelTests (TestServicesFixture fixture) : IClassFixture<TestServicesFixture>
{
    [Fact]
    public async Task LoadModelsData_HasModelsInMemory_ExceedsPaginationLimit_ShouldExposeOnlyLimitedNumberOfModels()
    {
        fixture.ModelCacheMock.Reset();

        var models = new List<OllamaModel>();

        for (var i = 0; i < 75; i++)
        {
            models.Add(
                new OllamaModel
                {
                    Name = "Name-" + i,
                    Size = i * 1000_000_000,
                    Family = new OllamaModelFamily
                    {
                        Name = "Family-" + i,
                        Description = "Desc-" + i,
                    }
                }
            );
        }

        fixture.ModelCacheMock
            .Setup(o => o.GetCachedModelsAsync())
            .ReturnsAsync(models);

        var vm = new ModelManagerViewModel(
            fixture.DialogMock.Object,
            fixture.OllamaMock.Object,
            fixture.ModelCacheMock.Object
        );

        await vm.InitializeAsync();

        Assert.Equal(ModelManagerViewModel.PaginationLimit, vm.Models.Count);
        Assert.True(vm.IsPaginationButtonVisible);
    }

    // Pagination jelenleg így működik:

    // ModelManagerViewModelben három lista van amit lehet használni:
    // - _modelsData (összes modell, kizárólag a memóriában, SortingOption kiválasztásakor újra lesznek sortolva)
    // - _filteredModelsData (a _modelsData-ból kiveszi ide a keresésnek megfelelő szűrt modelleket)
    // - Models (kirenderelt modellek, amikkel a felhasználó interaktálhat is)

    // - Models adatai lehetnek a _modelsData-ból vagy a _filteredModelsData-ból attól függően, hogy van-e keresés vagy nincs

    // -----------------

    // További tesztesetekamik kellhetnek:
    // - PaginationButton nem látszódik, ha PaginationLimit alatti a vm.Models.Count
    // - Paginate végig működik sok random generált OllamaModel listára (mondjuk 500ra), tehát hogy
    // az elején beadja, végigmegy ciklusban és a végén amikor már nem tud több modellt megjeleníteni akkor eltűnik a PaginationButton
    // - Keresésnél találat esetén megfelelő szűrt listát adja vissza, ha nincs találat akkor üres, ellenőrzés hogy HasModelsToDisplay true stb.
    // - Letöltött modellek megjelenítése (ha nincs letöltött modell és kiválasztja a Downloaded optiont rendezésre akkor nem történik semmi)
    // - Méret csökkenő/növekvő rendezés működése
    // - Pull count csökkenő/növekvő rendezés működése
}
