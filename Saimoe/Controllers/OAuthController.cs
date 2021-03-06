﻿/**
 * @file: Controllers/OAuthController.cs
 * @author Korepwx <public@korepwx.com>.
 * The OAuth 2.0 Controller for user verification via Google Service.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;

namespace Saimoe.Controllers
{
    /// <summary>
    /// The OAuth 2.0 AppKey Settings.
    /// </summary>
    public class OAuthSettings
    {
        // TODO: Please provide formal OAuth 2.0 AppKey and Redirect Url!
        public static readonly string RedirectUrl = "http://localhost/saimoe/oauth2callback";
        public static readonly string ClientID = "136777643268.apps.googleusercontent.com";
        public static readonly string ClientSecret = "-3GRfU45EoKsH2zftBQWjhLZ";
        public static readonly string ApiKey = "AIzaSyCWL9iC3r2BFjWBin70cxQWBtJmyvhEhRw";
        public static readonly string EmailAddress = "136777643268@developer.gserviceaccount.com";
    }

    /// <summary>
    /// The OAuth 2.0 login controller.
    /// Reference: http://www.cnblogs.com/dudu/archive/2012/04/30/asp_net_mvc_google_oauth_api.html.
    /// </summary>
    public class OAuthController : Controller
    {
        public ActionResult GoogleLogin()
        {
            var url = "https://accounts.google.com/o/oauth2/auth?" +
                "scope={0}&state={1}&redirect_uri={2}&response_type=code&client_id={3}&approval_prompt=auto";

            var scope = string.Join("+", new string[] {
                //HttpUtility.UrlEncode("https://www.googleapis.com/auth/userinfo.email"),
                HttpUtility.UrlEncode("https://www.googleapis.com/auth/userinfo.profile")
            });
            var state = "/profile";

            var redirectUri = HttpUtility.UrlEncode(OAuthSettings.RedirectUrl);
            var cilentId = HttpUtility.UrlEncode(OAuthSettings.ClientID);

            return Redirect(string.Format(url, scope, state, redirectUri, cilentId));
        }

        public ActionResult GoogleCallback()
        {
            // 由于是https，这里必须要转换为HttpWebRequest
            var webRequest = WebRequest.Create("https://accounts.google.com/o/oauth2/token") as HttpWebRequest;
            webRequest.Method = "POST";
            webRequest.ContentType = "application/x-www-form-urlencoded";

            // 参考https://developers.google.com/accounts/docs/OAuth2WebServer
            var postData = string.Format("code={0}&client_id={1}&client_secret={2}&redirect_uri={3}" +
                "&grant_type=authorization_code",
                Request.QueryString["code"],
                    OAuthSettings.ClientID,
                    OAuthSettings.ClientSecret,
                    OAuthSettings.RedirectUrl);

            // 在HTTP POST请求中传递参数
            using (var sw = new StreamWriter(webRequest.GetRequestStream()))
            {
                sw.Write(postData);
            }

            // 发送请求，并获取服务器响应
            var responseJson = "";
            using (var response = webRequest.GetResponse())
            {
                using (var sr = new StreamReader(response.GetResponseStream()))
                {
                    responseJson = sr.ReadToEnd();
                }
            }

            // 通过Json.NET对服务器返回的json字符串进行反序列化，得到access_token
            var accessToken = JsonConvert.DeserializeAnonymousType(responseJson, new { access_token = "" }).access_token;

            // 通过 AccessToken 拿到用户信息
            webRequest = WebRequest.Create("https://www.googleapis.com/oauth2/v1/userinfo") as HttpWebRequest;
            webRequest.Method = "GET";
            webRequest.Headers.Add("Authorization", "Bearer " + accessToken);

            using (var response = webRequest.GetResponse())
            {
                using (var sr = new StreamReader(response.GetResponseStream()))
                {
                    responseJson = sr.ReadToEnd();
                }
            }

            // 取得用户的 Profile 数据。
            var profile = JsonConvert.DeserializeAnonymousType(responseJson, new
            {
                Id = "",        // The Google+ Profile ID
                Name = "",      // The Google+ FullName.
                Link = "",      // The Google+ Profile URI.
                Picture = "",   // The Google+ Avatar URI.
                Gender = "",    // The Google+ User Gender. (e.g. male)
                Locale = ""     // The Google+ Language. (e.g. zh-CN)
            });

            // TODO: Please store the profile into Database!

            return Content(HttpUtility.HtmlEncode(profile.ToString()));
        }
    }
}
