﻿using System;
using System.Collections.Generic;
using System.Text;

namespace InstaBot.Models
{
    public static class PhotoExtensions
    {
        public static Photo ToPhoto(this Unsplasharp.Models.Photo photo)
        {
            return new Photo
            {
                Id = photo.Id,
                Description = photo.Description,
                DownloadLink = photo.Links.Download,
                Location = photo.Location != null ? photo.Location.ToLocation() : new Location(),
                User = new UserInfo
                {
                    Name = photo.User != null ? photo.User.Name : String.Empty
                }
            };
        }

        public static List<Photo> ToPhotoList(this List<Unsplasharp.Models.Photo> uPhotoList)
        {
            List<Photo> photoList = new List<Photo>();
            foreach (var item in uPhotoList)
            {
                photoList.Add(item.ToPhoto());
            }
            return photoList;
        }
    }
}
