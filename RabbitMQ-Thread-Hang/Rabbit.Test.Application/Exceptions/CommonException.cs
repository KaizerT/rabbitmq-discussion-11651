/////////////////////////////////////////////////////////////////////////////////
///         Confidential and Proprietary
///         Copyright 2024 Fresenius Kabi – All Rights Reserved
///         This software is considered a Trade Secret of Fresenius Kabi
/////////////////////////////////////////////////////////////////////////////////
using System;

namespace Rabbit.Test.Application.Exceptions
{
    public class CommonException : Exception
    {
        public CommonException()
        { }

        public CommonException(string message)
            : base(message)
        { }

        public CommonException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
