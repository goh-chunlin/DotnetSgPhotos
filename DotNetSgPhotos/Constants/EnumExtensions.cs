using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetSgPhotos.Constants
{
    public static class EnumExtensions
    {
        // Reference: https://stackoverflow.com/a/19621436/1177328
        public static T GetAttribute<T>(this Enum value) where T : Attribute
        {
            var type = value.GetType();
            var memberInfo = type.GetMember(value.ToString());
            var attributes = memberInfo[0].GetCustomAttributes(typeof(T), false);
            return (T)attributes[0];
        }

        // Reference: https://blogs.msdn.microsoft.com/dotnet/2017/03/09/new-features-in-c-7-0/
        public static string ToContentType(this Enum value)
        {
            var attribute = value.GetAttribute<ContentTypeAttribute>();
            return attribute == null ? value.ToString() : attribute.ContentType;
        }
    }

    class ContentTypeAttribute : Attribute
    {
        public string ContentType;

        public ContentTypeAttribute(string contentType)
        {
            ContentType = contentType;
        }
    }
}
