﻿using InstaSharper.API;
using InstaSharper.API.Builder;
using InstaSharper.Classes;
using InstaSharper.Classes.Android.DeviceInfo;
using InstaSharper.Classes.Models;
using InstaSharper.Classes.ResponseWrappers;
using InstaSharper.Classes.ResponseWrappers.BaseResponse;
using InstaSharper.Logger;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Unsplasharp;
using Unsplasharp.Models;
using InstaBot.Utils;

namespace InstaBot.API
{
    internal class Api : IApi
    {
        private readonly IInstaApi instaClient;
        private readonly UnsplasharpClient stockClient;

        public Api(string userName, string password)
        {
            UserSessionData user = new UserSessionData();
            user.UserName = userName;
            user.Password = password;

            instaClient = InstaApiBuilder.CreateBuilder()
                    .SetUser(user)
                    .UseLogger(new DebugLogger(LogLevel.Exceptions))
                    .SetRequestDelay(RequestDelay.FromSeconds(3, 5))
                    .Build();
        }

        public Api(string userName, string password, string applicationId, string secretKey) : this(userName, password)
        {
            stockClient = new UnsplasharpClient(applicationId, secretKey);
        }

        public async Task Login()
        {
            var loginRequest = await instaClient.LoginAsync();
            if (loginRequest.Succeeded)
            {
                Console.WriteLine("Success");
            }
            else
            {
                Console.WriteLine(loginRequest.Info.Message);
            }
        }

        public async Task Logout()
        {
            await instaClient.LogoutAsync();
        }

        public async Task MakeFollowRequestAsync(string userName)
        {
            validateInstaClient();
            validateLoggedIn();

            IResult<InstaUserShortList> userShortList = await instaClient.GetUserFollowersAsync(userName, PaginationParameters.Empty);

            foreach (var user in userShortList.Value)
            {
                await instaClient.FollowUserAsync(user.Pk);
            }

        }

        public async Task UploadPhotoAsync(string stockCategoryName)
        {
            validateInstaClient();
            validateLoggedIn();
            validateStockClient();

            List<Photo> photoList = await stockClient.SearchPhotos(stockCategoryName, 1, 30);
            FileUtils.DownloadAllPhotos(photoList);

            int uploadedPhoto = 1;
            Console.WriteLine(String.Format("Downloaded photo count {0}", FileUtils.ListOfDownloadedPhoto));
            foreach (var photo in FileUtils.ListOfDownloadedPhoto)
            {

                const string imagesSubdirectory = @"D:\unsplash\holiday";
                string filePath = imagesSubdirectory + @"\" + photo.Id + ".jpg";

                string caption = photo.Description + " \r\n \r\n";
                caption += "Thanx to " + photo.User.Name + " \r\n\r\n\r\n";
                caption += "#holiday #travel #trip \r\n";

                await uploadPhotoAsync(filePath, caption);

                Console.WriteLine(String.Format("{0}. uploaded. PhotoId : {1} ", uploadedPhoto, photo.Id));
                uploadedPhoto++;

            }
        }

        private async Task uploadPhotoAsync(string fullpath, string caption)
        {
            var mediaImage = new InstaImage
            {
                Height = 1080,
                Width = 1080,
                URI = new Uri(Path.GetFullPath(fullpath), UriKind.Absolute).LocalPath
            };

            var result = await instaClient.UploadPhotoAsync(mediaImage, caption);
            Console.WriteLine(result.Succeeded
                ? $"Media created: {result.Value.Pk}, {result.Value.Caption}"
                : $"Unable to upload photo: {result.Info.Message}");
        }

        #region private part

        private void validateLoggedIn()
        {
            if (!instaClient.IsUserAuthenticated)
                throw new ArgumentException("User must be authenticated");
        }

        private void validateInstaClient()
        {
            if (instaClient == null)
                throw new ArgumentException("Insta Client is null");
        }

        private void validateStockClient()
        {
            if (stockClient == null)
                throw new ArgumentException("Stock Client is null");
        }

        #endregion
    }
}