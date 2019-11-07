﻿using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.SharePoint.Phone.Application;
using Microsoft.SharePoint.Client;
using System.Collections.Generic;

namespace OrdersSPListApp
{
    /// <summary>
    /// Provides operations to fetch Items in a SharePoint List. 
    /// </summary>
    public class ListDataProvider : ListDataProviderBase
    {
        /// <summary>
        /// Provides access to ClientContext object which is used to execute queries to fetch ListItems from SharePoint server
        /// </summary>
        private ClientContext m_Context;
        public override ClientContext Context
        {
            get
            {
                if (m_Context != null)
                    return m_Context;

                m_Context = new ClientContext(SiteUrl);
                m_Context.Credentials = new Authenticator();

                return m_Context;
            }
        }

        /// <summary>
        /// Loads an Item from in-memory cache if already fetched once. If not, connects to SharePoint server to load the item asynchronously and update the cache
        /// </summary>
        /// <param name="id">Unique identifer of the Item (In case of external list, BDCIdentity else ListItem.Id)</param>
        public override void LoadItem(string id, Action<LoadItemCompletedEventArgs> loadItemCompletedCallback, params object[] filterParameters)
        {
            ListItem cachedItem = GetCachedItem(id);
            if (cachedItem != null)
            {
                loadItemCompletedCallback(new LoadItemCompletedEventArgs { Item = cachedItem });
                return;
            }

            LoadItemFromServer(id, loadItemCompletedCallback, filterParameters);
        }

        /// <summary>
        /// Loads a SharePoint View from in-memory cache if already fetched once. If not, queries SharePoint server to load the view and update the Cache.
        /// </summary>
        /// <param name="ViewName">Name of the SharePoint View</param>
        /// <param name="filterParameters">optional parameter which can be used to pass additional information while fetching views</param>
        public override void LoadData(string ViewName, Action<LoadViewCompletedEventArgs> loadViewCompletedCallback, params object[] filterParameters)
        {
            string customerName = string.Empty;
            string cacheKey = ViewName;

            // Parse the optional parameters:
            if (filterParameters.Length > 0)
            {
                customerName = filterParameters[0].ToString();
                cacheKey += "-" + customerName;
            }

            List<ListItem> CachedItems = GetCachedView(cacheKey);
            if (CachedItems != null)
            {
                loadViewCompletedCallback(new LoadViewCompletedEventArgs { ViewName = ViewName, Items = CachedItems });
                return;
            }

            LoadDataFromServer(ViewName, loadViewCompletedCallback, filterParameters);
        }


        /// <summary>
        /// Refreshes an Item by connecting to SharePoint server updates the cache
        /// </summary>
        /// <param name="id">Unique identifer of the Item (In case of external list, BDCIdentity else ListItem.Id)</param>
        public void RefreshItem(string id, Action<LoadItemCompletedEventArgs> loadItemCompletedCallback, params object[] filterParameters)
        {
            LoadItemFromServer(id, loadItemCompletedCallback, filterParameters);
        }

        /// <summary>
        /// Refreshes a SharePoint View by querying SharePoint server and updates the Cache.
        /// </summary>
        /// <param name="ViewName">Name of the SharePoint View</param>
        /// <param name="filterParameters">optional parameter which can be used to pass additional information while fetching views</param>
        public void RefreshData(string ViewName, Action<LoadViewCompletedEventArgs> loadItemCompletedCallback, params object[] filterParameters)
        {
            LoadDataFromServer(ViewName, loadItemCompletedCallback, filterParameters);
        }

