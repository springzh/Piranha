﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.ServiceModel.Activation;
using System.Text;
using System.Web;
using System.Web.Configuration;
using System.Web.Compilation;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.Routing;

using Piranha.Models;
using Piranha.WebPages.RequestHandlers;

namespace Piranha.WebPages
{
	public static class WebPiranha
	{
		#region Members
		/// <summary>
		/// Internal member for the host names.
		/// </summary>
		private static Dictionary<string, Entities.SiteTree> hostNames = null ;

		/// <summary>
		/// The different request handlers.
		/// </summary>
		internal static Dictionary<string, RequestHandlerRegistration> Handlers = new Dictionary<string, RequestHandlerRegistration>() ;

		/// <summary>
		/// The registered cultures.
		/// </summary>
		internal static Dictionary<string, CultureInfo> Cultures = new Dictionary<string,CultureInfo>() ;

		/// <summary>
		/// The registered cultures prefixes.
		/// </summary>
		internal static Dictionary<string, string> CulturePrefixes = new Dictionary<string, string>() ;

		/// <summary>
		/// The registered hostnames.
		/// </summary>
		internal static Dictionary<string, Entities.SiteTree> HostNames {
			get {
				if (hostNames == null) {
					hostNames = new Dictionary<string,Entities.SiteTree>() ;
					RegisterDefaultHostNames() ;
				}
				return hostNames ;
			}
		}

		/// <summary>
		/// The default site tree.
		/// </summary>
		internal static Entities.SiteTree DefaultSite = null ;

		/// <summary>
		/// The current site tree.
		/// </summary>
		internal static Entities.SiteTree CurrentSite {
			get {
				// Check for configured site tree from the host name
				if (HttpContext.Current != null && HttpContext.Current.Request != null) {
					var hostname = HttpContext.Current.Request.Url.Host.ToLower() ;
					if (HostNames.ContainsKey(hostname))
						return HostNames[hostname] ;
				}
				// Nothing found, return default
				return DefaultSite ;
			}
		}
		#endregion

		#region Properties
		/// <summary>
		/// Gets the current application root.
		/// </summary>
		public static string ApplicationPath {
			get {
				string root = HttpContext.Current.Request.ApplicationPath ;
				if (!root.EndsWith("/"))
					root += "/" ;
				return root ;
			}
		}

		/// <summary>
		/// Gets/sets whether to use prefixless permalinks.
		/// </summary>
		public static bool PrefixlessPermalinks { get ; set ; }
		#endregion

		/// <summary>
		/// Registers the given.
		/// </summary>
		/// <param name="urlprefix">The url prefix</param>
		/// <param name="id">The handler id</param>
		/// <param name="handler">The actual handler</param>
		public static void RegisterHandler(string urlprefix, string id, IRequestHandler handler) {
			Handlers.Add(id.ToUpper(), new RequestHandlerRegistration() { UrlPrefix = urlprefix, Id = id, Handler = handler }) ;
		}

		/// <summary>
		/// Removes the handler with the given id.
		/// </summary>
		/// <param name="id">The handler id</param>
		public static void RemoveHandler(string id) {
			if (Handlers.ContainsKey(id.ToUpper()))
				Handlers.Remove(id.ToUpper()) ;
		}

		/// <summary>
		/// Registers the culture to the given prefix.
		/// </summary>
		/// <param name="urlprefix">The url prefix.</param>
		/// <param name="culture">The culture</param>
		public static void RegisterCulture(string urlprefix, CultureInfo culture) {
			Cultures.Add(urlprefix, culture) ;
			CulturePrefixes.Add(culture.Name, urlprefix) ; 
		}

		/// <summary>
		/// Gets the current url prefix used for the given handler id.
		/// </summary>
		/// <param name="id">The handler id</param>
		/// <returns>The url prefix</returns>
		public static string GetUrlPrefixForHandlerId(string id) {
			if (Handlers.ContainsKey(id.ToUpper()))
				return Handlers[id.ToUpper()].UrlPrefix ;
			return "" ;
		}

		/// <summary>
		/// Get's the current culture prefix.
		/// </summary>
		/// <returns>The culture prefix</returns>
		public static string GetCulturePrefix() {
			return (CulturePrefixes.ContainsKey(CultureInfo.CurrentUICulture.Name) ? CulturePrefixes[CultureInfo.CurrentUICulture.Name] + "/" : "") ;
		}

