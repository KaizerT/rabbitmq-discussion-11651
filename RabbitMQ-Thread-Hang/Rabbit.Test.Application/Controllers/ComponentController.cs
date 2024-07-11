/////////////////////////////////////////////////////////////////////////////////
using Microsoft.AspNetCore.Mvc;
///         Confidential and Proprietary
///         Copyright 2024 Fresenius Kabi – All Rights Reserved
///         This software is considered a Trade Secret of Fresenius Kabi
/////////////////////////////////////////////////////////////////////////////////
using Rabbit.Test.Application.Infrastructure.Services.Interfaces;
using System.Net;

namespace Rabbit.Test.Application.Controllers
{
    [Route("api/component")]
    [ApiController]
    public class ComponentController : ControllerBase
    {
        private readonly IComponentInformationService _componentInformationService;

        public ComponentController(IComponentInformationService componentInformationService)
        {
            _componentInformationService = componentInformationService;
        }

        /// <summary>
        /// get service name and version
        /// </summary>
        /// <returns></returns>
        [Route("version")]
        [HttpGet]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public IActionResult GetServiceComponentInformation()
        {
            var result = _componentInformationService.GetComponentInformation();

            return result != null ? Ok(result) : StatusCode(500);
        }
    }
}
