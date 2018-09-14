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

            return View();
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
                    FacialExpressions = new List<FacialExpression>(),
                    Location = new Point(adjustedLatitude, adjustedLongitude),
                    CreatedBy = "",
                    UpdatedBy = ""
                };

                await _repoPhotos.AddAsync(newPhoto);

                // *** For Cosmos DB only ***
                //await _repoPhotos.AddToCosmosDbAsync(newPhoto);
            }

            TempData["NewImageUrl"] = imageUrl;
            TempData["NewLatitude"] = adjustedLatitude;
            TempData["NewLongitude"] = adjustedLongitude;

            return RedirectToAction("Index");
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
