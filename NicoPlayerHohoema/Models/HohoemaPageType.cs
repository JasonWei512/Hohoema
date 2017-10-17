﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	public enum HohoemaPageType
	{
		RankingCategoryList = 0,
		RankingCategory     = 1,
		UserMylist          = 2,
		Mylist              = 3,
		FollowManage,
		WatchHistory,

		Search,

        SearchResultCommunity,
        SearchResultTag,
        SearchResultKeyword,
        SearchResultMylist,
        SearchResultLive,

		FeedGroupManage,
		FeedGroup,
		FeedVideoList,

		UserInfo,
		UserVideo,

		Community,
		CommunityVideo,

        VideoInfomation,
        ConfirmWatchHurmfulVideo,

        CacheManagement,

        Settings,
        Login,

        Splash,
        VideoPlayer,


        NicoRepo,
    }
}
