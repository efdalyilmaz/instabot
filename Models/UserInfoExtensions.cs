﻿using InstaSharper.Classes.Models;
using System.Collections.Generic;

namespace InstaBot.Models
{
    public static class UserInfoExtensions
    {
        public static UserInfo ToUserInfo(this InstaUserShort instaUser)
        {
            UserInfo userInfo = new UserInfo {
                Id = instaUser.Pk,
                UserName = instaUser.UserName,
                FullName = instaUser.FullName,
                IsPrivate = instaUser.IsPrivate,
                HasProfilePicture = instaUser.ProfilePictureId != ApiConstans.UNKNOWN
            };

            return userInfo;
        }

        public static UserInfo ToUserInfo(this InstaCurrentUser instaCurrentUser)
        {
            UserInfo userInfo = new UserInfo
            {
                Id = instaCurrentUser.Pk,
                UserName = instaCurrentUser.UserName,
                FullName = instaCurrentUser.FullName,
                IsPrivate = instaCurrentUser.IsPrivate,
                HasProfilePicture = instaCurrentUser.ProfilePictureId != ApiConstans.UNKNOWN
            };

            return userInfo;
        }

        public static UserInfo ToUserInfo(this InstaUser instaUser)
        {
            UserInfo userInfo = new UserInfo
            {
                Id = instaUser.Pk,
                UserName = instaUser.UserName,
                FullName = instaUser.FullName,
                IsPrivate = instaUser.IsPrivate,
                HasProfilePicture = instaUser.ProfilePictureId != ApiConstans.UNKNOWN
            };

            return userInfo;
        }

        public static List<UserInfo> ToUserInfoList(this List<InstaUserShort> instaUserList)
        {
            List<UserInfo> userInfoList = new List<UserInfo>();
            foreach (var item in instaUserList)
            {
                userInfoList.Add(item.ToUserInfo());
            }

            return userInfoList;
        }
    }
}
