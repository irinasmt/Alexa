using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using AlexaRules.Helpers;

namespace AlexaRules.Controllers
{
    public class RubyController : ApiController
    {
        private const string ApplicationID = "amzn1.ask.skill.c8bb7c6b-b5e9-435e-9042-0e9185321836";
        private const string filePath = @"C:\RubyQuestions.json";

        [HttpPost, Route("api/alexa/ruby")]
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