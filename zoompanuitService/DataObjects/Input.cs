using System.Collections.Generic;
using Microsoft.WindowsAzure.Mobile.Service;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace zoompanuitService.DataObjects
{
    public class Input : EntityData
    {

        public virtual string IFeatures { get; set; }
        public int Classification { get; set; }

    }
}