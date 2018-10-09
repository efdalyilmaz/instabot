﻿using InstaSharper.API;
using InstaSharper.API.Builder;
using InstaSharper.Classes;
using InstaSharper.Classes.Models;
using InstaSharper.Logger;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace instabot
{
    public class Bot : IDisposable
    {
        private const int MaxRequestCount = 100;
        private const int DelayForWaitCount = 5;
        private const string FILE_REQUESTED = @"C:\Data\requested.txt";

        private static UserSessionData user;
        private static IInstaApi _instaApi;

        public Bot(string userName, string password)
        {
            user = new UserSessionData();
            user.UserName = userName;
            user.Password = password;

            _instaApi = InstaApiBuilder.CreateBuilder()
                                .SetUser(user)
                                .UseLogger(new DebugLogger(LogLevel.Exceptions))
                                .SetRequestDelay(RequestDelay.FromSeconds(3, 5))
                                .Build();


            Task login = Login();
            Task.WaitAny(login);
        }

        public async Task Login()
        {


            var loginRequest = await _instaApi.LoginAsync();
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
            await _instaApi.LogoutAsync();
        }

        public async Task PullUserPosts(string userName)
        {
            IResult<InstaUser> userInfo = await _instaApi.GetUserAsync(userName);
            Console.WriteLine(userInfo.Value.FullName);
            Console.WriteLine(userInfo.Value.IsPrivate);
            Console.WriteLine(userInfo.Value.FollowersCount);
            Console.WriteLine(userInfo.Value.FriendshipStatus.ToString());
            Console.WriteLine(userInfo.Value.SocialContext);
            Console.WriteLine(userInfo.Value.UnseenCount);

            IResult<InstaMediaList> media = await _instaApi.GetUserMediaAsync(userName, PaginationParameters.MaxPagesToLoad(5));
            InstaMediaList mediaList = media.Value;

            for (int i = 0; i < mediaList.Count; i++)
            {
                InstaMedia m = mediaList[i];
                if (m != null && m.Caption != null && !String.IsNullOrEmpty(m.Caption.Text) && m.MediaType == InstaMediaType.Image)
                {
                    for (int x = 0; x < m.Images.Count; x++)
                    {
                        if (m.Images[x] != null && m.Images[x].URI != null)
                        {
                            Console.WriteLine(m.Caption.Text);
                            Console.WriteLine(m.Images[x].URI);
                        }
                    }
                }
            }

        }

        public async Task PullUserInfo(string userName)
        {
            IResult<InstaUser> userInfo = await _instaApi.GetUserAsync(userName);
            writeAllProperties(userInfo);
        }

        public async Task<IResult<InstaUserShortList>> PullUsersFollowers(string userName)
        {
            IResult<InstaUserShortList> userShortList = await _instaApi.GetUserFollowersAsync(userName, PaginationParameters.MaxPagesToLoad(10));

            return userShortList;
        }

        public async Task<IResult<InstaUserShortList>> PullUsersFollowing(string userName)
        {
            IResult<InstaUserShortList> userShortList = await _instaApi.GetUserFollowingAsync(userName, PaginationParameters.MaxPagesToLoad(10));

            return userShortList;
        }

        public async Task MakeFollowRequestToPrivateAccount(string userName, bool toFollowers)
        {
            IResult<InstaUserShortList> userShortList;
            if (toFollowers)
            {
                userShortList = await PullUsersFollowers(userName);
            }
            else
            {
                userShortList = await PullUsersFollowing(userName);
            }

            List<InstaUserShort> privateUserList = userShortList.Value.FindAll(u => u.IsPrivate && u.ProfilePictureId != "unknown");
            List<long> requestedUser = readRequestedListFromFile();
            Console.WriteLine(String.Format("Firstly, Private User Count : {0}", privateUserList.Count));

            privateUserList.RemoveAll(item => requestedUser.Contains(item.Pk));
            
            if (privateUserList.Count > MaxRequestCount)
            {
                privateUserList = privateUserList.Take(MaxRequestCount).ToList();
            }

            Console.WriteLine(String.Format("Finally, Private User Count : {0}", privateUserList.Count));

            writeAllToRequestedFile(privateUserList);

            int wait = DelayForWaitCount;
            int requestCount = privateUserList.Count;
            foreach (var user in privateUserList)
            {
                wait--;
                requestCount --;
                if (wait == 0)
                {
                    wait = DelayForWaitCount;
                    await _instaApi.FollowUserAsync(user.Pk);
                }
                else
                {
                    _instaApi.FollowUserAsync(user.Pk);
                }

                Console.WriteLine(String.Format("Requested User : {0}, Remaining Count: {1}", user.FullName, requestCount));

            }

        }

        public async Task FollowUser(long userId)
        {
            await _instaApi.FollowUserAsync(userId);
        }

        public async Task UploadPhotoAsync(string fullpath, string caption)
        {
            var mediaImage = new InstaImage
            {
                Height = 1080,
                Width = 1080,
                URI = new Uri(Path.GetFullPath(fullpath), UriKind.Absolute).LocalPath
            };

            var result = await _instaApi.UploadPhotoAsync(mediaImage, caption);
            Console.WriteLine(result.Succeeded
                ? $"Media created: {result.Value.Pk}, {result.Value.Caption}"
                : $"Unable to upload photo: {result.Info.Message}");
        }

        private void waitForLogin()
        {
            int timeout = 30; // second
            while (!_instaApi.IsUserAuthenticated)
            {
                timeout--;
                if (timeout < 0)
                {
                    throw new TimeoutException("Login timeout");
                }

                Task.Delay(1000);
            }
        }

        private void writeAllProperties(object obj)
        {
            string sObj = JsonConvert.SerializeObject(obj);
            JObject parsed = JObject.Parse(sObj);
            foreach (var pair in parsed)
            {
                Console.WriteLine("{0}: {1}", pair.Key, pair.Value);
            }

        }

        private void writeAllToRequestedFile(List<InstaUserShort> privateUserList)
        {
            try
            {
                if (!File.Exists(FILE_REQUESTED))
                {
                    File.Create(FILE_REQUESTED);
                }

                using (StreamWriter w = File.AppendText(FILE_REQUESTED))
                {
                    foreach (var item in privateUserList)
                    {
                        w.WriteLine(item.Pk.ToString());
                    }
                   
                }
            }
            catch (Exception ex)
            {
               
            }
        }

        private List<long> readRequestedListFromFile()
        {
            List<long> requestedUser = new List<long>();
            try
            {
                if (!File.Exists(FILE_REQUESTED))
                {
                    return requestedUser;
                }

                string[] lines = File.ReadAllLines(FILE_REQUESTED);
                requestedUser = lines.Select(Int64.Parse).ToList();

                return requestedUser;
            }
            catch (Exception ex)
            {
                return requestedUser;
            }
        }

        public void Dispose()
        {
            Logout();
        }
    }
}
