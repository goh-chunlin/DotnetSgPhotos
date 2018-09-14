using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DotNetSgPhotos.Models;
using DotNetSgPhotos.Data.ViewModels;
using DotNetSgPhotos.Data.Repositories;
using DotNetSgPhotos.Data;
using NetTopologySuite.Geometries;
using GeoAPI.Geometries;
using System.IO;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using DotNetSgPhotos.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.ProjectOxford.Face;

namespace DotNetSgPhotos.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IRepository<Photo> _repoPhotos;

        public HomeController(IConfiguration configuration, IRepository<Photo> repoPhotos)
        {
            _configuration = configuration;
            _repoPhotos = repoPhotos;
        }

        public async Task<IActionResult> Index()
        {
            var nearestPhotos = await _repoPhotos.GetAll()
                .Where(p => p.Location.Distance(new Point(1.3450524, 103.8046703)) < 0.08d)
                .WithTag("Getting photos near the center of the Singapore.")
                .ToListAsync();

            // *** For CosmosDB only ***
            //var nearestPhotos = _repoPhotos.GetAll()
            //    .Where(p => p.Location.Distance(new Point(1.3450524, 103.8046703)) < 0.08d)
            //    .WithTag("Getting photos near the center of the Singapore.")
            //    .ToList();

            return View(new PhotoViewModel { Photos = nearestPhotos, Name = TempData["MyName"]?.ToString() });
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public async Task<IActionResult> UploadNew(PhotoViewModel model)
        {
            string imageUrl = null;
            var facialProperties = new List<FacialExpression>();

            using (var memoryStream = new MemoryStream())
            {
                await model.File.CopyToAsync(memoryStream);

                string fileName = model.File.FileName;

                if (fileName.Contains("\\"))
                {
                    fileName = fileName.Substring(fileName.LastIndexOf("\\") + 1);
                }

                imageUrl = await UploadFileToAzureStorageAsync(
                    _configuration["AzureStorageSettings:ConnectionString"],
                    _configuration["AzureStorageSettings:ContainerName"],
                    memoryStream.ToArray(),
                    fileName);

                // Reference: https://cloud.tencent.com/developer/ask/125850
                memoryStream.Position = 0;

                try
                {
                    var faceServiceClient = new FaceServiceClient(_configuration["FaceApiServiceKey"], "https://southeastasia.api.cognitive.microsoft.com/face/v1.0");
                    var faces = await faceServiceClient.DetectAsync(memoryStream, false, false,
                        new FaceAttributeType[] {
                        FaceAttributeType.Smile,
                        FaceAttributeType.Accessories,
                        FaceAttributeType.Blur,
                        FaceAttributeType.Age,
                        FaceAttributeType.Emotion,
                        FaceAttributeType.Exposure,
                        FaceAttributeType.FacialHair,
                        FaceAttributeType.Glasses,
                        FaceAttributeType.Gender,
                        FaceAttributeType.Makeup,
                        FaceAttributeType.Noise,
                        FaceAttributeType.Occlusion
                        });

                    foreach (var face in faces)
                    {
                        facialProperties.Add(new FacialExpression { Label = "Anger", Value = face.FaceAttributes.Emotion.Anger });
                        facialProperties.Add(new FacialExpression { Label = "Contempt", Value = face.FaceAttributes.Emotion.Contempt });
                        facialProperties.Add(new FacialExpression { Label = "Disgust", Value = face.FaceAttributes.Emotion.Disgust });
                        facialProperties.Add(new FacialExpression { Label = "Fear", Value = face.FaceAttributes.Emotion.Fear });
                        facialProperties.Add(new FacialExpression { Label = "Happiness", Value = face.FaceAttributes.Emotion.Happiness });
                        facialProperties.Add(new FacialExpression { Label = "Neutral", Value = face.FaceAttributes.Emotion.Neutral });
                        facialProperties.Add(new FacialExpression { Label = "Sadness", Value = face.FaceAttributes.Emotion.Sadness });
                        facialProperties.Add(new FacialExpression { Label = "Surprise", Value = face.FaceAttributes.Emotion.Surprise });
                    }
                    
                }
                catch (Exception ex)
                {

                }
            }

            double adjustedLatitude = 1.3450524;
            double adjustedLongitude = 103.8046703;

            if (imageUrl != null)
            {
                var random = new Random();

                adjustedLatitude = model.Latitude + (random.Next(0, 11) * 0.01);
                adjustedLongitude = model.Longitude + (random.Next(0, 6) * 0.01);

                var newPhoto = new Photo
                {
                    Url = imageUrl,
                    FacialExpressions = facialProperties,
                    Location = new Point(adjustedLatitude, adjustedLongitude),
                    CreatedBy = model.Name,
                    UpdatedBy = model.Name
                };

                await _repoPhotos.AddAsync(newPhoto);

                // *** For Cosmos DB only ***
                //await _repoPhotos.AddToCosmosDbAsync(newPhoto);
            }

            TempData["MyName"] = model.Name;

            TempData["NewImageUrl"] = imageUrl;
            TempData["NewLatitude"] = adjustedLatitude;
            TempData["NewLongitude"] = adjustedLongitude;

            TempData["NewEmotionAnger"]     = facialProperties.Where(fp => fp.Label == "Anger").Select(fp => fp.Value).Sum();
            TempData["NewEmotionContempt"]  = facialProperties.Where(fp => fp.Label == "Contempt").Select(fp => fp.Value).Sum();
            TempData["NewEmotionDisgust"]   = facialProperties.Where(fp => fp.Label == "Disgust").Select(fp => fp.Value).Sum();
            TempData["NewEmotionFear"]      = facialProperties.Where(fp => fp.Label == "Fear").Select(fp => fp.Value).Sum();
            TempData["NewEmotionHappiness"] = facialProperties.Where(fp => fp.Label == "Happiness").Select(fp => fp.Value).Sum();
            TempData["NewEmotionNeutral"]   = facialProperties.Where(fp => fp.Label == "Neutral").Select(fp => fp.Value).Sum();
            TempData["NewEmotionSadness"]   = facialProperties.Where(fp => fp.Label == "Sadness").Select(fp => fp.Value).Sum();
            TempData["NewEmotionSurprise"]  = facialProperties.Where(fp => fp.Label == "Surprise").Select(fp => fp.Value).Sum();

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


        private async Task<string> UploadFileToAzureStorageAsync(string storageConnectionString, string containerName, byte[] fileData, string fileName)
        {
            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);

            var blobClient = storageAccount.CreateCloudBlobClient();
            blobClient.DefaultRequestOptions.RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(3), 3);

            var blobContainer = blobClient.GetContainerReference(containerName);

            var fileBlob = blobContainer.GetBlockBlobReference(fileName);

            bool isExist = await fileBlob.ExistsAsync();

            if (isExist)
            {
                string newFileName = "";

                if (fileName.Contains("."))
                {
                    newFileName = fileName.Substring(0, fileName.LastIndexOf(".")) + "." +
                        DateTime.Now.ToString("yyyy-MM-dd-HHmmss") + "." +
                        fileName.Substring(fileName.LastIndexOf(".") + 1);
                }
                else
                {
                    newFileName = $"{ fileName }-{ DateTime.Now.ToString("yyyy-MM-dd-HHmmss") }";
                }

                fileBlob = blobContainer.GetBlockBlobReference(newFileName);
            }

            fileBlob.Properties.ContentType = GetFileContentType(fileName);

            if (fileBlob.Properties.ContentType == null)
            {
                return null;
            }

            await fileBlob.UploadFromByteArrayAsync(fileData, 0, fileData.Length);



            return fileBlob.Uri.ToString();
        }

        private string GetFileContentType(string fileName)
        {
            string fileType = fileName.Substring(fileName.LastIndexOf(".") + 1).ToUpper();

            foreach (FileTypeEnum type in Enum.GetValues(typeof(FileTypeEnum)))
            {
                if (fileType == type.ToString())
                {
                    return type.ToContentType();
                }
            }

            return null;
        }
    }
}
