using GeoAPI.Geometries;
using Microsoft.AspNetCore.Mvc;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetSgPhotos.Data.ViewModels
{
    public class PhotoViewModel : FileUploadViewModel
    {
        public List<Photo> Photos { get; set; }

        [Required]
        public string Name { get; set; }

        [HiddenInput]
        public double Latitude { get; set; } = 1.3450524;

        [HiddenInput]
        public double Longitude { get; set; } = 103.8046703;
    }
}
