// Messages/VotoAlarmaMessage.cs
// Mensaje para sincronizar estado de votos (verdadero/falso) entre
// DescribirPage (tarjetas) y DetalleDescripcionAlarmaPage.

namespace sospect.Messages
{
    public class VotoAlarmaMessage
    {
        public long AlarmaId { get; }
        public bool EsVerdadero { get; }
        public int CantidadVerdaderos { get; }
        public int CantidadFalsos { get; }

        public VotoAlarmaMessage(long alarmaId, bool esVerdadero, int cantidadVerdaderos, int cantidadFalsos)
        {
            AlarmaId = alarmaId;
            EsVerdadero = esVerdadero;
            CantidadVerdaderos = cantidadVerdaderos;
            CantidadFalsos = cantidadFalsos;
        }
    }
}
