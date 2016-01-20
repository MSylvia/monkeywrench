<%@ Page Language="C#" MasterPageFile="~/Master.master" AutoEventWireup="true" Inherits="Login" Codebehind="Login.aspx.cs" %>

<asp:Content ID="Content2" ContentPlaceHolderID="content" runat="Server">
	<asp:HiddenField ID="txtReferrer" runat="server" />
	<br/>
	<div class="login-box">
		<div class="login-logo">
			<a href="/"><b>Monkey</b>Wrench</a>
		</div>
		<div class="login-box-body">
			<p class="login-box-msg">
				<asp:Label ID="lblMessage" runat="server"></asp:Label>
				<asp:Label ID="lblMessageOpenId" runat="server"></asp:Label>
			</p>
			<%if (cmdLogin.Visible) { %>
				<div class="form-group has-feedback">
					<asp:TextBox ID="txtUser" runat="server" class="form-control" type="email" placeholder="Username"></asp:TextBox>
					<span class="glyphicon glyphicon-user form-control-feedback"></span>
				</div>
				<div class="form-group has-feedback">
					<asp:TextBox ID="txtPassword" runat="server" TextMode="Password" type="password" class="form-control" placeholder="Password"></asp:TextBox>
					<span class="glyphicon glyphicon-lock form-control-feedback"></span>
				</div>
				<div class="row">
					<div class="col-xs-8">
					</div>
					<div class="col-xs-4">
						<asp:Button ID="cmdLogin" runat="server" class="btn btn-primary btn-block btn-flat" Text="Login" OnClick="cmdLogin_Click" />
					</div>
				</div>
			<%} %>
			<div class="social-auth-links text-center">
				<%if (cmdLogin.Visible && (cmdLoginOauth.Visible || cmdLoginOpenId.Visible)){ %>
					<p>- OR -</p>
				<%} %>
				<asp:Button ID="cmdLoginOpenId" runat="server" class="btn btn-primary btn-block btn-flat" Text="Login using OpenId" OnClick="cmdLoginOpenId_Click" />
				<asp:Button ID="cmdLoginOauth" runat="server" class="btn btn-primary btn-block btn-flat" Text="Login using Google Oauth" OnClick="cmdLoginOauth_Click" />
			</div>
		</div>
	</div>
</asp:Content>
