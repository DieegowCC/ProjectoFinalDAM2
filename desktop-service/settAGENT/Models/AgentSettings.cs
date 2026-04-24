using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace settAGENT.Models
{
    public class AgentSettings
    {
        // Nota para Diego (de Miquel): Si vamos a trabajar con clases o modelos prefiero tenerlos separados por archivos que no tener 2 en uno con el nombre del otro archivo.
        // He creado este archivo solo para AgentSettings, ya que no creo que sea buena praxis tenerlo todo junto en ActivitySnapshot. 
        public string ApiUrl { get; set; } = string.Empty;
        public int WorkerId { get; set; } = 1;
        public int CollectionIntervalSeconds { get; set; } = 10;
        public int InactivityThresholdMinutes { get; set; } = 5;
    }
}
