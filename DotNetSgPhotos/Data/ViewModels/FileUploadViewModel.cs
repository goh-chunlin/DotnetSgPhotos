using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetSgPhotos.Data.ViewModels
{
    public class FileUploadViewModel
    {
        [Required]
        public IFormFile File { get; set; }

        public string FileName
        {
            get
            {
                return (File != null) ? File.FileName : "";
            }
        }
    }
}
