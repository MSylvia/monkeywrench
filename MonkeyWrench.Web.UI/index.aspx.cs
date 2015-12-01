/*
 * index.aspx.cs
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
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;
using MonkeyWrench.Web.WebServices;

using System.Linq;
using System.IO;

public partial class index : System.Web.UI.Page
{
	int limit = 10;

	private new Master Master
	{
		get { return base.Master as Master; }
	}

	protected override void OnLoad (EventArgs e)
	{
		base.OnLoad (e);

		FrontPageResponse data;

		if (string.IsNullOrEmpty (Request ["limit"])) {
			if (Request.Cookies ["limit"] != null)
				int.TryParse (Request.Cookies ["limit"].Value, out limit);
		} else {
			int.TryParse (Request ["limit"], out limit);
		}
		if (limit <= 0)
			limit = 10;
		Response.Cookies.Add (new HttpCookie ("limit", limit.ToString ()));

		string lanes_str = null;
		string lane_ids_str = null;

		string [] lanes = null;
		List<int> lane_ids = null;
		string [] tags = null;

		if (!string.IsNullOrEmpty (Request ["show_all"])) {
			// do nothing, default is to show all
		} else if (!string.IsNullOrEmpty (Request ["tags"])) {
			tags = Request ["tags"].Split (',');
		} else {
			HttpCookie cookie;

			lanes_str = Request ["lane"];
			lane_ids_str = Request ["lane_id"];

			if (string.IsNullOrEmpty (lanes_str) && string.IsNullOrEmpty (lane_ids_str)) {
				if ((cookie = Request.Cookies ["index:lane"]) != null) {
					lanes_str = HttpUtility.UrlDecode (cookie.Value);
				}
				if ((cookie = Request.Cookies ["index:lane_id"]) != null) {
					lane_ids_str = HttpUtility.UrlDecode (cookie.Value);
				}
			}

			if (!string.IsNullOrEmpty (lanes_str))
				lanes = lanes_str.Split (';');
			if (!string.IsNullOrEmpty (lane_ids_str)) {
				lane_ids = new List<int> ();
				foreach (string str in lane_ids_str.Split (';')) {
					int? ii = Utils.TryParseInt32 (str);
					if (ii.HasValue)
						lane_ids.Add (ii.Value);
				}
			}

			Response.Cookies.Set (new HttpCookie ("index:lane", HttpUtility.UrlEncode (lanes_str)));
			Response.Cookies.Set (new HttpCookie ("index:lane_id", HttpUtility.UrlEncode (lane_ids_str)));
		}
			
		// When viewing tags historically set day limit to zero otherwise we see nothing.
		data = Utils.LocalWebService.GetFrontPageDataWithTags (Master.WebServiceLogin, limit, 0, lanes, lane_ids != null ? lane_ids.ToArray () : null, tags != null ? 0 : 30, tags);
		
		this.buildtable.InnerHtml = tags != null ? GenerateTaggedOverview (data) : GenerateOverview (data);
	}

	private void WriteLanes (List<StringBuilder> header_rows, LaneTreeNode node, int level, int depth)
	{
		if (header_rows.Count <= level)
			header_rows.Add (new StringBuilder ());

		foreach (LaneTreeNode n in node.Children) {
			header_rows [level].AppendFormat ("<td colspan='{0}'>{1} </br></br> PID: {2} </br> SID: {3}</td>", n.Leafs == 0 ? 1 : n.Leafs, n.Lane.lane, n.Lane.parent_lane_id, n.Lane.id);

			WriteLanes (header_rows, n, level + 1, depth);
		}

		if (node.Children.Count == 0) {
			for (int hl = 0; hl < node.HostLanes.Count; hl++) {
				for (int i = level; i < depth; i++) {
					if (header_rows.Count <= i)
						header_rows.Add (new StringBuilder ());
					header_rows [i].Append ("<td colspan='1'>-</td>");
				}
			}
			// Empty all the way down
			if(node.HostLanes.Count == 0)
				for (int i = level; i < depth; i++) {
					if (header_rows.Count <= i)
						header_rows.Add (new StringBuilder ());
					header_rows [i].Append ("<td colspan='1'>-</td>");
				}
		}
	}

	void WriteHostLane (StringBuilder matrix, IEnumerable<DBHost> hosts, DBHostLane hl)
	{
		matrix.AppendFormat ("<td><a href='ViewTable.aspx?lane_id={1}&amp;host_id={2}' class='{3}'>{0}</a></td>", Utils.FindHost (hosts, hl.host_id).host, hl.lane_id, hl.host_id, hl.enabled ? "enabled-hostlane" : "disabled-hostlane");
	}

	private void WriteHostLanes (StringBuilder matrix, LaneTreeNode node, IEnumerable<DBHost> hosts, List<int> hostlane_order)
	{
		node.ForEach (new Action<LaneTreeNode> (delegate (LaneTreeNode target)
		{
			if (target.Children.Count != 0)
				return;

			if (target.HostLanes.Count == 0) {
				matrix.Append ("<td>-</td>"); // for empty ones
			} else {
				foreach (DBHostLane hl in target.HostLanes) {
					hostlane_order.Add (hl.id);
					WriteHostLane (matrix, hosts, hl);
				}
			}
		}));
	}

	private LaneTreeNode BuildTree (FrontPageResponse data)
	{
		LaneTreeNode result = LaneTreeNode.BuildTree (data.Lanes, data.HostLanes);
		if (data.Lane != null) {
			result = result.Find (v => v.Lane != null && v.Lane.id == data.Lane.id);
		} else if (data.SelectedLanes.Count > 1) {
			for (int i = result.Children.Count - 1; i >= 0; i--) {
				LaneTreeNode ltn = result.Children [i];
				if (!data.SelectedLanes.Exists ((DBLane l) => l.id == ltn.Lane.id)) {
					result.Children.RemoveAt (i);
				}
			}
		}
		return result;
	}

	public static string TimeDiffToString (DateTime from, DateTime to)
	{
		int value;
		TimeSpan diff = to - from;

		if (from == DateTime.MinValue)
			return "Never";

		if (diff.TotalHours < 1) {
			value = (int) diff.TotalMinutes;
			if (value == 1) {
				return "1 minute ago";
			} else {
				return string.Format ("{0} minutes ago", value);
			}
		} else if (diff.TotalDays < 1) {
			value = (int) diff.TotalHours;
			if (value == 1) {
				return "1 hour ago";
			} else {
				return string.Format ("{0} hours ago", value);
			}
		} else if (diff.TotalDays < 3) {
			value = (int) diff.TotalDays;
			if (value == 1) {
				return "1 day ago";
			} else {
				return string.Format ("{0} days ago", value);
			}
		} else {
			return from.ToString ("yyyy-MM-dd");
		}
	}

	public string GenerateTaggedOverview (FrontPageResponse data)
	{
		StringBuilder matrix = new StringBuilder ();

		var lane_row = new StringBuilder ();
		var hl_row = new StringBuilder ();
		var rows = new List<StringBuilder> ();

		// First pass to get the total ammount of rows needed.
		for (int i = 0; i < data.SelectedLanes.Count; i++) {
			var lane = data.SelectedLanes [i];
			var hls  = data.HostLanes.FindAll ((hl) => hl.lane_id == lane.id);
			foreach (var hl in hls) {
				var work_views = FindRevisionWorkViews (data, hl.id);
				// Create more rows if needed.
				if (rows.Count < work_views.Count) {
					rows.Capacity = work_views.Count;
					for (int r = rows.Count; r < work_views.Count; r++)
						rows.Add (new StringBuilder ());
				}
			}
		}

		// Render pass
		matrix.AppendLine ("<table class='buildstatus'>");

		for (int i = 0; i < data.SelectedLanes.Count; i++) {
			var lane = data.SelectedLanes [i];
			var hls  = data.HostLanes.FindAll ((hl) => hl.lane_id == lane.id);
			lane_row.AppendFormat ("<td colspan='{1}'>{0}</td>", data.SelectedLanes [i].lane, hls.Count == 0 ? 1 : hls.Count).AppendLine ();
			foreach (var hl in hls) {
				// This renders all the host header lanes 
				WriteHostLane (hl_row, data.Hosts, hl);

				// Find all the builds
				var work_views = FindRevisionWorkViews (data, hl.id);

				// Create more rows if needed.
				if (rows.Count < work_views.Count) {
					rows.Capacity = work_views.Count;
					for (int r = rows.Count; r < work_views.Count; r++)
						rows.Add (new StringBuilder ());
				}

				for (int r = 0; r < work_views.Count; r++) {
					WriteWorkCell (rows [r], work_views [r]);
				}

				// Fix the staggering, add the empty cells.
				if (work_views.Count < rows.Count)
					for (int r = work_views.Count; r < rows.Count; r++) {
						WriteWorkCell (rows [r], null);
					}

			}
		}

		matrix.Append ("<tr>").Append (lane_row).AppendLine ("</tr>");
		matrix.Append ("<tr>").Append (hl_row).AppendLine ("</tr>");
		for (int r = 0; r < rows.Count; r++) {
			matrix.Append ("<tr>").Append (rows [r]).AppendLine ("</tr>");
		}
		matrix.AppendLine ("</table>");

		return matrix.ToString ();
	}

	void WriteWorkCell (StringBuilder row, DBRevisionWorkView2 work)
	{
		if (work == null) {
			row.Append ("<td>-</td>");
			return;
		}

		string revision = work.revision;
		int lane_id = work.lane_id;
		int host_id = work.host_id;
		int revision_id = work.revision_id;
		DBState state = work.State;
		bool completed = work.completed;
		string state_str = state.ToString ().ToLowerInvariant ();
		bool is_working;
		string str_date = string.Empty;

		if (work.endtime != null && work.endtime.Value.Year > 2000)
			str_date = "<br/>" + TimeDiffToString (work.endtime.Value, DateTime.UtcNow);

		switch (state) {
		case DBState.Executing:
			is_working = true;
			break;
		case DBState.NotDone:
		case DBState.Paused:
		case DBState.DependencyNotFulfilled:
		case DBState.Ignore:
			is_working = false;
			break;
		default:
			is_working = !completed;
			break;
		}

		long dummy;
		if (revision.Length > 16 && !long.TryParse (revision, out dummy))
			revision = revision.Substring (0, 8);

		if (is_working) {
			row.AppendFormat (
				@"<td class='{1}'>
							<center>
								<table class='executing'>
									<td>
										<a href='ViewLane.aspx?lane_id={2}&amp;host_id={3}&amp;revision_id={4}' title='{5}'>{0}{6}</a>
									</td>
								</table>
							<center>
						  </td>",
				revision, state_str, lane_id, host_id, revision_id, "", str_date);
		} else {
			row.AppendFormat ("<td class='{1}'><a href='ViewLane.aspx?lane_id={2}&amp;host_id={3}&amp;revision_id={4}' title='{5}'>{0}{6}</a></td>",
				revision, state_str, lane_id, host_id, revision_id, "", str_date);
		}
	}

	List<DBRevisionWorkView2> FindRevisionWorkViews (FrontPageResponse data, int hostlane_id)
	{
		for (int k = 0; k < data.RevisionWorkHostLaneRelation.Count; k++) {
			if (data.RevisionWorkHostLaneRelation [k] == hostlane_id)
				return data.RevisionWorkViews [k];
		}

		return null;
	}

	public string ExtractRepoName(String repo) {
		var regex = new Regex (@"github.com.(\S+)\/(\S+)");
		var match = regex.Match (repo);

		if (match.Groups [1].Length == 0 && match.Groups [2].Length == 0)
			return null;

		return match.Groups [1] + "/" + Path.GetFileNameWithoutExtension(match.Groups [2].ToString());
	}

	public string ExtractBranchName(string branch) {
		return (string.IsNullOrEmpty(branch) ? "master" : new Regex (@".*?origin\/(.*?)(\s|$)").Match(branch).Groups[1].ToString());
	}

	public string GenerateOverview (FrontPageResponse data)
	{
		StringBuilder matrix = new StringBuilder ();

		matrix.AppendLine ("<script type=\"text/javascript\">(function(document) {\n\t'use strict';\n\n\tvar LightTableFilter = (function(Arr) {\n\n\t\tvar _input;\n\n\t\tfunction _onInputEvent(e) {\n\t\t\t_input = e.target;\n\t\t\tvar tables = document.getElementsByClassName(_input.getAttribute('data-table'));\n\t\t\tArr.forEach.call(tables, function(table) {\n\t\t\t\tArr.forEach.call(table.tBodies, function(tbody) {\n\t\t\t\t\tArr.forEach.call(tbody.rows, _filter);\n\t\t\t\t});\n\t\t\t});\n\t\t}\n\n\t\tfunction _filter(row) {\n\t\t\tvar text = row.textContent.toLowerCase(), val = _input.value.toLowerCase();\n\t\t\trow.style.display = text.indexOf(val) === -1 ? 'none' : 'table-row';\n\t\t}\n\n\t\treturn {\n\t\t\tinit: function() {\n\t\t\t\tvar inputs = document.getElementsByClassName('light-table-filter');\n\t\t\t\tArr.forEach.call(inputs, function(input) {\n\t\t\t\t\tinput.oninput = _onInputEvent;\n\t\t\t\t});\n\t\t\t}\n\t\t};\n\t})(Array.prototype);\n\n\tdocument.addEventListener('readystatechange', function() {\n\t\tif (document.readyState === 'complete') {\n\t\t\tLightTableFilter.init();\n\t\t}\n\t});\n\n})(document);</script>");
		matrix.AppendLine ("Fuzzy Filter: <input type=\"search\" class=\"light-table-filter\" data-table=\"order-table\" placeholder=\"Filter\">");

		matrix.AppendLine ("<table class='buildstatus order-table table'>");

		// By repo to lane to hostlane to job ---------------
		bool first = true; // little style hack for first element
		var all_repos = data.Lanes.Select(lane => lane.repository).Distinct();

		foreach (var repos in all_repos.GroupBy ((r) => ExtractRepoName (r)).OrderBy( r => r.Key )) {
			if (string.IsNullOrEmpty (repos.Key))
				continue;
			
			matrix.AppendLine ("<tr>");
			matrix.AppendLine ("<td colspan='" + (limit + 2) + "' style='text-align:left; border-left-color: #FFF; border-right-color: #FFF; " + (!first ? "" : "border-top-color: #FFF")  + "'>");
			matrix.AppendLine ("<h3>" + repos.Key + " </h3>");
			matrix.AppendLine ("</td></tr>");

			foreach (var repo in repos) {
				if (string.IsNullOrEmpty (repo))
					continue;
			
				List<DBLane> lanes;
				if (data.SelectedLanes.Count > 0) {
					LaneTreeNode tree = BuildTree(data);

					// recursivly find lanes under this
					lanes = data.SelectedLanes;
					LaneTreeNode node = tree.Find (n => n.Lane.lane == lanes.First().lane);

					if(node != null)
						lanes = node.GetAllNodes().Select(l => l.Lane).ToList();
					

				} else {
					lanes = data.Lanes.FindAll (lane => lane.repository == repo);
				}
				
				lanes = lanes.FindAll (lane => lane.repository == repo).OrderBy(l => ExtractBranchName(l.max_revision)).ToList();

				foreach (var lane in lanes) {
					List<DBHostLane> hosts_lanes = data.HostLanes.FindAll (hl => hl.lane_id == lane.id);
					int count = hosts_lanes.Count;
					if (count == 0)
						continue;

					if (count == 1) {
						var rev = FindRevisionWorkViews (data, hosts_lanes [0].id);
						if (rev == null || rev.Count == 0)
							continue;
					}

					matrix.AppendLine ("<tr>");
					matrix.AppendFormat ("<td rowspan='{0}' title='Branch: {2}'>{1}</td>",
						(count != 1 ? count + 1 : count),
						lane.lane,
						ExtractBranchName(lane.max_revision)
					);

					foreach (var host_lane in hosts_lanes) {
						var rev = FindRevisionWorkViews (data, host_lane.id);

						if (hosts_lanes.Count > 1)
							matrix.AppendLine ("<tr>");
					
						DBHost host = data.Hosts.Find (h => h.id == host_lane.host_id);

						matrix.AppendFormat ("<td><a href='ViewTable.aspx?lane_id={1}&amp;host_id={2}' class='{3}'>{0}</a></td>",
							host.host,
							host_lane.lane_id,
							host_lane.host_id,
							host_lane.enabled ? "enabled-hostlane" : "disabled-hostlane"
						);

						for (int i = 0; i < limit; i++) {
							var cell = (i >= rev.Count) ? null : rev [i];
							WriteWorkCell (matrix, cell);
						}

						if (hosts_lanes.Count > 1)
							matrix.AppendLine ("</tr>");
					}
					matrix.AppendLine ("</tr>");
				}
			}
		}

		matrix.AppendLine ("</table>");

		return matrix.ToString ();
	}


	// Old Page - not used any
	public string GenerateOverview2 (FrontPageResponse data)
	{
		StringBuilder matrix = new StringBuilder ();
		LaneTreeNode tree = BuildTree (data);
		List<StringBuilder> header_rows = new List<StringBuilder> ();
		List<int> hostlane_order = new List<int> ();

		if (tree == null)
			return string.Empty;
		
		// This renders all the host header lanes 
		WriteLanes (header_rows, tree, 0, tree.Depth); 

		matrix.AppendLine ("<table class='buildstatus'>");
		for (int i = 0; i < header_rows.Count; i++) {
			if (header_rows [i].Length == 0)
				continue;

			matrix.Append ("<tr>");
			matrix.Append (header_rows [i]);
			matrix.AppendLine ("</tr>");
		}

		// Renders all the hosts
		matrix.AppendLine ("<tr>");
		WriteHostLanes (matrix, tree, data.Hosts, hostlane_order); 
		matrix.AppendLine ("</tr>");

		// Renders all the builds
		int counter = 0;
		int added = 0;
		StringBuilder row = new StringBuilder ();
		do {
			added = 0;
			row.Length = 0;

			for (int i = 0; i < hostlane_order.Count; i++) {
				int hl_id = hostlane_order [i];

				var rev = FindRevisionWorkViews (data, hl_id);
				DBRevisionWorkView2 work = null;

				if (rev != null && rev.Count > counter) {
					work = rev [counter];
					added++;
				}

				WriteWorkCell (row, work);
			}

			if (added > 0 && row.Length > 0) {
				matrix.Append ("<tr>");
				matrix.Append (row.ToString ());
				matrix.Append ("</tr>");
			}

			counter++;
		} while (counter <= limit && added > 0);

		matrix.AppendLine ("</table>");

		return matrix.ToString ();
	}
}

