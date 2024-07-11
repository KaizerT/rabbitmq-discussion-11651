/////////////////////////////////////////////////////////////////////////////////
using Microsoft.Extensions.Configuration;
///         Confidential and Proprietary
///         Copyright 2024 Fresenius Kabi – All Rights Reserved
///         This software is considered a Trade Secret of Fresenius Kabi
/////////////////////////////////////////////////////////////////////////////////
using Rabbit.Test.Application.Infrastructure.Services.Interfaces;
using Rabbit.Test.Application.Models;
using System;

namespace Rabbit.Test.Application.Infrastructure.Services
{
    public class ComponentInformationService : IComponentInformationService
    {
        IConfiguration _config;
        public ComponentInformationService(IConfiguration config)
        {
            _config = config;
        }
        /// <summary>
        /// Retrieves the assembly info and service name of the current application for version reporting
        /// </summary>
        /// <returns></returns>
        public ComponentInformation GetComponentInformation()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            Version version = assembly.GetName().Version;

            var componentInformation = new ComponentInformation
            {
                Name = _config["ServiceName"],
                Version = version.ToString()
            };

            return componentInformation;
        }
    }
}
