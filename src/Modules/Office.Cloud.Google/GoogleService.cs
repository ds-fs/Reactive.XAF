﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Threading;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Model.Core;
using Fasterflect;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Auth.OAuth2.Web;
using Google.Apis.Requests;
using Google.Apis.Services;

using Xpand.Extensions.EventArgExtensions;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Tracing;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.SecurityExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Office.Cloud.Google.BusinessObjects;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Platform = Xpand.Extensions.XAF.XafApplicationExtensions.Platform;

namespace Xpand.XAF.Modules.Office.Cloud.Google{

    public static class GoogleService{
        
        public static IObservable<(Frame frame, UserCredential userCredential)> AuthorizeGoogle(this IObservable<Frame> source) 
            => source.SelectMany(frame => Observable.Defer(() => frame.Application.GoogleNeedsAuthentication().WhenDefault()
                .SelectMany(_ => frame.View.AsObjectView().Application().AuthorizeGoogle()
                    .Select(userCredential => (frame, userCredential)))).ObserveOn(SynchronizationContext.Current));

        public static IObservable<bool> GoogleNeedsAuthentication(this XafApplication application) 
            => application.NeedsAuthentication<GoogleAuthentication>(() => Observable.Using(application.GoogleAuthorizationCodeFlow,
                flow => Observable.FromAsync(() => flow.LoadTokenAsync(application.CurrentUserId().ToString(), CancellationToken.None))
                    .Select(response => response)
                    .WhenNotDefault().RefreshToken(flow, application.CurrentUserId().ToString()))
                .SwitchIfEmpty(true.Observe()))
                .Publish().RefCount()
                .TraceGoogleModule();

        
        public static SimpleAction ConnectGoogle(this (GoogleModule googleModule, Frame frame) tuple) 
            => tuple.frame.Action(nameof(ConnectGoogle)).As<SimpleAction>();

        
        public static SimpleAction DisconnectGoogle(this (GoogleModule googleModule, Frame frame) tuple) 
            => tuple.frame.Action(nameof(DisconnectGoogle)).As<SimpleAction>();


        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager) 
            => manager.Connect("Google", typeof(GoogleAuthentication), application
                => application.GoogleNeedsAuthentication(), application
                => application.AuthorizeGoogle((_, flow) => flow.AuthorizeApp(application)).ToUnit())
                .Merge(manager.ExchangeCodeForToken())
                .Merge(Observable.If(() => DesignerOnlyCalculator.IsRunTime,manager.Defer(() => manager.CheckBlazor("Xpand.Extensions.Office.Cloud.Google.Blazor.GoogleCodeStateStartup", "Xpand.Extensions.Office.Cloud.Google.Blazor"))))
                ;

        internal static IObservable<TSource> TraceGoogleModule<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<ITraceEvent> traceAction = null,
	        Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.OnNextOrOnError,Func<string> allMessageFactory = null,
	        [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) 
            => source.Trace(name, GoogleModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy,allMessageFactory, memberName,sourceFilePath,sourceLineNumber);
        
        private static IObservable<bool> RefreshToken(this IObservable<TokenResponse> source, GoogleAuthorizationCodeFlow flow, string userId) 
            => source.SelectMany(response 
                => response.IsExpired(flow.Clock) ? flow.RefreshTokenAsync(userId, response.RefreshToken, CancellationToken.None).ToObservable().Select(tokenResponse 
                    => tokenResponse.IsExpired(flow.Clock)) : false.Observe())
                .TraceGoogleModule();


        public class AuthorizationCodeFlow : GoogleAuthorizationCodeFlow{
	        public AuthorizationCodeFlow(Initializer initializer) : base(initializer){
	        }

	        public override AuthorizationCodeRequestUrl CreateAuthorizationCodeRequest(string redirectUri) 
                => new GoogleAuthorizationCodeRequestUrl(new Uri(AuthorizationServerUrl)) {
			        ClientId = ClientSecrets.ClientId,
			        Scope = string.Join(" ", Scopes),
			        RedirectUri = redirectUri,
			        AccessType = "offline",Prompt = Prompt
		        };
        };

        public static AuthorizationCodeFlow GoogleAuthorizationCodeFlow(this XafApplication application)
            => new(new GoogleAuthorizationCodeFlow.Initializer{
                ClientSecrets = application.NewClientSecrets(), Scopes = application.Model.OAuthGoogle().Scopes(),
                DataStore = application.NewXafOAuthDataStore(),Prompt = application.Model.OAuthGoogle().Prompt.ToString().ToLower()
            });