		/// <summary>
		/// Gets the public site url.
		/// </summary>
		/// <returns>The url</returns>
		public static string GetSiteUrl() {
			var context = HttpContext.Current ;
			var url = "http://" + context.Request.Url.DnsSafeHost + 
				(!context.Request.Url.IsDefaultPort ? ":" + context.Request.Url.Port : "") +
				context.Request.ApplicationPath ;

			if (url.EndsWith("/"))
				return url.Substring(0, url.Length - 1) ;
			return url ;
		}

		/// <summary>
		/// Clears all of the currently registered handlers.
		/// </summary>
		public static void ResetHandlers() {
			Handlers.Clear() ;
		}

		/// <summary>
		/// Registers all of the default request handlers.
		/// </summary>
		public static void RegisterDefaultHandlers() {
			RegisterHandler("", "STARTPAGE", new PermalinkHandler()) ;
			RegisterHandler("home", "PERMALINK", new PermalinkHandler()) ;
			RegisterHandler("draft", "DRAFT", new DraftHandler()) ;
			RegisterHandler("media", "CONTENT", new ContentHandler()) ;
			RegisterHandler("mediadraft", "CONTENTDRAFT", new DraftContentHandler()) ;
			RegisterHandler("thumb", "THUMBNAIL", new ThumbnailHandler()) ;
			RegisterHandler("thumbdraft", "THUMBNAILDRAFT", new DraftThumbnailHandler()) ;
			RegisterHandler("upload", "UPLOAD", new UploadHandler()) ;
			RegisterHandler("archive", "ARCHIVE", new ArchiveHandler()) ;
			RegisterHandler("rss", "RSS", new RssHandler()) ;
			RegisterHandler("sitemap.xml", "SITEMAP", new SitemapHandler()) ;
		}

		/// <summary>
		/// Registers all of the hostnames configured in the database.
		/// </summary>
		public static void RegisterDefaultHostNames() {
			// Make sure we don't try to register the host names before
			// the database has been installed.
			if (SysParam.GetByName("SITE_VERSION") != null) {
				if (hostNames == null)
					hostNames = new Dictionary<string,Entities.SiteTree>() ;

				HostNames.Clear() ;
				if (HttpContext.Current != null)
					Page.InvalidateStartpage() ;

				// We need to check version so we don't try to access the column sitetree_hostnames
				// before it's been created in the database.
				if (Data.Database.InstalledVersion > 26) {
					using (var db = new DataContext()) {
						var sites = db.SiteTrees.ToList() ;
					
						foreach (var site in sites) {
							if (!String.IsNullOrEmpty(site.HostNames)) {
								var hostnames = site.HostNames.Split(new char[] { ',' }) ;

								foreach (var host in hostnames) {
									if (HostNames.ContainsKey(host))
										throw new Exception("Duplicates of the hostname [" + host + "] was found configured") ;
									HostNames.Add(host.ToLower(), site) ;
								}
							}
							if (site.Id == Config.DefaultSiteTreeId)
								DefaultSite = site ;
						}
					}
				}
			}
		}

		/// <summary>
		/// Initializes the webb app.
		/// </summary>
		public static void Init() {
			// Register virtual path provider for the manager area. This part includes a nasty hack for 
			// precompiled sites due to Microsofts implementation in the .NET framework. See
			//
			// http://sunali.com/2008/01/09/virtualpathprovider-in-precompiled-web-sites/
			//
			// for more information on the issue
			//
			PropertyInfo pc = typeof(BuildManager).GetProperty("IsPrecompiledApp", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) ;
			if (pc != null && (bool)pc.GetValue(null, null)) {
				// This is a precompiled application, bend the framework a bit.
				HostingEnvironment instance = (HostingEnvironment)typeof(HostingEnvironment).InvokeMember("_theHostingEnvironment", 
					BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.GetField, null, null, null) ;
				if (instance == null)
					throw new NullReferenceException("Can't get the current hosting environment") ;
				MethodInfo m = typeof(HostingEnvironment).GetMethod("RegisterVirtualPathProviderInternal", BindingFlags.NonPublic | BindingFlags.Static) ;
				if (m == null)
					throw new NullReferenceException("Can't get the RegisterVirtualPathProviderInternal method") ;
				m.Invoke(instance, new object[] { (VirtualPathProvider)new Piranha.Web.ResourcePathProvider() });
			} else {
				HostingEnvironment.RegisterVirtualPathProvider(new Piranha.Web.ResourcePathProvider()) ;
			}

			// Register the basic account route
			RouteTable.Routes.MapRoute("Account", "account/{action}", new { controller = "auth", action = "index" }, new string[] { "Piranha.Web" }) ;

			// This will trigger the manager area registration
			AreaRegistration.RegisterAllAreas() ;

			// Register handlers
			RegisterDefaultHandlers() ;

			// Register hostnames
			RegisterDefaultHostNames() ;

			// Reset template cache
			Web.TemplateCache.Clear() ;

			// Initialize the extension manager
			Extend.ExtensionManager.Init() ;

			// Register json deserialization for post data
			ValueProviderFactories.Factories.Add(new JsonValueProviderFactory());

			// Add the new view engine
			ViewEngines.Engines.Add(new Mvc.ManagerViewEngine());
		}

