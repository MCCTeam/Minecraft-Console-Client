using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftClient.EntityHandler
{
    public record Effect
    {
        public EffectType Type { init; get; }

        public int EffectLevel { init; get; }

        public ulong StartTick { init; get; }

        public int DurationInTick { init; get; }

        public bool IsFromBeacon { init; get; }

        public bool ShowParticles { init; get; }

        public bool ShowIcon { init; get; }

        public Dictionary<string, object>? FactorData { init; get; } = null;

        /*  Factor Data
            Name                      Type
            padding_duration          TAG_INT	
            factor_start	          TAG_FLOAT	
            factor_target	          TAG_FLOAT	
            factor_current	          TAG_FLOAT	
            effect_changed_timestamp  TAG_INT
            factor_previous_frame	  TAG_FLOAT
            had_effect_last_tick	  TAG_BOOLEAN
         */
    }
}
