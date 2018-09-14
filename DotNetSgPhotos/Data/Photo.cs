using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetSgPhotos.Data
{
    public class Photo : BaseEntity
    {
        public string Url { get; set; }

        public List<FacialExpression> FacialExpressions { get; set; }

        public Point Location { get; set; }
    }
}
