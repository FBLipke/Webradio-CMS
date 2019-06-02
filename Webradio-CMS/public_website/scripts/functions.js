



function LoadDocument(url, sender, title, action, data, realm) {
	var u = url;
	if (data !== '') {
		u = url + "?" + data;
	}

	var request = $.ajax(
		{
			url: u,
			method: "GET",
			cache: false,
		    beforeSend: function (req)
		    {
			    req.setRequestHeader("Request-Type", "async");
			    req.setRequestHeader("UAgent", "Namiono");
			    if (action !== '') {
				    req.setRequestHeader("Action", action);
			    }

			    if (realm !== '') {
				    req.setRequestHeader("Realm", realm);
			    }

			    if (sender !== '') {
				    req.setRequestHeader("Sender", sender);
			    }
            }})
	.done(function (html) {
		var tar = request.getResponseHeader("Target");

		$(tar).fadeOut("50", function () {
			$(tar).html(html);

			if (action === 'login' || action === 'logout') {
				LoadDocument('/providers/navigation/', '.navigation-box', '', 'show', '', '');
				LoadDocument('/providers/navigation/', '.navigation-box', '', 'show', '', '');
				LoadDocument('index.html', '#content', 'Startseite', 'show', '', '');
			}
			
		});

		$(tar).fadeIn("1300");
	})
	.fail(function (html) {
		var tar = request.getResponseHeader("Target");
		$(tar).fadeOut("50", function() {
			$(tar).html(MsgBox(title, '<p class=\"exclaim\">Die Anfrage wurde beendet! (Fehler:' + html.statusCode + ' : ' + html.statusText + ')</p>'));
		});

		$(tar).fadeIn("1300");
	});
}

function MsgBox(title, message) {
	var x = "<div id=\"messagebox_err-box\">";
	x += "<div class=\"box-header\">" + title + "</div>\n";
	x += "<div class=\"box-content\">" + message + "</div>\n";
	x += "</div>";

	return x;
}

function update_metadata() {
	LoadDocument("/providers/shoutcast/", "#streaminfo", '', 'metadata', '', '');
	setTimeout("update_metadata()", 90000);
}

function sendForm(url, target, sender, form, action, realm, hide) {
	$(form).submit(function () {
		var request = $.ajax(
		{
			method: "POST",
			url: url,
			data: $(form).serialize(),
			beforeSend: function(xhr) {
				xhr.setRequestHeader("Request-Type", "async");
				xhr.setRequestHeader("UAgent", "Namiono");

				if (action != '') {
					xhr.setRequestHeader("Action", action);
				}

				if (realm != '') {
					xhr.setRequestHeader("Realm", realm);
				}

				if (sender != '') {
					xhr.setRequestHeader("Sender", sender);
				}

				if (target != '') {
					xhr.setRequestHeader("Target", target);
				}
			}
		})

		.done(function(data) {
			var tar = request.getResponseHeader("Target");
			$(tar).fadeOut("50", function() {
				$(tar).html(data);
				if (action == 'login' || action == 'logout')
				{
					LoadDocument('/providers/navigation/', '.navigation-box', '', 'show', '', '');
					LoadDocument('index.html', '#content', 'Startseite', 'show', '', '');
				}
			});

			$(tar).fadeIn("1300");
		})
		.fail(function(html) {
			var tar = request.getResponseHeader("Target");
			$(tar).fadeOut("50", function () {
				$(tar).html(MsgBox(title, '<p class=\"exclaim\">Die Anfrage wurde beendet! (Fehler: ' + html.statusText + ')</p>'));
			});

			$(tar).fadeIn("1300");
            });

		return false;
	});
}

function toggle(o, n) {
	$(o).fadeOut("50", function () {
		$(n).fadeIn("50");
	});
}

function Window(url, title) {
	window.open(url, "win", title, "toolbar=no,location=0,height=100,width=200");
}
