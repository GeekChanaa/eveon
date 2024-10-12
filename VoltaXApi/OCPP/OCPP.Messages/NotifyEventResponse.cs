using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace VoltaXApi.OCPP.Messages
{
    public class NotifyEventResponse
    {
        public CustomDataType CustomData { get; set; }
    }
}
