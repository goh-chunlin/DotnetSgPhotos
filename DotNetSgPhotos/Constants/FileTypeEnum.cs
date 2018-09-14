using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetSgPhotos.Constants
{
    public enum FileTypeEnum
    {
        [ContentType("image/png")]
        PNG = 0,

        [ContentType("image/jpg")]
        JPG = 1,

        [ContentType("image/jpeg")]
        JPEG = 2,

        [ContentType("image/bmp")]
        BMP = 3,

        [ContentType("image/gif")]
        GIF = 4
    }
}
