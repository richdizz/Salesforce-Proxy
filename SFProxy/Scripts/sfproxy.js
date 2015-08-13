var SFProxy;
(function (SFProxy) {
    "use strict";

    $.fn.sfLoginButton = function (oauthCompleteCallback) {
        var ctrl = this;
        var cid = "3MVG9KI2HHAq33RwNhhQSjqy7NxjoavGDbHPGE1KwK.rZk5pdB4.NyPBNUhyp3LtX6BGKL6C2bCw9aAedVR3F";
        //var proxy_address = "https://localhost:44300";
        var proxy_address = "https://o365workshop.azurewebsites.net";
        var callback = oauthCompleteCallback;
        var hub = null, proxy = null, signalId = null;

        //temporarily disable the ctrl while scripts load
        ctrl.prop("disabled", true);

        //import signalR scripts from proxy
        $.getScript(proxy_address + "/scripts/jquery.signalR-2.1.2.min.js").done(function () {
            $.getScript(proxy_address + "/signalr/hubs").done(function () {
                //setup signalR hub through proxy
                $.connection.hub = $.hubConnection(proxy_address + "/signalr/hubs", { useDefaultPath: false });
                hub = $.connection.hub;
                proxy = $.connection.hub.createHubProxy("OAuthHub");

                //establish a callback for the server to send tokens from
                proxy.on("oAuthComplete", function (token) {
                    $.ajaxSetup({
                        beforeSend: function (jqXHR, settings) {
                            if (settings.url.indexOf("salesforce.com") !== -1) {
                                //take the original REST call into SalesForce and send it through our proxy
                                settings.url = proxy_address + "/api/query?q=" + settings.url;
                            }
                        }
                    });

                    //send the token back on the provided callback
                    callback(token);
                });

                // Start the connection to the hub
                $.connection.hub.start({ jsonp: true }).done(function () {
                    //initialize the hub and then get the client off of it
                    proxy.invoke("initialize");
                    signalId = $.connection.hub.id;

                    //enable the button now that all information is loaded
                    ctrl.prop("disabled", false);

                    //setup the click event on the control
                    ctrl.click(function () {
                        var oauthRedirect = "https://login.salesforce.com/services/oauth2/authorize?response_type=code&client_id=" + cid + "&redirect_uri=" + proxy_address + "/OAuth/AuthCode&state=";
                        oauthRedirect += signalId;
                        window.open(oauthRedirect, "_blank", "width=500, height=600, scrollbars=0, toolbar=0, menubar=0, resizable=0, status=0, titlebar=0");
                    });
                }).fail(function (err) {
                    callback(null);
                });
            });
        });
    };
})(SFProxy || (SFProxy = {}));