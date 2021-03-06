﻿using InstaBot.Filter;
using InstaBot.Logger;
using InstaBot.Models;
using InstaBot.Services;
using InstaBot.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InstaBot
{
    internal class Api : IApi
    {
        private readonly IInstaService instaService;
        private readonly IStockService stockService;
        private readonly ILogger logger;

        public Api(ILogger logger, string userName, string password)
        {
            this.logger = logger;
            instaService = new InstaService(logger, userName, password);
        }

        public Api(ILogger logger, string userName, string password, string applicationId, string secretKey) : this(logger, userName, password)
        {
            stockService = new StockService(applicationId, secretKey);
        }

        public async Task LikeMediaAsync(string hashtag)
        {
            List<Media> mediaList = await instaService.GetTagFeedAsync(hashtag);
            mediaList = mediaList.FindAll(m => m.LikesCount > ApiConstans.MIN_LIKES_COUNT).Take(ApiConstans.MAX_REQUEST_COUNT).ToList();

            for (int i = 0; i < mediaList.Count; i++)
            {
                instaService.LikeMediaAsync(mediaList[i].Id);
                await Task.Delay(ApiConstans.DELAY_TIME);
                logger.Write($"Liked Media User: {mediaList[i].User.UserName}, Remaining Media {mediaList.Count - i - 1}");
            }

        }

        public async Task MakeFollowRequestAsync(string userName, IFilter<UserInfo> filter = null)
        {
            Random rnd = new Random();
            List<UserInfo> userInfoList = await instaService.GetUserFollowers(userName, 20);
            filter = filter ?? FollowerFilter.DefaultFilter();

            var filtered = filter.Apply(userInfoList);
            for (int i = 0; i < filtered.Count; i++)
            {
                instaService.FollowUserAsync(filtered[i].Id);
                await Task.Delay(rnd.Next(ApiConstans.DELAY_TIME_MIN, ApiConstans.DELAY_TIME_MAX));
                logger.Write($"Requested UserName : {filtered[i].UserName}, Remaining User {filtered.Count - i - 1}");
            }

            FileUtils.WriteAllToRequestedFile(filtered);
        }

        public async Task MakeAllFollowingsFollowersFollowRequestAsync(int top = 1000, IFilter<UserInfo> filter = null)
        {
            List<UserInfo> currentUserFollowingList = await instaService.GetCurrentUserFollowings();
            List<UserInfo> requestList = new List<UserInfo>();
            RandomGenerator random = new RandomGenerator(currentUserFollowingList.Count);

            for (int i = 0; i < currentUserFollowingList.Count; i++)
            {
                int index = random.Different();
                var following = currentUserFollowingList[index];
                logger.Write($"Random UserName : {following.UserName}, Index Order {i}");
                List<UserInfo> userInfoList = await instaService.GetUserFollowers(following.UserName, 10);
                filter = filter ?? FollowerFilter.DefaultFilter();


                var filtered = filter.Apply(userInfoList);
                filtered.RemoveAll(u => currentUserFollowingList.Exists(c => c.Id == u.Id));
                if (filtered != null && filtered.Count > 0)
                {
                    requestList.AddRange(filtered);
                }

                if (requestList.Count >= top)
                {
                    requestList = requestList.Take(top).ToList();
                    break;
                }
            }

            int requestIndex = 0;
            try
            {
                for (requestIndex = 0; requestIndex < requestList.Count; requestIndex++)
                {
                    logger.Write($"Requested UserName : {requestList[requestIndex].UserName}, Remaining User {requestList.Count - requestIndex - 1}");
                    await Retry.DoAsync(() => instaService.FollowUserAsync(requestList[requestIndex].Id), TimeSpan.FromSeconds(3));
                }
            }
            catch (Exception ex)
            {
                logger.Write(ex.ToString());
            }
            finally
            {
                if (requestIndex > 0)
                {
                    FileUtils.WriteAllToRequestedFile(requestList.Take(requestIndex).ToList());
                }
            }
        }

        public async Task UploadPhotoAsync(string stockCategoryName, int photoCount, IDownloadService downloadService)
        {
            List<string> downloadedPhotos = downloadService.GetAllDownloadedPhotoNames();
            List<Photo> photoList = await stockService.SearchNewPhotosAsync(stockCategoryName, photoCount, downloadedPhotos);
            await downloadService.DownloadAllPhotosAsync(photoList);
            downloadService.WriteDownloadedPhotoNames(photoList);

            int uploadedPhoto = 1;
            logger.Write(String.Format("Downloaded photo count {0}", photoList.Count));
            foreach (var photo in photoList)
            {
                string filePath = FileUtils.GetFullFilePath(downloadService.FullDirectory, photo.Id, ApiConstans.PHOTO_EXTENSION);
                await instaService.UploadPhotoAsync(filePath, photo.GetCaption());

                logger.Write(String.Format("{0}. uploaded. PhotoId : {1} ", uploadedPhoto, photo.Id));
                uploadedPhoto++;

            }
        }
    }
}