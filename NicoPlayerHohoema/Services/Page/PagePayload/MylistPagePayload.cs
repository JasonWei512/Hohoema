﻿using NicoPlayerHohoema.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Services.Page
{
    public class MylistPagePayload : PagePayloadBase
    {
        public string Id { get; set; }
        public PlaylistOrigin? Origin { get; set; }

        public MylistPagePayload() { }

        public MylistPagePayload(string id)
        {
            Id = id;
        }
    }
}
