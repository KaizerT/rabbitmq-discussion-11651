/////////////////////////////////////////////////////////////////////////////////
///         Confidential and Proprietary
///         Copyright 2024 Fresenius Kabi – All Rights Reserved
///         This software is considered a Trade Secret of Fresenius Kabi
/////////////////////////////////////////////////////////////////////////////////
using Rabbit.Test.Application.Models;

namespace Rabbit.Test.Application.Infrastructure.Services.Interfaces
{
    public interface IComponentInformationService
    {
        ComponentInformation GetComponentInformation();
    }
}
