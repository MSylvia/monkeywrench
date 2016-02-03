﻿/*
 * Master.master.cs
 *
 * Authors:
 *   Rolf Bjarne Kvinge (RKvinge@novell.com)
 *   
 * Copyright 2009 Novell, Inc. (http://www.novell.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */

using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using MonkeyWrench;
using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;
using MonkeyWrench.Web.WebServices;

public partial class Master : System.Web.UI.MasterPage
{
	private WebServiceLogin web_service_login;
	private WebServiceResponse response;
	private GetLeftTreeDataResponse tree_response;

	public string userProfile = "";

	public WebServiceLogin WebServiceLogin
	{
		get
		{
			if (web_service_login == null)
				web_service_login = Utilities.CreateWebServiceLogin (Context.Request);

			return web_service_login;
		}
	}

	public void ClearLogin ()
	{
		web_service_login = null;
	}

	/// <summary>
	/// Redirects to the login page. Does not end the current request.
	/// </summary>
	public void RequestLogin ()
	{
		Response.Redirect ("Login.aspx?referrer=" + HttpUtility.UrlEncode (Request.Url.ToString ()));
	}

	private void SetResponse (WebServiceResponse response)
	{
		this.response = response;
		LoadView ();
	}

	protected void Page_Load (object sender, EventArgs e)
	{
		LoadView ();
		if (!string.IsNullOrEmpty (Configuration.SiteSkin)) {
			//idFavicon.Href = "res/" + Configuration.SiteSkin + "/favicon.ico";
			//imgLogo.Src = "res/" + Configuration.SiteSkin + "/logo.png";
			//cssSkin.Href = "res/" + Configuration.SiteSkin + "/" + Configuration.SiteSkin + ".css";
		}
	}

	private void LoadView ()
	{
		

		if (!Authentication.IsLoggedIn (response)) {
			cellLogin.Text = "<li><a href='User.aspx'>Create account</a></li><li><a href='Login.aspx'>Login</a></li>";
			adminmenu.Visible = false;
		} else {
			var user_response = Utils.LocalWebService.GetUser (WebServiceLogin, null, response.UserName);

			string displayName = String.IsNullOrEmpty(user_response.User.fullname)? user_response.User.login : user_response.User.fullname;

			cellLogin.Text = "";
			cellLogout.Text = "";
			adminmenu.Visible = true;

			userProfile = string.Format ("User.aspx?username={0}", Utilities.GetCookie (Request, "user"));
			userFullName.Text = displayName;
			userEmail.Text = user_response.User.Emails.Length > 0 ? user_response.User.Emails[0] : "No email provided.";
			userRole.Text = user_response.UserRoles.Length > 0 ? user_response.UserRoles[0] : "Standard";
		}

		if (tree_response != null)
			return;

		try {
			tree_response = Utils.LocalWebService.GetLeftTreeData (WebServiceLogin);
		} catch(UnauthorizedException) {
			// LoadView is called on the login page, but if anonymous access is disabled,
			// the user will be 'unauthorized' to view the sidebar, and thus the entire
			// login page.
			// So catch the exception and ignore it.
		}

		if (tree_response != null) {
			CreateTree ();
			CreateHostStatus ();
			CreateTagsList ();
		}


	}

	private void CreateTagsList() {
		var tags = new List<string> ();
		foreach (var tag in tree_response.Tags) {
			string next = string.Format ("<li><a href=\"{0}?tags={1}\" title='{2}'><i class=\"fa fa-circle-o\"></i>{2}</a></li>",
			              				MonkeyWrench.Configuration.IndexPage,
				              			HttpUtility.UrlEncode (tag),
				              			tag);
			tags.Add(next);
		}
		BuildLaneTags.InnerHtml = string.Join ("\n", tags.ToArray ());
	}

	private void CreateHostStatus ()
	{

		string headerW = @"<a href=""#"">
			<i class=""fa fa-server""></i> <span>Working</span>
			<i class=""fa fa-angle-left pull-right""></i>
			</a>
			<ul class=""treeview-menu"">";
		string headerL = @"<a href=""#"">
			<i class=""fa fa-server""></i> <span>Idle</span>
			<i class=""fa fa-angle-left pull-right""></i>
			</a>
			<ul class=""treeview-menu"">";
		string footer = "</ul></li>";

		try {
			var idles = new List<string> ();
			var working = new List<string> ();

			for (int i = 0; i < tree_response.HostStatus.Count; i++) {
				var status = tree_response.HostStatus [i];
				var idle = string.IsNullOrEmpty (status.lane);

				string tooltip = string.Empty;
				var color = EditHosts.GetReportDateColor (true, status.report_date);
				if (!idle)
					tooltip = string.Format ("Executing {0}\n", status.lane);
				tooltip += string.Format ("Last check-in date: {0}", index.TimeDiffToString (status.report_date, DateTime.Now));

				var str = string.Format("<li><a href=\"ViewHostHistory.aspx?host_id={0}\" title='{2}' class=\"text-{3}\"><i class=\"fa fa-circle-o text-{3}\"></i>{1}</a></li>", status.id, status.host, HttpUtility.HtmlEncode (tooltip), color);
				if (idle) {
					idles.Add (str);
				} else {
					working.Add (str);
				}
			}

			string w = string.Join ("\n", working.ToArray ());
			string l = string.Join ("\n", idles.ToArray ());

			HostStatusWorking.InnerHtml = headerW + w + footer;
			HostStatusIdle.InnerHtml = headerL + l + footer;
		} catch {
			HostStatusWorking.InnerHtml = "";
			HostStatusIdle.InnerHtml = "";
		}
	}

