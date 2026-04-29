using sospect.ViewModels;

namespace sospect.Views
{
    public partial class DetallePromocionPage : ContentPage
    {
        public DetallePromocionPage(long subscripcionId)
        {
            InitializeComponent();
            BindingContext = new DetallePromocionViewModel(subscripcionId);
        }
    }
}