        static ClientSecrets NewClientSecrets(this XafApplication application){
            var oAuth = application.Model.OAuthGoogle();
            return new ClientSecrets(){ClientId = oAuth.ClientId,ClientSecret = oAuth.ClientSecret};
        }

        static IObservable<Unit> ExchangeCodeForToken(this ApplicationModulesManager manager) 
            => manager.WhenApplication(application => application.WhenWindowCreated().When(TemplateContext.ApplicationWindow)
                .SelectMany(window => window.Application.WhenWeb()
                    .SelectMany(api => {
                        var code = api.Application.GetPlatform() != Platform.Web ? api.Application.GetCodeFromObject()
                            : (string) api.HttpContext()?.GetPropertyValue("Request")?.GetIndexer("code");
                        return code != null ? window.ExchangeCodeForToken(code, api).TraceGoogleModule(response => $"IdToken={response.IdToken}").To((default(TokenResponse)))
                            : Observable.Empty<TokenResponse>();
                    }))).ToUnit();

        private static IObservable<TokenResponse> ExchangeCodeForToken(this Window window, string code, IXAFAppWebAPI api){
            var requestUri = api.GetRequestUri();
            return window.Application.GetPlatform() == Platform.Blazor ? window.ExchangeBlazorCodeForToken( code, requestUri) : api.ExchangeWebFormsCodeForToken(window, code,  requestUri);
        }

        private static IObservable<TokenResponse> ExchangeBlazorCodeForToken(this Window window, string code, Uri requestUri) {
            var uri = requestUri.GetLeftPart(UriPartial.Authority);
            return window.Application.GoogleAuthorizationCodeFlow()
                .ExchangeCodeForTokenAsync(window.Application.CurrentUserId().ToString(), code, uri,                    CancellationToken.None).ToObservable();
        }

        private static IObservable<TokenResponse> ExchangeWebFormsCodeForToken(this IXAFAppWebAPI api,Window window, string code,  Uri requestUri){
            var state = api.HttpContext().GetPropertyValue("Request").GetIndexer("state").ToString();
            return Observable.FromAsync(() => window.Application.GoogleAuthorizationCodeFlow().ExchangeCodeForTokenAsync(
                    window.Application.CurrentUserId().ToString(), code,
                    requestUri.ToString().Substring(0, requestUri.ToString().IndexOf("?", StringComparison.Ordinal)),
                    CancellationToken.None))
                .ObserveOn(SynchronizationContext.Current)
                .TraceGoogleModule(response => $"IdToken={response.IdToken}")
                .Do(_ => api.Redirect(state.Substring(0, state.Length - AuthorizationCodeWebApp.StateRandomLength), false))
                .To((default(TokenResponse)));
        }


        static string GetCodeFromObject(this XafApplication application){
            var service = (ConcurrentDictionary<object,object>)application.WhenWeb().Wait().GetService("Xpand.Extensions.Blazor.SingletonItems");
            service.TryRemove(application.CurrentUserId(), out var value);
            return (string) value;
        }

        
        public static IObservable<T> NewService<T>(this IObservable<UserCredential> source) where T : BaseClientService 
            => source.Select(NewService<T>);

        public static T NewService<T>(this UserCredential credential) where T : BaseClientService 
            => (T) typeof(T).CreateInstance(new BaseClientService.Initializer(){HttpClientInitializer = credential});

        public static IObservable<UserCredential> AuthorizeGoogle(this XafApplication application,Func<UserFriendlyException,IAuthorizationCodeFlow,  IObservable<UserCredential>> acquireToken = null) 
            => application.GoogleAuthorizationCodeFlow().AuthorizeGoogle(application,acquireToken)
                .TraceGoogleModule(credential => credential.UserId);

        
        public static IObservable<TResponse> ToObservable<TResponse>(this ClientServiceRequest request) => ((IClientServiceRequest<TResponse>)request).ToObservable();

        public static IObservable<TResponse> ToObservable<TResponse>(this IClientServiceRequest<TResponse> request) => Observable.FromAsync(() => request.ExecuteAsync());

        static readonly Subject<GenericEventArgs<Func<XafApplication,XafOAuthDataStore>>> CustomizeOathDataStoreSubject=new();
        
