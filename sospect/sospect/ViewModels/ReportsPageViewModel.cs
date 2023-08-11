    using System;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Newtonsoft.Json;
    using sospect.AuthHelpers;
    using sospect.Helpers;
    using sospect.Models;
    using sospect.Popups;
    using sospect.Views;
    using Xamarin.Essentials;
    using Xamarin.Forms;

    namespace sospect.ViewModels
    {
        public class ReportsPageViewModel : BaseViewModel
        {
            public ICommand ReporteBasicoCommand { get; }
            public ICommand ReporteCalorCommand { get; }
            public ICommand TimeEvoReportCommand { get; }
            public ICommand PreCogReportsCommand { get; }
            public ICommand AnaliticaDeTextosCommand { get; }

            public ReportsPageViewModel(INavigation navigation)
            {

                ReporteBasicoCommand = new Command(async () => await ReporteBasicoClicked(navigation));
                ReporteCalorCommand = new Command(async () => await ReporteCalorClicked(navigation));
                TimeEvoReportCommand = new Command(async () => await TimeEvoReportClicked(navigation));
                PreCogReportsCommand = new Command(() => PreCogReportsClicked(navigation));
                AnaliticaDeTextosCommand = new Command(async () => await AnaliticaDeTextosClicked(navigation));
            }




            private async Task ReporteBasicoClicked(INavigation navigation)
            {
                var BasicReportPage = new BasicReportPage();
                await navigation.PushAsync(BasicReportPage);
            }

            private async Task ReporteCalorClicked(INavigation navigation)
            {
                var HeatReportPage = new HeatReportPage();
                await navigation.PushAsync(HeatReportPage);
            }

            private async Task TimeEvoReportClicked(INavigation navigation)
            {
                var TimeEvolutionReportPage = new TimeEvolutionReportPage();
                await navigation.PushAsync(TimeEvolutionReportPage);
            }

            private async Task PreCogReportsClicked(INavigation navigation)
            {
                var PredictiveReportPage = new PredictiveReportPage();
                await navigation.PushAsync(PredictiveReportPage);
            }

            private async Task AnaliticaDeTextosClicked(INavigation navigation)
            {
                var TextAnalyticsPage = new TextAnalyticsPage();
                await navigation.PushAsync(TextAnalyticsPage);
            }

        }
    }

