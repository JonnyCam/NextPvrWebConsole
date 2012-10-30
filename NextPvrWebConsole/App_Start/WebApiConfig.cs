﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Security;
using System.Net.Http;
using System.Threading;
using System.Net;
using System.Text;
using System.Security.Principal;
using System.Web.Http.Hosting;
using System.Threading.Tasks;

namespace NextPvrWebConsole
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            //config.MessageHandlers.Add(new BasicAuthMessageHandler());
            

        }
    }

    public class BasicAuthMessageHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // check if already signed in
            if (WebMatrix.WebData.WebSecurity.IsAuthenticated)
                return base.SendAsync(request, cancellationToken);

            if (request.Headers.Authorization == null || request.Headers.Authorization.Scheme != "Basic")
            {
                return
                    Task<HttpResponseMessage>.Factory.StartNew(
                        () => new HttpResponseMessage(HttpStatusCode.Unauthorized));
            }
            var encoded = request.Headers.Authorization.Parameter;
            var encoding = Encoding.GetEncoding("iso-8859-1");
            var userPass = encoding.GetString(Convert.FromBase64String(encoded));
            int sep = userPass.IndexOf(':');
            var username = userPass.Substring(0, sep);
            var identity = new GenericIdentity(username, "Basic");
            //request.Properties.Add(HttpPropertyKeys.UserPrincipalKey, new GenericPrincipal(identity, new string[] { }));
            return base.SendAsync(request, cancellationToken);
        }

    }
}
