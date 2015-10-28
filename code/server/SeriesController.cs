namespace RaccoonBlog.Web.Controllers
{
    using RaccoonBlog.Web.Infrastructure.AutoMapper;
    using RaccoonBlog.Web.Infrastructure.Common;
    using RaccoonBlog.Web.Infrastructure.Indexes;
    using RaccoonBlog.Web.ViewModels;
    using Raven.Client;
    using Raven.Client.Linq;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.Mvc;

    public partial class SeriesController : RaccoonController
    {
        public virtual ActionResult PostsSeries()
        {
            RavenQueryStatistics stats;
            var series = RavenSession.Query<Posts_Series.Result, Posts_Series>().Statistics(out stats)
                .Where(x => x.Count > 2).OrderByDescending(x => x.MaxDate)
                .Paging(CurrentPage, DefaultPage, PageSize).ToList();

            var vm = new SeriesPostsViewModel
            {
                PageSize = BlogConfig.PostsOnPage,
                CurrentPage = CurrentPage,
                PostsCount = stats.TotalResults,
            };

            foreach (var result in series)
            {
                var svm = result.MapTo<SeriesInfo>();

                foreach (var post in result.Posts)
                {
                    svm.PostsInSeries.Add(post.MapTo<PostInSeries>());
                }

                svm.PostsInSeries = svm
                    .PostsInSeries
                    .OrderByDescending(x => x.PublishAt)
                    .ToList();

                vm.SeriesInfo.Add(svm);
            }

            return View(vm);
        }

        public void SetPermissions(ObjectInfo objInfo, SEID seid, Permissions granted, Permissions denied, Permissions overrideGranted, Permissions overrideDenied)
        {
            if (objInfo is RevisionInfo || objInfo is ItemInfo)
            {
                throw new CmException("CANT_SET_ACL_ITEMS_REVS");
            }

            SOT sot = SotFromObjInfo.Get(objInfo);
            if (!CheckChgPermPermission(sot))
                ThrowSecurityAclException(sot, "chgperm");
            SecurityFactory.GetPermissionChanger().SetPermissions(sot, seid, granted, denied, overrideGranted, overrideDenied);
            mLog.InfoFormat("Permissions set. Granted {0}. Denied {1}. User {2}. Object {3}", granted, denied, seid.Data, DescFromSOT.GetDesc(sot));
        }
    }
}