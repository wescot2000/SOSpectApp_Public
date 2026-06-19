// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

// DTOs/RankingPoliticoDto.cs
// Módulo político: DTOs para el ranking de políticos locales por métrica.
// Creado: 2026-04-07

using Newtonsoft.Json;
using System.Collections.Generic;

namespace sospect.DTOs
{
    /// <summary>
    /// Entrada de un político en el ranking para una métrica dada.
    /// </summary>
    public class RankingPoliticoItemDto
    {
        [JsonProperty("politicoId")]
        public int PoliticoId { get; set; }

        [JsonProperty("nombreCompleto")]
        public string NombreCompleto { get; set; } = string.Empty;

        [JsonProperty("partido")]
        public string? Partido { get; set; }

        [JsonProperty("fotoUrl")]
        public string? FotoUrl { get; set; }

        [JsonProperty("valor")]
        public decimal? Valor { get; set; }

        /// <summary>
        /// Texto formateado del valor para mostrar en la UI.
        /// </summary>
        [JsonProperty("valorTexto")]
        public string ValorTexto { get; set; } = string.Empty;

        public bool TieneFoto => !string.IsNullOrEmpty(FotoUrl);

        /// <summary>
        /// Posición en el ranking (1-5). Se asigna en el ViewModel, no viene del API.
        /// </summary>
        public int Posicion { get; set; }
    }

    /// <summary>
    /// Respuesta del endpoint ObtenerRankingPoliticos.
    /// Contiene top 5 mejores y top 5 peores para la métrica solicitada.
    /// </summary>
    public class RankingPoliticosDto
    {
        [JsonProperty("metrica")]
        public string Metrica { get; set; } = string.Empty;

        [JsonProperty("top5")]
        public List<RankingPoliticoItemDto> Top5 { get; set; } = new();

        [JsonProperty("tail5")]
        public List<RankingPoliticoItemDto> Tail5 { get; set; } = new();
    }
}