		/// <summary>
		/// Initializes the manager app.
		/// </summary>
		/// <param name="context"></param>
		public static void InitManager(AreaRegistrationContext context) {
			// Register manager routing
			if (context.Routes["Manager"] == null) {
				context.MapRoute(
					"Manager",
					"manager/{controller}/{action}/{id}",
					new { area = "manager", controller = "account", action = "index", id = UrlParameter.Optional }
				) ;
			}

			// Register filters & binders
			RegisterGlobalFilters(GlobalFilters.Filters) ;
			RegisterBinders() ;
		}

		/// <summary>
		/// Registers all of the default rest wcf services.
		/// </summary>
		public static void InitServices() {
			RouteTable.Routes.Add("REST_CATEGORY", new ServiceRoute("rest/category", new WebServiceHostFactory(), typeof(Rest.CategoryService))) ;
			RouteTable.Routes.Add("REST_SITEMAP", new ServiceRoute("rest/sitemap", new WebServiceHostFactory(), typeof(Rest.SitemapServices))) ;
			RouteTable.Routes.Add("REST_PAGE", new ServiceRoute("rest/page", new WebServiceHostFactory(), typeof(Rest.PageService))) ;
			RouteTable.Routes.Add("REST_POST", new ServiceRoute("rest/post", new WebServiceHostFactory(), typeof(Rest.PostService))) ;
			RouteTable.Routes.Add("REST_CONTENT", new ServiceRoute("rest/content", new WebServiceHostFactory(), typeof(Rest.ContentService))) ;
			RouteTable.Routes.Add("REST_PAGETEMPLATE", new ServiceRoute("rest/pagetemplate", new WebServiceHostFactory(), typeof(Rest.PageTemplateService))) ;
			RouteTable.Routes.Add("REST_POSTTEMPLATE", new ServiceRoute("rest/posttemplate", new WebServiceHostFactory(), typeof(Rest.PostTemplateService))) ;
			RouteTable.Routes.Add("REST_CHANGES", new ServiceRoute("rest/changes", new WebServiceHostFactory(), typeof(Rest.ChangeService))) ;
		}

		/// <summary>
		/// Handles the URL Rewriting for the application
		/// </summary>
		/// <param name="context">Http context</param>
		public static void BeginRequest(HttpContext context) {
			string path = context.Request.Path.Substring(context.Request.ApplicationPath.Length > 1 ? 
				context.Request.ApplicationPath.Length : 0) ;

			string[] args = path.Split(new char[] {'/'}).Subset(1) ;
				
			if (args.Length > 0) {
				int pos = 0 ;

				// Ensure database
				if (args[0] == "" && SysParam.GetByName("SITE_VERSION") == null)
					context.Response.Redirect("~/manager") ;

				// Check for culture prefix
				if (Cultures.ContainsKey(args[0])) {
					System.Threading.Thread.CurrentThread.CurrentUICulture = Cultures[args[0]] ;
					pos = 1;
				} else {
					var def = (GlobalizationSection)WebConfigurationManager.GetSection("system.web/globalization") ;
					if (def != null)
						System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo(def.UICulture) ;
				}

				var handled = false ;

				// Find the correct request handler
				foreach (RequestHandlerRegistration hr in Handlers.Values) {
					if (hr.UrlPrefix.ToLower() == args[pos].ToLower()) {
						if (hr.Id != "PERMALINK" || !PrefixlessPermalinks) {
							// Execute the handler
							hr.Handler.HandleRequest(context, args.Subset(pos + 1)) ;
							handled = false ;
							break ;
						}
					}
				}

				// If no handler was found and we are using prefixless permalinks, 
				// route traffic to the permalink handler.
				if (!handled && PrefixlessPermalinks && args[0].ToLower() != "manager") {
					if (Permalink.GetByName(Config.SiteTreeNamespaceId, args[0]) != null) {
						var handler = new PermalinkHandler() ;
						handler.HandleRequest(context, args) ;
					}
				}
			}
		}

