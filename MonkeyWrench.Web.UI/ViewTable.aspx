<%@ Page MasterPageFile="~/Master.master" Language="C#" AutoEventWireup="true" Inherits="ViewTable" EnableViewState="false" Codebehind="ViewTable.aspx.cs" %>

<asp:Content ID="Content2" ContentPlaceHolderID="content" runat="Server" EnableViewState="false">
    <script type="text/javascript" src="ViewTable.js"></script>
	
	<div id="header" runat="server" enableviewstate="false"></div>
	<div id="buildtable" runat="server" enableviewstate="false">
        <asp:Label ID="lblMessage" runat="server" /></div>
	
	<br />
	<div id="buildtable-footer">
		<div id="pager" runat="server" enableviewstate="false"></div>
	</div>
</asp:Content>