        public static IObservable<GenericEventArgs<Func<XafApplication, XafOAuthDataStore>>> CustomizeOathDataStore => CustomizeOathDataStoreSubject.AsObservable();

        static XafOAuthDataStore NewXafOAuthDataStore(this XafApplication application){
            var cloudTypes = XafTypesInfo.Instance.PersistentTypes.Where(info => info.Type.Namespace == typeof(CloudOfficeObject).Namespace)
                .Select(info => info.Type);
            application.Security.AddAnonymousType(cloudTypes.ToArray());
            var args = new GenericEventArgs<Func<XafApplication, XafOAuthDataStore>>();
            CustomizeOathDataStoreSubject.OnNext(args);
            return args.Handled ? args.Instance(application) : new XafOAuthDataStore(application.CreateObjectSpace, application.CurrentUserId(),application.GetPlatform());
        }
        
        
        public static IObservable<GenericEventArgs<IObservable<UserCredential>>> CustomAcquireTokenInteractively 
            => CustomAcquireTokenInteractivelySubject.AsObservable();

        static readonly Subject<GenericEventArgs<IObservable<UserCredential>>> CustomAcquireTokenInteractivelySubject=new();

        static IObservable<UserCredential> AuthorizeGoogle(this GoogleAuthorizationCodeFlow flow, XafApplication application,
	        Func<UserFriendlyException, IAuthorizationCodeFlow, IObservable<UserCredential>> acquireToken) 
	        => flow.RequestAuthorizationCode(application,acquireToken);

        private static IObservable<UserCredential> AuthorizeApp(this IAuthorizationCodeFlow flow, XafApplication application) 
	        => Observable.Defer(() => ((XafOAuthDataStore) flow.DataStore).Platform == Platform.Win
			        ? flow.AuthorizeInstalledApp(application) : flow.AuthorizeWebApp(application));

        private static IObservable<UserCredential> AuthorizeInstalledApp(this IAuthorizationCodeFlow flow, XafApplication application) 
            => Observable.FromAsync(() => new AuthorizationCodeInstalledApp(flow, new LocalServerCodeReceiver()).AuthorizeAsync(application.CurrentUserId().ToString(), CancellationToken.None));

        private static IObservable<UserCredential> AuthorizeWebApp(this  IAuthorizationCodeFlow flow,XafApplication application) 
            => application.WhenWeb().WhenNotDefault(api => api.HttpContext())
                .SelectMany(api => {
                    var redirectUri = api.GetRequestUri().GetLeftPart(application.GetPlatform()==Platform.Blazor?UriPartial.Authority :UriPartial.Path).ToString();
                    return new AuthorizationCodeWebApp(flow, redirectUri,application.GetPlatform()!=Platform.Blazor?redirectUri: application.CurrentUserId().ToString())
                        .AuthorizeAsync(application.CurrentUserId().ToString(), CancellationToken.None)
                        .ToObservable()
                        .SelectMany(result => result.RedirectUri == null ? result.Credential.Observe() : application.WhenWeb()
                            .TraceGoogleModule(_ => redirectUri)
                            .Do(webApi => webApi.Redirect(result.RedirectUri,false))
                            .To(default(UserCredential)))
                        .WhenNotDefault();
                });


        static IObservable<UserCredential> RequestAuthorizationCode(this IAuthorizationCodeFlow flow, XafApplication application,
	        Func<UserFriendlyException, IAuthorizationCodeFlow, IObservable<UserCredential>> acquireToken) 
	        => Observable.FromAsync(() => flow.LoadTokenAsync(application.CurrentUserId().ToString(), CancellationToken.None))
		        .Select(token => flow.ShouldForceTokenRetrieval() || token == null || token.RefreshToken == null && token.IsExpired(flow.Clock))
		        .SelectMany(b => b ? Observable.Throw<UserCredential>(new UserFriendlyException("Google authentication failed. Use the profile view to authenticate again"))
			        : default(UserCredential).Observe())
		        .Catch<UserCredential,UserFriendlyException>(exception => {
                    acquireToken??=(_, _) => Observable.Empty<UserCredential>();
			        var args = new GenericEventArgs<IObservable<UserCredential>>(acquireToken(exception, flow));
			        CustomAcquireTokenInteractivelySubject.OnNext(args);
			        return args.Instance;
		        })
		        .SelectMany(_ => flow.AuthorizeApp(application));
    }
}