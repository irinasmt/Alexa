using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using AlexaRules.Helpers;

namespace AlexaRules.Controllers
{
    public class CSharpController : ApiController
    {
        
        private const string filePath = @"C:\Questions.json";

        [HttpPost, Route("api/alexa")]
        public dynamic Index(AlexaRequest alexaRequest)
        {

            if (alexaRequest.Session.Application.ApplicationId != ApplicationID)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest));
            }

            var alexa = new BaseAlexa(filePath);
            return alexa.Index(alexaRequest);
        }
    }
}
