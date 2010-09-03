<%@ Control Language="C#" Inherits="Orchard.Mvc.ViewUserControl<SmtpSettingsPart>" %>
<%@ Import Namespace="Orchard.Email.Models"%>
<%@ Import Namespace="System.Net.Mail" %>
<fieldset>
    <legend><%: T("SMTP")%></legend>
    <div>
        <label for="<%: Html.FieldIdFor(m => m.Address)%>"><%: T("Sender email address")%></label>
        <%: Html.EditorFor(m => m.Address)%>
        <%: Html.ValidationMessage("Address", "*")%> 
    </div>
    <div>
        <label for="<%: Html.FieldIdFor(m => m.Host)%>"><%: T("Host name")%></label>
        <%: Html.EditorFor(m => m.Host)%>
        <%: Html.ValidationMessage("Host", "*")%> 
    </div>
    <div>
        <label for="<%: Html.FieldIdFor(m => m.Port)%>"><%: T("Port number")%></label>
        <%: Html.EditorFor(m => m.Port)%>
        <%: Html.ValidationMessage("Port", "*")%> 
    </div>
    <div>
        <%: Html.EditorFor(m => m.EnableSsl)%>
        <label for="<%: Html.FieldIdFor(m => m.EnableSsl)%>" class="forcheckbox"><%: T("Enable SSL communications")%></label>
        <%: Html.ValidationMessage("EnableSsl", "*")%> 
    </div>
    <div>
        <%: Html.EditorFor(m => m.RequireCredentials)%>
        <label for="<%: Html.FieldIdFor(m => m.RequireCredentials)%>" class="forcheckbox"><%: T("Require credentials")%></label>
        <%: Html.ValidationMessage("RequireCredentials", "*")%> 
    </div>
    <div data-controllerid="<%: Html.FieldIdFor(m => m.RequireCredentials)%>">
        <div>
            <label for="<%: Html.FieldIdFor(m => m.UserName)%>"><%: T("User name")%></label>
            <%: Html.EditorFor(m => m.UserName)%>
            <%: Html.ValidationMessage("UserName", "*")%> 
        </div>
        <div>
            <label for="<%: Html.FieldIdFor(m => m.Password)%>"><%: T("Password")%></label>
            <%: Html.PasswordFor(m => m.Password)%>
            <%: Html.ValidationMessage("Password", "*")%> 
        </div>
    </div>
</fieldset>