	private void CreateTree ()
	{
		if (this.response != null)
			return;

		// we need to create a tree of the lanes
		LaneTreeNode root;
		Panel div;

		// Remove disabled lanes.
		var lanes = new List<DBLane> (tree_response.Lanes);
		for (int i = lanes.Count -1; i >= 0; i--) {
			if (lanes [i].enabled)
				continue;
			lanes.RemoveAt (i);
		}
		root = LaneTreeNode.BuildTree (lanes, null);

		SetResponse (tree_response);

		// layout the tree
		div = new Panel ();
		div.ID = "tree_root_id";

		tableMainTree.Rows.Add (Utils.CreateTableRow (CreateTreeViewRow (MonkeyWrench.Configuration.IndexPage + "?show_all=true", "All", 0, root.Depth, true, div, true)));

		tableMainTree.Rows.Add (Utils.CreateTableRow (div));
		WriteTree (root, tableMainTree, 1, root.Depth, div);

		// layout the tags
		div = new Panel ();
		div.ID = "tags_root_id";

		tableMainTree.Rows.Add (Utils.CreateTableRow (CreateTreeViewRow (null, "Tags", 0, 1, true, div, true)));
		tableMainTree.Rows.Add (Utils.CreateTableRow (div));
		//WriteTags (tree_response.Tags, tableMainTree, 1, div);
	}

	public void WriteTags (List<string> tags, Table tableMain, int level, Panel containing_div)
	{
		Panel div = new Panel ();
		div.ID = "tag_node_" + (++counter).ToString ();

		foreach (var tag in tags) {
			containing_div.Controls.Add (CreateTreeViewRow (string.Format (MonkeyWrench.Configuration.IndexPage + "?tags={0}", HttpUtility.UrlEncode (tag)), tag, 1, 1, false, div, false));
		}
	}

	public void WriteTree (LaneTreeNode node, Table tableMain, int level, int max_level, Panel containing_div)
	{
		Panel div = new Panel ();
		div.ID = "tree_node_" + (++counter).ToString ();

		foreach (LaneTreeNode n in node.Children) {
			bool hiding = true;
			if (!string.IsNullOrEmpty (Request ["lane"])) {
				if (n.Lane.lane == Request ["lane"] || n.Find ((v) => v.Lane.lane == Request ["lane"]) != null) {
					hiding = false;
				}
			}

			containing_div.Controls.Add (CreateTreeViewRow (string.Format (MonkeyWrench.Configuration.IndexPage + "?lane={0}", HttpUtility.UrlEncode (n.Lane.lane)), n.Lane.lane, level, max_level, n.Children.Count > 0, div, hiding));
			
			if (n.Children.Count > 0) {
				containing_div.Controls.Add (div);
				WriteTree (n, tableMain, level + 1, max_level, div);
				div = new Panel ();
				div.ID = "tree_node_" + (++counter).ToString ();
			}
		}
	}

	static int counter = 0;
	public Table CreateTreeViewRow (string target, string name, int level, int max_level, bool has_children, Panel div_to_switch, bool enable_default_hiding)
	{
		TableRow row = new TableRow ();
		TableCell cell;
		Table tbl = new Table ();

		for (int i = 0; i < level; i++) {
			cell = new TableCell ();
			cell.Text = "<div style='width:8px;height:1px;'/>";
			row.Cells.Add (cell);
		}
		cell = new TableCell ();
		if (!has_children) {
			cell.Text = "<div style='width:20px;height:20px;'/>";
		} else {
			counter++;
			cell.Text = string.Format (@"
<img id='minus_img_{1}' src='res/minus.gif' alt='Collapse {0}' height='20px' width='20px' style='display:{3};' onclick='switchVisibility (""plus_img_{1}"", ""minus_img_{1}"", ""{2}"");' />
<img id='plus_img_{1}'  src='res/plus.gif'  alt='Expand   {0}' height='20px' width='20px' style='display:{4};'  onclick='switchVisibility (""plus_img_{1}"", ""minus_img_{1}"", ""{2}"");' />
", name, counter, div_to_switch.ClientID, (enable_default_hiding && level > 0) ? "none" : "block", (enable_default_hiding && level > 0) ? "block" : "none");
			if (enable_default_hiding && level > 0) {
				div_to_switch.Attributes ["style"] = "display: none;";
			}
		}
		row.Cells.Add (cell);
		if (!string.IsNullOrEmpty (target)) {
			row.Cells.Add (Utils.CreateTableCell (string.Format ("<a href='{0}'>{1}</a>", target, name)));
		} else {
			row.Cells.Add (Utils.CreateTableCell (name));
		}

		tbl.Rows.Add (row);

		return tbl;
	}
}

