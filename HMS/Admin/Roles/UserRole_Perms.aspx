﻿<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="UserRole_Perms.aspx.cs" Inherits="HMS.UserRole_Perms" Theme="Theme1" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body onload="ExpCollpse();" style="background-color:#f5f5f5">
    <form id="form1" runat="server">
      <asp:ScriptManager ID="ScriptManager1" runat="server">
                </asp:ScriptManager>
  <a href="#" onclick="TreeviewExpandCollapseAll('roleTreeView',true)"  style="color: #000000">Expand All</a>&nbsp;
  <a href="#" onclick="TreeviewExpandCollapseAll('roleTreeView',false)"  style="color: #000000">Collapse All</a>&nbsp;&nbsp;&nbsp;&nbsp;
  <a href="#" onclick="CheckUnCheckAll(true)"  style="color: #000000">Check All</a> &nbsp;
  <a href="#" onclick="CheckUnCheckAll(false)"  style="color: #000000">Un-Check All</a> &nbsp;
    <div style="height:400px;overflow:auto;" class="SGrid">
        <asp:TreeView ID="roleTreeView" runat="server" ShowCheckBoxes="Leaf" 
            ShowLines="True" ForeColor="Black">
        </asp:TreeView>
       <span style="display:none;">
           <asp:Button ID="btnSave" runat="server" Text="Button" OnClick="btnSave_Click" />
           <asp:HiddenField ID="isExpandedHdn" runat="server" Value="true" />
           </span>
    </div>
     <asp:PlaceHolder ID="PlaceHolder1" runat="server">
    <script language="javascript" type="text/javascript">
        function ExpCollpse() {
            var expandAll = document.getElementById('<%=isExpandedHdn.ClientID %>').value;
            if (expandAll == 'true')
                TreeviewExpandCollapseAll('roleTreeView', true);
            else
                TreeviewExpandCollapseAll('roleTreeView', false);
        }

        function TreeviewExpandCollapseAll(treeViewId, expandAll) {
            document.getElementById('<%=isExpandedHdn.ClientID %>').value = expandAll.toString();
            var displayState = (expandAll == true ? 'none' : 'block');
            var treeView = document.getElementById(treeViewId);
            if (treeView) {
                var treeLinks = treeView.getElementsByTagName('a');
                var nodeCount = treeLinks.length;
                var flag = true; for (i = 0; i < nodeCount; i++) {
                    if (treeLinks[i].firstChild.tagName) {
                        if (treeLinks[i].firstChild.tagName.toLowerCase() == 'img') {
                            var node = treeLinks[i];
                            var level = parseInt(treeLinks[i].id.substr(treeLinks[i].id.length - 1), 10);
                            var childContainer = GetParentByTagName('table', node).nextSibling;
                            if (flag) {
                                if (childContainer.style.display == displayState) {
                                    TreeView_ToggleNode(eval(treeViewId + '_Data'), level, node, 'r', childContainer);
                                }
                                flag = false;
                            }
                            else {
                                if (childContainer.style.display == displayState) {
                                    TreeView_ToggleNode(eval(treeViewId + '_Data'), level, node, 'l', childContainer);
                                }
                            }
                        }
                    }
                } //for loop ends
            }
        }
        //utility function to get the container of an element by tagname
        function GetParentByTagName(parentTagName, childElementObj) {
            var parent = childElementObj.parentNode;
            while (parent.tagName.toLowerCase() != parentTagName.toLowerCase()) {
                parent = parent.parentNode;
            }
            return parent;
        }

        function CheckUnCheckAll(checkAll) {
            var chList = document.getElementById('<%=roleTreeView.ClientID %>').getElementsByTagName('input');
            for (j = 0; j < chList.length; j++) {
                chList[j].checked = checkAll;
            }
        }

        function SaveParent() {
            var btnUpdate = window.parent.document.getElementById('btnUpdate');
            if (btnUpdate != null)
                btnUpdate.click();
        }
    </script>
    </asp:PlaceHolder>
    </form>
</body>
</html>