        private void LoadItemFromServer(string id, Action<LoadItemCompletedEventArgs> loadItemCompletedCallback, params object[] filterParameters)
        {
            ListItem spListItem = Context.Web.Lists.GetByTitle(ListTitle).GetItemById(id);
            Context.Load(spListItem);
            Context.Load(spListItem, Item => Item.AttachmentFiles);

            Context.ExecuteQueryAsync(
                delegate(object sender1, ClientRequestSucceededEventArgs args)
                {
                    base.CacheItem(spListItem);
                    loadItemCompletedCallback(new LoadItemCompletedEventArgs { Item = spListItem });
                },
                delegate(object sender1, ClientRequestFailedEventArgs args)
                {
                    loadItemCompletedCallback(new LoadItemCompletedEventArgs { Error = args.Exception });
                });
        }

        private void LoadDataFromServer(string ViewName, Action<LoadViewCompletedEventArgs> loadItemCompletedCallback, params object[] filterParameters)
        {
            string customerName = string.Empty;
            string cacheKey = ViewName;

            // Parse the optional parameters:
            if (filterParameters.Length > 0)
            {
                customerName = filterParameters[0].ToString();
                cacheKey += "-" + customerName;
            }

            CamlQuery query = CamlQueryBuilder.GetCamlQuery(ViewName, customerName);
            ListItemCollection items = Context.Web.Lists.GetByTitle(ListTitle).GetItems(query);
            Context.Load(items);
            Context.Load(items, listItems => listItems.Include(item => item.FieldValuesAsText));

            Context.ExecuteQueryAsync(
                delegate(object sender, ClientRequestSucceededEventArgs args)
                {
                    base.CacheView(cacheKey, items);
                    loadItemCompletedCallback(new LoadViewCompletedEventArgs { ViewName = ViewName, Items = base.GetCachedView(cacheKey) });
                },
                delegate(object sender, ClientRequestFailedEventArgs args)
                {
                    loadItemCompletedCallback(new LoadViewCompletedEventArgs { Error = args.Exception });
                });
        }
    }

    /// <summary>
    /// Builds CamlQuery for fetching ListItems in SharePoint Views
    /// </summary>
    public static class CamlQueryBuilder
    {
        /// <summary>
        /// Provies access to ViewXml for SharePoint Views.
        /// </summary>
        static Dictionary<string, string> ViewXmls = new Dictionary<string, string>()
        {   
            //{"View1", @"<View><Query><OrderBy><FieldRef Name='ID' /></OrderBy></Query><RowLimit>30</RowLimit><ViewFields>{0}</ViewFields></View>"}
            {"View1", @"<View><Query>{0}</Query><RowLimit>30</RowLimit><ViewFields>{1}</ViewFields></View>"}
        };

        /// <summary>
        /// Provides access to ViewFields which should be fetched for every View. Since, all the Views uses the same Display,Edit and NewForms the ViewFields got 
        /// to be the same for all of them.
        /// </summary>
        static string ViewFields = @"<FieldRef Name='Title'/><FieldRef Name='Unit_x0020_Price'/><FieldRef Name='Quantity'/><FieldRef Name='Order_x0020_Value'/><FieldRef Name='Order_x0020_Date'/><FieldRef Name='Status'/><FieldRef Name='Customer'/>";

        /// <summary>
        /// Returns CamlQuery for fetching ListItems in the specified SharePoint Views
        /// </summary>
        /// <param name="viewName">Name of the View</param>
        /// <returns>CamlQuery for the View</returns>
        public static CamlQuery GetCamlQuery(string viewName, string customerName)
        {
            string queryClause = string.Empty;

            // Create appropriate Query Clause, depending on customerName parameter.
            if (string.IsNullOrWhiteSpace(customerName))
            {
                queryClause = "<OrderBy><FieldRef Name='ID' /></OrderBy>";
            }
            else
            {
                queryClause = string.Format("<Where><Eq><FieldRef Name='Customer' /><Value Type='Text'>{0}</Value></Eq></Where>", customerName);
            }

            // Add Query Clause and ViewFields to ViewXml.
            string viewXml = ViewXmls[viewName];
            viewXml = string.Format(viewXml, queryClause, ViewFields);

            return new CamlQuery { ViewXml = viewXml };
        }
    }
}