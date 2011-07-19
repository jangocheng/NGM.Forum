﻿using System.Web.Mvc;
using NGM.Forum.Extensions;
using NGM.Forum.Models;
using Orchard;
using Orchard.ContentManagement;
using Orchard.DisplayManagement;
using Orchard.Localization;
using Orchard.Themes;
using Orchard.UI.Notify;

namespace NGM.Forum.Controllers {
    [Themed]
    [ValidateInput(false)]
    public class PostController : Controller, IUpdateModel {
        private readonly IOrchardServices _orchardServices;

        public PostController(IOrchardServices orchardServices, 
            IShapeFactory shapeFactory) {
            _orchardServices = orchardServices;

            T = NullLocalizer.Instance;
            Shape = shapeFactory;
        }

        dynamic Shape { get; set; }
        public Localizer T { get; set; }

        public ActionResult Create(int contentId) {
            if (IsNotAllowedToCreatePost())
                return new HttpUnauthorizedResult();

            var contentItem = _orchardServices.ContentManager.Get(contentId, VersionOptions.Latest);
            if (contentItem.As<PostPart>() == null && contentItem.As<ThreadPart>() == null) {
                return HttpNotFound();
            }

            var part = _orchardServices.ContentManager.New<PostPart>(ContentPartConstants.Post);

            dynamic model = _orchardServices.ContentManager.BuildEditor(part);
            
            return View((object)model);
        }

        [HttpPost, ActionName("Create")]
        public ActionResult CreatePOST(int contentId) {
            if (IsNotAllowedToCreatePost())
                return new HttpUnauthorizedResult();

            var contentItem = _orchardServices.ContentManager.Get(contentId, VersionOptions.Latest);
            if (contentItem.As<PostPart>() == null && contentItem.As<ThreadPart>() == null) {
                return HttpNotFound();
            }

            var post = _orchardServices.ContentManager.New<PostPart>(ContentPartConstants.Post);
            if (contentItem.As<PostPart>() != null) {
                post.ThreadPart = contentItem.As<PostPart>().ThreadPart;
                post.ParentPostId = contentItem.As<PostPart>().Id;
            }
            else {
                post.ThreadPart = contentItem.As<ThreadPart>();
            }

            _orchardServices.ContentManager.Create(post, VersionOptions.Draft);
            var model = _orchardServices.ContentManager.UpdateEditor(post, this);

            if (!ModelState.IsValid) {
                _orchardServices.TransactionManager.Cancel();
                return View((object)model);
            }

            _orchardServices.ContentManager.Publish(post.ContentItem);

            _orchardServices.Notifier.Information(T("Your {0} has been created.", post.TypeDefinition.DisplayName));
            return Redirect(Url.ViewThread(post.ThreadPart));
        }

        bool IUpdateModel.TryUpdateModel<TModel>(TModel model, string prefix, string[] includeProperties, string[] excludeProperties) {
            return TryUpdateModel(model, prefix, includeProperties, excludeProperties);
        }

        void IUpdateModel.AddModelError(string key, LocalizedString errorMessage) {
            ModelState.AddModelError(key, errorMessage.ToString());
        }

        private bool IsNotAllowedToCreatePost() {
            return !_orchardServices.Authorizer.Authorize(Permissions.AddPost, T("Not allowed to create post"));
        }
    }
}