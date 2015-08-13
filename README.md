# Salesforce-Proxy
This repo contains an ASP.NET Web API project that serves as a proxy for working with Salesforce in a client-side editor (Napa, Plunkr, etc). It services two purposes...performing a web authentication OAuth flow with SF for tokens and performing REST queries for the client (where CORS might not be supported).

# Handling Authentication #
Most ad-hoc editors have a hard time handling OAuth flows because most of these editors use dynamic and often obfuscated reply URLs. The Salesforce Proxy solution makes it easy to work around this by leveraging the proxy (which has a static reply URL) for performing the OAuth flow. The user simply needs to reference the sfproxy.js and wire-up the proxy to a button using the sfLoginButton. The sfLoginButton takes a callback that will return all the token details from the OAuth flow. Here is a simple (but complete) sample that show the wire-up and callback handling:

    <!DOCTYPE html>
    <html lang="en" xmlns="http://www.w3.org/1999/xhtml">
    <head>
        <meta charset="utf-8" />
        <title>Salesforce Auth Sample</title>
        <!-- must reference jquery and sfproxy.js-->
        <script src="https://ajax.aspnetcdn.com/ajax/jQuery/jquery-2.1.4.js"></script>
        <script src="https://o365workshop.azurewebsites.net/scripts/sfproxy.js"></script>
        <script type="text/javascript">
            $(document).ready(function () {
                $("#btnSignin").sfLoginButton(function (token) {
                    if (token !== null)
                        alert("We got tokens!!!");
                    else
                        alert("Something went wrong!!!");
                });
            });
        </script>
    </head>
    <body>
        <button id="btnSignin">Sign-in with Salesforce</button>
    </body>
    </html>

If you want to see how the OAuth flow is works in the proxy, look at the **Scripts\sfproxy.js** file and the **Controllers\OAuthController.cs**.

# Calling REST APIs #
The goal of the proxy is to not change the way you call into Salesforce REST APIs. The proxy hosts a Web API end-point that accepts a Salesforce REST query as a parameter. This operation just performs the Salesforce REST query exactly as it was written and passes the raw json back. To make it transparent to the developer, the sfproxy.js file intercepts jquery ajax calls and swaps calls out the URL with the proxy with the original URL as a parameter. The proxy hosts a generic Web API Currently, only GET operations are supported, but I am looking to support other REST verbs such as POST, PATCH, DELETE, etc.