<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="_Default" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <asp:Button id="randButton" Text="Randomize" OnClick="Randomize" runat="server" UseSubmitBehavior="false" OnClientClick="this.disabled = true" Visible="false"/>
        <asp:Button id="downloadButton" Text="Download Data" OnClick="DownloadData" runat="server" UseSubmitBehavior="false" OnClientClick="this.disabled = true"/>
        <div id="items" runat="server"></div>
    </div>
    </form>
</body>
</html>
