﻿using InstaBot.Logger;
using InstaBot.Models;
using InstaBot.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InstaBot.Filter
{
    public sealed class FollowerFilter : IFilter<UserInfo>
    {
        private bool? IsPrivate;
        private bool? IsKnownProfile;
        private bool IsCheckedRequested;
        private int MaxFollowerCount = ApiConstans.MAX_REQUEST_COUNT;
        private ILogger logger;

        public FollowerFilter(ILogger logger)
        {
            this.logger = logger;
        }

        public FollowerFilter SetPrivate(bool isPrivate)
        {
            IsPrivate = isPrivate;
            return this;
        }

        public FollowerFilter SetKnownProfile(bool isKnownProfile)
        {
            IsKnownProfile = isKnownProfile;
            return this;
        }

        public FollowerFilter SetMaxFollowerCount(int maxFollowerCount)
        {
            MaxFollowerCount = maxFollowerCount;
            return this;
        }

        public FollowerFilter CheckRequested()
        {
            IsCheckedRequested = true;
            return this;
        }

        public List<UserInfo> Apply(List<UserInfo> list)
        {
            logger.Write(String.Format("Firstly, Follower User Count : {0}", list.Count));

            List<UserInfo> filtered = list;
            if (IsPrivate != null)
            {
                filtered= filtered.FindAll(p => p.IsPrivate == IsPrivate.GetValueOrDefault());
                logger.Write(String.Format("IsPrivate Filter, Follower User Count : {0}", filtered.Count));
            }

            if (IsKnownProfile != null)
            {
                filtered = filtered.FindAll(p => p.HasProfilePicture == IsKnownProfile.GetValueOrDefault());
                logger.Write(String.Format("IsKnownProfile Filter, Follower User Count : {0}", filtered.Count));
            }

            if(IsCheckedRequested)
            {
                var requestedUser = FileUtils.ReadRequestedList();
                filtered.RemoveAll(item => requestedUser.Contains(item.Id));

                logger.Write(String.Format("CheckRequested Filter, Follower User Count : {0}", filtered.Count));
            }

            filtered = filtered.Take(MaxFollowerCount).ToList();
            logger.Write(String.Format("Finally, Follower User Count : {0}", filtered.Count));

            return filtered;
        }

        public static FollowerFilter DefaultFilter()
        {
            ILogger logger = new ConsoleLogger();
            FollowerFilter filter = FollowerFilter.CreateFilter(logger).
                SetPrivate(true).
                SetKnownProfile(true).
                CheckRequested();

            return filter;
        }

        public static FollowerFilter CreateFilter(ILogger logger)
        {
            return new FollowerFilter(logger);
        }


    }
}