		/// <summary>
		/// Handles current UI culture.
		/// </summary>
		/// <param name="context">The http context</param>
		public static void HandleCulture(HttpContext context) {
			//
			// NOTE: This code will fail completely in the manager view as accessing the request 
			// collection triggers the form data validation.
			//
			try {
				if (context.Request.HttpMethod.ToUpper() == "POST") {
					if (!String.IsNullOrEmpty(context.Request["lang"]))
						context.Session["lang"] = context.Request["lang"] ;
				}
				if (context.Session != null && context.Session["lang"] != null)
					System.Threading.Thread.CurrentThread.CurrentUICulture =
						new System.Globalization.CultureInfo((string)context.Session["lang"]) ;
			} catch {}
		}

		#region Private methods
		/// <summary>
		/// Registers all global filters.
		/// </summary>
		/// <param name="filters">The current filter collection</param>
		private static void RegisterGlobalFilters(GlobalFilterCollection filters) {
			filters.Add(new HandleErrorAttribute());
		}

		/// <summary>
		/// Registers all custom binders.
		/// </summary>
		private static void RegisterBinders() {
			if (ModelBinders.Binders[typeof(Piranha.Models.Manager.PageModels.EditModel)] == null)
				ModelBinders.Binders.Add(typeof(Piranha.Models.Manager.PageModels.EditModel), 
					new Piranha.Models.Manager.PageModels.EditModel.Binder()) ;
			if (ModelBinders.Binders[typeof(Piranha.Models.Manager.PostModels.EditModel)] == null)
				ModelBinders.Binders.Add(typeof(Piranha.Models.Manager.PostModels.EditModel), 
					new Piranha.Models.Manager.PostModels.EditModel.Binder()) ;
			if (ModelBinders.Binders[typeof(Piranha.Models.Manager.TemplateModels.PageEditModel)] == null)
				ModelBinders.Binders.Add(typeof(Piranha.Models.Manager.TemplateModels.PageEditModel),
					new Piranha.Models.Manager.TemplateModels.PageEditModel.Binder()) ;
			if (ModelBinders.Binders[typeof(Piranha.Models.Manager.TemplateModels.PostEditModel)] == null)
				ModelBinders.Binders.Add(typeof(Piranha.Models.Manager.TemplateModels.PostEditModel),
					new Piranha.Models.Manager.TemplateModels.PostEditModel.Binder()) ;
			if (ModelBinders.Binders[typeof(Piranha.Models.Manager.CategoryModels.EditModel)] == null)
				ModelBinders.Binders.Add(typeof(Piranha.Models.Manager.CategoryModels.EditModel),
					new Piranha.Models.Manager.CategoryModels.EditModel.Binder()) ;
			if (ModelBinders.Binders[typeof(Piranha.Models.Manager.SettingModels.UserEditModel)] == null)
				ModelBinders.Binders.Add(typeof(Piranha.Models.Manager.SettingModels.UserEditModel),
					new Piranha.Models.Manager.SettingModels.UserEditModel.Binder()) ;
			if (ModelBinders.Binders[typeof(Piranha.Models.Manager.ContentModels.EditModel)] == null)
				ModelBinders.Binders.Add(typeof(Piranha.Models.Manager.ContentModels.EditModel),
					new Piranha.Models.Manager.ContentModels.EditModel.Binder()) ;
			if (ModelBinders.Binders[typeof(Piranha.Extend.IExtension)] == null)
				ModelBinders.Binders.Add(typeof(Piranha.Extend.IExtension),
					new Piranha.Mvc.ModelBinders.IExtensionBinder()) ;
		}
		#endregion
	}
}
