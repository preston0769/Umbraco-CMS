﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Hosting;
using System.Web.Http.Routing;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Services;
using Umbraco.Web.WebApi;
using Umbraco.Web.WebServices;
using umbraco.presentation.umbraco.controls;
using umbraco.cms.presentation.Trees;
using System.Web.Script.Services;
using System.Web.Script.Serialization;
using System.EnterpriseServices;
using System.IO;
using System.Web.UI;
using umbraco.controls.Tree;
using Umbraco.Web.Trees;

namespace umbraco.presentation.webservices
{
    /// <summary>
    /// Client side ajax utlities for the tree
    /// </summary>
    [ScriptService]
    [WebService]
    public class TreeClientService : UmbracoAuthorizedWebService
    {

        /// <summary>
        /// Returns a key/value object with: json, app, js as the keys
        /// </summary>	
        /// <returns></returns>
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public Dictionary<string, string> GetInitAppTreeData(string app, string treeType, bool showContextMenu, bool isDialog, TreeDialogModes dialogMode, string functionToCall, string nodeKey)
        {
            AuthorizeRequest(app, true);

            var treeCtl = new TreeControl()
            {
                ShowContextMenu = showContextMenu,
                IsDialog = isDialog,
                DialogMode = dialogMode,
                App = app,
                TreeType = string.IsNullOrEmpty(treeType) ? "" : treeType, //don't set the tree type unless explicitly set
                NodeKey = string.IsNullOrEmpty(nodeKey) ? "" : nodeKey,
                StartNodeID = -1, //TODO: set this based on parameters!
                FunctionToCall = string.IsNullOrEmpty(functionToCall) ? "" : functionToCall
            };

            var returnVal = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(treeType))
            {
                //if there's not tree type specified, then render out the tree as per normal with the normal 
                //way of doing things
                returnVal.Add("json", treeCtl.GetJSONInitNode());
            }
            else
            {
                //first get the app tree definition so we can then figure out if we need to load by legacy or new
                //now we'll look up that tree
                var appTree = Services.ApplicationTreeService.GetByAlias(treeType);
                if (appTree == null)
                    throw new InvalidOperationException("No tree found with alias " + treeType);

                var controllerAttempt = appTree.TryGetControllerTree();
                if (controllerAttempt.Success)
                {
                    var context = WebApiHelper.CreateContext(new HttpMethod("GET"), Context.Request.Url, new HttpContextWrapper(Context));

                    var rootAttempt = appTree.TryGetRootNodeFromControllerTree(
                        new FormDataCollection(new Dictionary<string, string> {{"app", app}}),
                        context);

                    if (rootAttempt.Success)
                    {
                        
                    }
                }

                var legacyAttempt = appTree.TryGetLegacyTreeDef();


                //get the tree that we need to render
                var tree = TreeDefinitionCollection.Instance.FindTree(treeType).CreateInstance();
                tree.ShowContextMenu = showContextMenu;
                tree.IsDialog = isDialog;
                tree.DialogMode = dialogMode;
                tree.NodeKey = string.IsNullOrEmpty(nodeKey) ? "" : nodeKey;
                tree.FunctionToCall = string.IsNullOrEmpty(functionToCall) ? "" : functionToCall;
                //this would be nice to set, but no parameters :( 
                //tree.StartNodeID =

                //now render it's start node
                var xTree = new XmlTree();
                xTree.Add(tree.RootNode);
                returnVal.Add("json", xTree.ToString());    
            }

            returnVal.Add("app", app);
            returnVal.Add("js", treeCtl.JSCurrApp);

            return returnVal;
        }

        [Obsolete("Use the AuthorizeRequest methods on the base class UmbracoAuthorizedWebService instead")]
        public static void Authorize()
        {
            if (!BasePages.BasePage.ValidateUserContextID(BasePages.BasePage.umbracoUserContextID))
                throw new Exception("Client authorization failed. User is not logged in");
        }

    }
}
