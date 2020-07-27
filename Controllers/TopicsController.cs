using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Api.Attributes;
using Nop.Plugin.Api.DTO.Errors;
using Nop.Plugin.Api.DTO.Topics;
using Nop.Plugin.Api.Helpers;
using Nop.Plugin.Api.JSON.Serializers;
using Nop.Plugin.Api.Services;
using Nop.Services.Customers;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Stores;
using Nop.Services.Topics;
using Nop.Plugin.Api.Infrastructure;
using Nop.Plugin.Api.JSON.ActionResults;
using Nop.Plugin.Api.Models.TopicsParameters;
using System.Linq;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Configuration;
using Nop.Core.Domain;
using Nop.Services.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Media;

namespace Nop.Plugin.Api.Controllers
{
    public class TopicsController : BaseApiController
    {
        private readonly ITopicApiService _topicApiService;
        private readonly ITopicService _topicService;
        private readonly IDTOHelper _dtoHelper;
        private readonly IStoreContext _storeContext;
        private readonly IUrlRecordService _urlRecordService;
        private readonly IStaticCacheManager _cacheManager;
        private readonly StoreInformationSettings _storeInformationSettings;
        private readonly IPictureService _pictureService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IWorkContext _workContext;
        private readonly MediaSettings _mediaSettings;

        public TopicsController(
            ITopicApiService topicApiService,
            IJsonFieldsSerializer jsonFieldsSerializer,
            ITopicService categoryService,
            IUrlRecordService urlRecordService,
            ICustomerActivityService customerActivityService,
            ILocalizationService localizationService,
            IPictureService pictureService,
            IStoreMappingService storeMappingService,
            IStoreService storeService,
            IDiscountService discountService,
            IAclService aclService,
            IStaticCacheManager cacheManager,
            ICustomerService customerService,
            IDTOHelper dtoHelper,
            IGenericAttributeService genericAttributeService,
            IStoreContext storeContext,
            IWorkContext workContext,
            MediaSettings mediaSettings,
            StoreInformationSettings storeInformationSettings) : base(jsonFieldsSerializer, aclService, customerService, storeMappingService, storeService, discountService,
                                         customerActivityService, localizationService, pictureService)
        {
            _topicApiService = topicApiService;
            _topicService = categoryService;
            _urlRecordService = urlRecordService;
            _dtoHelper = dtoHelper;
            _storeContext = storeContext;
            _cacheManager = cacheManager;
            _storeInformationSettings = storeInformationSettings;
            _pictureService = pictureService;
            _genericAttributeService = genericAttributeService;
            _workContext = workContext;
            _mediaSettings = mediaSettings;
        }

        /// <summary>
        ///     Receive a list of all Topics
        /// </summary>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        [HttpGet]
        [Route("/api/topics")]
        [ProducesResponseType(typeof(TopicsRootObject), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
        [GetRequestsErrorInterceptorActionFilter]
        public IActionResult GetCategories(TopicsParametersModel parameters)
        {
            if (parameters.Limit < Constants.Configurations.MinLimit || parameters.Limit > Constants.Configurations.MaxLimit)
            {
                return Error(HttpStatusCode.BadRequest, "limit", "Invalid limit parameter");
            }

            if (parameters.Page < Constants.Configurations.DefaultPageValue)
            {
                return Error(HttpStatusCode.BadRequest, "page", "Invalid page parameter");
            }

            var allTopics = _topicApiService.GetTopics(parameters.Ids, parameters.CreatedAtMin, parameters.CreatedAtMax,
                                                                  parameters.UpdatedAtMin, parameters.UpdatedAtMax,
                                                                  parameters.Limit, parameters.Page, parameters.SinceId)
                                                   .Where(c => StoreMappingService.Authorize(c));

            IList<TopicDto> topicsAsDtos = allTopics.Select(topic => _dtoHelper.PrepareTopicToDTO(topic)).ToList();

            var topicsRootObject = new TopicsRootObject
            {
                Topics = topicsAsDtos
            };

            var json = JsonFieldsSerializer.Serialize(topicsRootObject, parameters.Fields);

            return new RawJsonActionResult(json);
        }


        // get logos
        [HttpGet]
        [Route("/api/logos")]
        [ProducesResponseType(typeof(LogoModel), (int)HttpStatusCode.OK)]
        [GetRequestsErrorInterceptorActionFilter]
        public IActionResult GetLogos()
        {
            var logoPictureId = _storeInformationSettings.LogoPictureId;
            var model = new LogoModel
            {
                StoreName = LocalizationService.GetLocalized(_storeContext.CurrentStore, x => x.Name),
                LogoPath = _pictureService.GetPictureUrl(logoPictureId, showDefaultPicture: false)
            };
            
            return Ok(model);
        }

        [HttpGet]
        [Route("/api/avatar")]
        [ProducesResponseType(typeof(CustomerAvatarModel), (int)HttpStatusCode.OK)]
        [GetRequestsErrorInterceptorActionFilter]
        public IActionResult GetAvatar()
        {
            var customer = _workContext.CurrentCustomer;
            var model = new CustomerAvatarModel { };
            var customerAvatar = _pictureService.GetPictureById(_genericAttributeService.GetAttribute<int>(customer, NopCustomerDefaults.AvatarPictureIdAttribute));
            var customerAvatarId = 0;

            if (customerAvatar != null)
                customerAvatarId = customerAvatar.Id;

            _genericAttributeService.SaveAttribute(customer, NopCustomerDefaults.AvatarPictureIdAttribute, customerAvatarId);

            model.AvatarUrl = _pictureService.GetPictureUrl(
                        _genericAttributeService.GetAttribute<int>(customer, NopCustomerDefaults.AvatarPictureIdAttribute),
                        _mediaSettings.AvatarPictureSize,
                        false);

            var a = model.AvatarUrl;

            return Ok(a);
        }
    }
}
