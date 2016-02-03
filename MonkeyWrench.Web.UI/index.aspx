﻿<%@ Page Language="C#" MasterPageFile="~/Master.master" AutoEventWireup="true" Inherits="index" Codebehind="index.aspx.cs" EnableViewState="false" %>

<asp:Content ID="Content1" ContentPlaceHolderID="content" runat="Server" EnableViewState="False">
	<section class="content-header">
      <h1>
        Build Matrix
        <small>Tiny Text</small>
      </h1>
      <ol class="breadcrumb">
        <li><a href="#"><i class="fa fa-home"></i> Home</a></li>
        <li class="active">Build Matrix</li>
      </ol>
    </section>

    <section class="content">
        <div class="row">
    		<div id="buildtable" runat="server" />
    	</div>
    </section>

    <div><asp:Label runat="server" ID="lblMessage" ForeColor="Red" /></div>
    <div id="buildtable-footer">
    <div><a href="SelectLanes.aspx">Select lanes</a></div>
    <div>
    <a href="index.aspx?limit=10">View 10 revisions</a> - 
    <a href="index.aspx?limit=50">View 50 revisions</a> - 
    <a href="index.aspx?limit=100">View 100 revisions</a> - 
    <a href="index.aspx?limit=200">View 200 revisions</a> - 
    <a href="index.aspx?limit=500">View 500 revisions</a>
    </div>
    <h3>
        Legend</h3>
    <div>
        <table class='buildstatus'>
            <tr>
                <td class='success'>
                    Success
                </td>
                <td class='issues'>
                    Issues (test failures)
                </td>
                <td class='aborted'>
                    Aborted
                </td>
                <td class='executing'>
                    Executing
                </td>
                <td class='failed'>
                    Failed
                </td>
                <td class='notdone'>
                    Queued
                </td>
                <td class='paused'>
                    Paused
                </td>
                <td class='skipped'>
                    Skipped
                </td>
                <td class='timeout'>
                    Timeout
                </td>
                <td class='ignore'>
                    Ignore
                </td>
            </tr>
        </table>
    </div>
    </div>
</asp:Content>
