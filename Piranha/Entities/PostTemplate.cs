﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace Piranha.Entities
{
	/// <summary>
	/// The post template entity.
	/// </summary>
	[Serializable]
	public class PostTemplate : StandardEntity<PostTemplate>
	{
		#region Properties
		/// <summary>
		/// Gets/sets the template name.
		/// </summary>	
		public string Name { get ; set ; }

		/// <summary>
		/// Gets/sets the description for the manager interface.
		/// </summary>
		public string Description { get ; set ; }

		/// <summary>
		/// Gets/sets the html-preview for the manager interface.
		/// </summary>
		public string Preview { get ; set ; }

		/// <summary>
		/// Gets/sets the properties defined. This field is NOT searchable in queries.
		/// </summary>
		public List<string> Properties { get ; set ; }

		/// <summary>
		/// Gets/sets the optional template that should render the view.
		/// </summary>
		public string ViewTemplate { get ; set ; }

		/// <summary>
		/// Gets/sets if the post should be able to override the template.
		/// </summary>
		public bool ShowViewTemplate { get ; set ; }

		/// <summary>
		/// Gets/sets the optional template that should render the archive view.
		/// </summary>
		public string ViewArchiveTemplate { get ; set ; }

		/// <summary>
		/// Gets/sets if the post should be able to override the archive template.
		/// </summary>
		public bool ShowViewArchiveTemplate { get ; set ; }

		/// <summary>
		/// Gets/sets whether to include post in rss feeds.
		/// </summary>
		public bool AllowRss { get ; set ; }
		#endregion

		#region Internal properties
		/// <summary>
		/// Gets/sets the persisted json data for the properties.
		/// </summary>
		internal string PropertiesJson { get ; set ; }
		#endregion

		/// <summary>
		/// Default constructor. Creates an empty template.
		/// </summary>
		public PostTemplate() {
			Properties = new List<string>() ;
		}

		#region Events
		/// <summary>
		/// Called when the entity has been loaded.
		/// </summary>
		/// <param name="db">The db context</param>
		public override void OnLoad(DataContext db) {
			var js = new JavaScriptSerializer() ;
			Properties = !String.IsNullOrEmpty(PropertiesJson) ? js.Deserialize<List<string>>(PropertiesJson) : Properties ;

			base.OnLoad(db);
		}

		/// <summary>
		/// Called when the entity is about to be saved.
		/// </summary>
		/// <param name="db">The db context</param>
		/// <param name="state">The current entity state</param>
		public override void OnSave(DataContext db, System.Data.EntityState state) {
			var js = new JavaScriptSerializer() ;
			PropertiesJson = js.Serialize(Properties) ;
			
			base.OnSave(db, state);
		}
		#endregion
	}
}
